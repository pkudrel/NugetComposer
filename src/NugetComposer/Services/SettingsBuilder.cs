using System;
using System.IO;
using NugetComposer.Common.Bootstrap;
using NugetComposer.Feature.Arguments;
using NugetComposer.Models;

namespace NugetComposer.Services
{
    public class SettingsBuilder
    {
        public static Settings Create(AppEnvironment env, Options options)
        {
            var res = new Settings();
            var config = TryReadConfig(env);
            var packageConfigPath = GetPackageConfigPath(env, options, config);
            var destination = GetDestination(env, options, config);

            var clean = options?.Clean ?? config?.Clean ?? Constants.DEFAULT_CLEAN;
            res.Destination = destination;
            res.PackageConfigFile = packageConfigPath;

            res.CleanExtraction = config?.CleanExtraction ?? clean;
            res.CleanDestination = config?.CleanDestination ?? clean;

            res.CleanAllAfter = config?.CleanAllAfter ?? clean ;
            res.CleanAllBefore = config?.CleanAllBefore ?? clean;
            res.CleanDownload = config?.CleanDownload ?? clean;

            return res;
        }

        private static string GetDestination(AppEnvironment env, Options options, Config config)
        {
            if (string.IsNullOrEmpty(options.Destination) == false) return options.Destination;
            if (string.IsNullOrEmpty(config.Destination) == false) return config.Destination;
            return Constants.DEFAULT_PACKAGE_DIR;
        }

        private static string GetPackageConfigPath(AppEnvironment env, Options options, Config config)
        {
            if (string.IsNullOrEmpty(options.PackageConfig) == false) return options.PackageConfig;
            if (string.IsNullOrEmpty(config.PackageConfigFile) == false) return config.PackageConfigFile;
            return Constants.DEFAULT_PACKAGE_CONFIG_FILE;
        }


        private static Config TryReadConfig(AppEnvironment env)
        {
            var path = Path.Combine(env.RootDir, Constants.APP_CONFIG_FILE);
            if (File.Exists(path) == false) return null;
            try
            {
                var json = File.ReadAllText(path);
                var conf = SimpleJson.SimpleJson.DeserializeObject<Config>(json);
                return conf;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(e.Message);
            }

            return null;
        }
    }
}