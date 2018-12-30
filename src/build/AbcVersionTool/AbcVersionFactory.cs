using System;
using System.IO;
using AbcVersion;
using Newtonsoft.Json;
using Nuke.Common;
using Nuke.Common.IO;

namespace AbcVersionTool
{
    public static class AbcVersionFactory
    {
        const string _CONFIG_FILE = ".abcversion.json";
        static readonly PathConstruction.AbsolutePath _rootPath = NukeBuild.RootDirectory;
        static readonly PathConstruction.AbsolutePath _configPath = _rootPath / _CONFIG_FILE;
        static readonly DateTime DateTime = DateTime.UtcNow;

        public static AbcVersion Create()
        {
            var config = Read();

            var data = GitTool.GetAllGitData();
            var baseVersion = GetBaseAbcVersion();

            var ver = CalculateVersion(baseVersion, data, config);
            return ver;
        }

        static AbcVersion CalculateVersion(AbcVersion baseVersion, AbcVersionGitSubData data, Config config)
        {
            var branch = data.GitBranch;
            if (config.Branches.ContainsKey(branch))
            {
                var configBranch = config.Branches[branch];
                var firstParentNumber = GitTool.GetCommitNumberCurrentBranchFirstParent(configBranch.ParentSha);
                var sem = SemVersion.Parse(configBranch.Version);
                var patchNewValue = sem.Patch + firstParentNumber - 1;
                var pathNewValue2 = patchNewValue < 0 ? 0 : patchNewValue;
                var simple = new AbcVersionSimple(sem.Major, sem.Minor,
                    pathNewValue2,
                    baseVersion.Special, baseVersion.BuildCounter, baseVersion.DateTime, baseVersion.Env);
                return new AbcVersion(data, simple);
            }
            else
            {
                var firstParentNumber = data.GitCommitsCurrentBranchFirstParent;
                var simple = new AbcVersionSimple(baseVersion.Major, baseVersion.Minor,
                    firstParentNumber, baseVersion.Special, baseVersion.BuildCounter, baseVersion.DateTime,
                    baseVersion.Env);

                return new AbcVersion(data, simple);
            }
        }

        static AbcVersion GetBaseAbcVersion()
        {
            var data = GitTool.GetAllGitData();
            var simple = new AbcVersionSimple(0, 0, 0, "", 0, DateTime, "");
            var baseVersion = new AbcVersion(data, simple);
            return baseVersion;
        }

        static Config Read()
        {
            if (File.Exists(_configPath) == false)
            {
                Logger.Trace($"Config is not present: {_configPath}");
                return new Config();
            }

            try
            {
                var json = File.ReadAllText(_configPath);
                var o = JsonConvert.DeserializeObject<Config>(json);
                return o;
            }
            catch (Exception)
            {
                Logger.Error($"Config is not valid: {_configPath}");
                return new Config();
            }
        }
    }
}