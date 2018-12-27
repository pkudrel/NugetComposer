using System.IO;
using NugetComposer.Common.Bootstrap;
using NugetComposer.Models;

namespace NugetComposer.Feature.MainTask
{
    public class MainProcessSettingsBuilder
    {
        public static MainProcessSettings Create(AppEnvironment env, Settings setting)
        {
            var ret = new MainProcessSettings();

            ret.PackageConfigSource = GetPackageConfigSource(env, setting.PackageConfigFile);
            ret.PackageConfig = GetFromLocalFile(env, ret.PackageConfigSource);
            ret.Destination = Path.IsPathRooted(setting.Destination)
                ? Path.Combine(setting.Destination,ret.PackageConfig.Name)
                : Path.Combine(env.RootDir, setting.Destination, ret.PackageConfig.Name);
            ret.TmpExtraction = Path.Combine(env.RootDir, Constants.APP_TMP_EXTRACTION_DIR);
            ret.TmpDownload = Path.Combine(env.RootDir, Constants.APP_TMP_DOWNLOAD_DIR);

            ret.CleanDownload = setting.CleanDownload;
            ret.CleanExtraction = setting.CleanExtraction;
            ret.CleanDestination = setting.CleanDestination;

            ret.CleanAllBefore = setting.Clean;
            ret.CleanAllAfter = setting.Clean;

            return ret;
        }


        private static string GetPackageConfigSource(AppEnvironment env, string configFile)
        {
            var p = Path.IsPathRooted(configFile) ? configFile : Path.Combine(env.RootDir, configFile);
            return p;
        }


        private static PackageConfig GetFromLocalFile(AppEnvironment env, string source)
        {
            var json = File.ReadAllText(source);
            var pc = SimpleJson.SimpleJson.DeserializeObject<PackageConfig>(json);
            return pc;
        }
    }
}