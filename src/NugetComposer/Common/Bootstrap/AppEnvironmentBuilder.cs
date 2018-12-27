using System;
using System.IO;
using System.Reflection;

namespace NugetComposer.Common.Bootstrap
{
    public class AppEnvironmentBuilder
    {
        private const string _DEV_DIR = "dev";
        private const string _GIT_DIR = ".git";
        private const string _DEV_FILE = "dev.json";
        private static readonly AppEnvironmentBuilder _instance = new AppEnvironmentBuilder();

        private static AppEnvironment _appEnvironmentValue;
        private static readonly object _padlock = new object();

        static AppEnvironmentBuilder()
        {
        }

        private AppEnvironmentBuilder()
        {
        }

        // ReSharper disable once ConvertToAutoProperty
        public static AppEnvironmentBuilder Instance => _instance;


        public AppEnvironment GetAppEnvironment()
        {
            if (_appEnvironmentValue == null)
                lock (_padlock)
                {
                    var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                    if (_appEnvironmentValue != null) return _appEnvironmentValue;
                    _appEnvironmentValue = GetTemporaryRegistryImpl(asm);
                }

            return _appEnvironmentValue;
        }

        private AppEnvironment GetTemporaryRegistryImpl(Assembly asm)
        {
            var res = new AppEnvironment();

            // main 
            res.AssemblyFilePath = new Uri(asm.CodeBase).LocalPath;
            res.ExeFileDir = Path.GetDirectoryName(res.AssemblyFilePath);
            res.CommandLineArgs = Environment.GetCommandLineArgs();


            // dev
            res.DevDir = FindDir(res.ExeFileDir, _DEV_DIR);
            var dv = GetDevSettings(res.DevDir);
            res.IsDeveloperMode = IsDeveloperMode(res.DevDir, dv);

            // root
            res.RootDir = GetRoot(res.ExeFileDir, res.DevDir, dv);

            // important dev dirs
            res.GitDir = FindDir(res.RootDir, _GIT_DIR);
            res.RepositoryRootDir = GetRepositoryRoot(res.GitDir);

      
            // others
            res.AssemblyName = asm.GetName().Name;
            res.AppVersion = AppVersionBuilder.Init(asm);
            res.MachineName = Environment.MachineName;
            res.Is64BitProcess = Environment.Is64BitProcess;
            res.ProcessorArchitecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");


#if DEBUG
            res.IsDebug = true;
#else
            res.IsDebug = false;
#endif

            return res;
        }


        private static string GetSimpleDir(string rootDir, string dirName, bool useShort)
        {
            return useShort ? Path.Combine(rootDir, dirName) : Path.Combine(rootDir, dirName, Environment.MachineName);
        }

        private static string GetDir(string rootDir, string dirName)
        {
            return Path.Combine(rootDir, dirName);
        }


        private static bool IsDeveloperMode(string devDir, DeveloperConfig dv)
        {
            return Directory.Exists(devDir) && dv.DevMode;
        }

        private static string GetRepositoryRoot(string gitDir)
        {
            var parentGit = string.IsNullOrEmpty(gitDir)
                ? string.Empty
                : new DirectoryInfo(gitDir)?.Parent?.FullName;

            return parentGit;
        }


        private string GetRoot(string appDir, string devDir, DeveloperConfig dv)
        {
            if (dv.DevMode)
            {
                var devAppDir = Path.Combine(devDir, dv.DevSubdir);
                Io.CreateDirIfNotExist(devAppDir);
                if (Directory.Exists(devAppDir)) return devAppDir;
            }

            return appDir;
        }


        private static DeveloperConfig GetDevSettings(string devDir)
        {
            var conf = new DeveloperConfig();
            try
            {
                if (!Directory.Exists(devDir)) return conf;
                var devConfig = Path.Combine(devDir, _DEV_FILE);
                if (!File.Exists(devConfig)) return conf;
                var json = File.ReadAllText(devConfig);
                conf = SimpleJson.SimpleJson.DeserializeObject<DeveloperConfig>(json);
            }
            catch (Exception e)
            {
                // ignore
            }

            return conf;
        }


        private static string FindDir(string startPath, string dirToFind)
        {
            var di = new DirectoryInfo(startPath);
            while (true)
            {
                var path = Path.Combine(di.FullName, dirToFind);
                if (Directory.Exists(path))
                    return path;

                if (di.Parent == null) return null;
                di = di.Parent;
            }
        }


        internal class DevSettings
        {
            public bool IsDeveloperMode { get; set; }
            public DeveloperConfig DeveloperConfig { get; set; }
        }
    }
}