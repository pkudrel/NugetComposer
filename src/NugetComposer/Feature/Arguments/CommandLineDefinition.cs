using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using NugetComposer.Common.Bootstrap;
using NugetComposer.Feature.MainTask;
using NugetComposer.Services;

namespace NugetComposer.Feature.Arguments
{
    public static class CommandLineDefinition
    {
        public static CommandLineApplication Make(AppEnvironment appEnvironment)
        {
            var app = new CommandLineApplication();

            app.HelpOption();
            var optionConfigFile = app.Option<string>(
                "-c|--configFile <SUBJECT>",
                $"Config file. Default value: {Constants.DEFAULT_PACKAGE_CONFIG_FILE}",
                CommandOptionType.SingleValue).OnValidate(x =>
                {
                    var val = x.Items.Values.FirstOrDefault()?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(val) == false)
                    {
                        var isRooted = Path.IsPathRooted(val);
                        if (isRooted) return GetValidationResult(val);

                        var pathRelative = Path.Combine(appEnvironment.RootDir, val);
                        return GetValidationResult(pathRelative);
                    }

                    var defaultPath = Path.Combine(appEnvironment.RootDir, Constants.DEFAULT_PACKAGE_CONFIG_FILE);
                    return GetValidationResult(defaultPath);
                }
            );


            var optionDestination = app.Option<string>(
                "-d|--destination <SUBJECT>",
                $"Directory where program creates chrome-runtime. Default value: {Constants.DEFAULT_PACKAGE_DIR}",
                CommandOptionType.SingleValue);

            var optionClean = app.Option<bool>(
                "--clean",
                "Force clean after process",
                CommandOptionType.SingleValue);

            app.OnExecute(async () =>
            {
                var configFile = optionConfigFile.HasValue()
                    ? optionConfigFile.Value()
                    : Constants.DEFAULT_PACKAGE_CONFIG_FILE;

                var destination = optionDestination.HasValue()
                    ? optionDestination.Value()
                    : Constants.DEFAULT_PACKAGE_DIR;

                var clean = optionClean.HasValue() && optionClean.ParsedValue;


                var env = Boot.Instance.GetAppEnvironment();
                var options = new Options(configFile, destination, clean);
                var setting = SettingsBuilder.Create(env, options);
                var mpc = MainProcessSettingsBuilder.Create(env, setting);
                Console.WriteLine($"Package config source: '{mpc.PackageConfigSource}'");
                Console.WriteLine($"Destination dir: '{mpc.Destination}'");
                Console.WriteLine($"Package id: '{mpc.PackageConfig.Name}'");
                var mp = new MainProcess(env);
                await mp.Do(mpc);
            });

            return app;
        }

        private static ValidationResult GetValidationResult(string val)
        {
            if (File.Exists(val))
                return ValidationResult.Success;
            return new ValidationResult($"Cannot find file: {val}");
        }
    }
}