using System;
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.Tools.Git;
using Nuke.Common.Utilities;

namespace Helpers.MagicVersionService
{
    [PublicAPI]
    public static class MagicVersionFactory
    {
        static DateTime DateTimeNowUtc = DateTime.UtcNow;
        static LocalRunner GitLocalRunner = new LocalRunner(GitTasks.GitPath);
        static readonly MagicVersionGitSubData _gitData = GetGitSubData();

        public static MagicVersion Make() => Make(0, 0, 0, 0, MagicVersionStrategy.Standard, DateTime.UtcNow);

        public static MagicVersion Make(int buildCounter) =>
            Make(0, 0, 0, buildCounter, MagicVersionStrategy.Standard, DateTime.UtcNow);

        public static MagicVersion Make(int buildCounter, MagicVersionStrategy strategy) =>
            Make(0, 0, 0, buildCounter, strategy, DateTime.UtcNow);

        public static MagicVersion Make(int buildCounter, MagicVersionStrategy strategy, DateTime utcNow) =>
            Make(0, 0, 0, buildCounter, strategy, DateTime.UtcNow);

        public static MagicVersion Make(
            int major,
            int minor,
            int patch) =>
            Make(major, minor, patch, 0, MagicVersionStrategy.Standard, DateTime.UtcNow);

        public static MagicVersion Make(
            int major,
            int minor,
            int patch,
            DateTime utcNow) =>
            Make(major, minor, patch, 0, MagicVersionStrategy.Standard, utcNow);

        public static MagicVersion Make(
            int major,
            int minor,
            int patch,
            int buildCounter,
            MagicVersionStrategy strategy,
            DateTime utcNow,
            string env = null)
        {
            var localStrategy = strategy ?? MagicVersionStrategy.Standard;


            switch (localStrategy.Name)
            {
                case nameof(MagicVersionStrategy.Standard):
                    var simple1 = GetForStandard(major, minor, patch, buildCounter, utcNow, env);
                    return new MagicVersion(_gitData, simple1);
                case nameof(MagicVersionStrategy.PatchFromCounter):
                    var simple2 = GetForPatchFromCounter(major, minor, patch, buildCounter, utcNow, env);
                    return new MagicVersion(_gitData, simple2);

                case nameof(MagicVersionStrategy.PatchFromGitCommitsCurrentBranchFirstParent):
                    var simple3 =
                        GetForPatchFromGitCommitsCurrentBranchFirstParen(major, minor, patch, buildCounter, utcNow,env);
                    return new MagicVersion(_gitData, simple3);
                default:
                    return new MagicVersion(_gitData);
            }
        }

        static MagicVersionSimple GetForPatchFromGitCommitsCurrentBranchFirstParen(
            int major, 
            int minor, 
            int patch,
            int buildCounter, 
            DateTime utcNow, 
            string env) => new MagicVersionSimple(major, minor, _gitData.GitCommitsCurrentBranchFirstParent,
            string.Empty, buildCounter, utcNow, env);

        static MagicVersionSimple GetForPatchFromCounter(int major,
            int minor,
            int patch,
            int buildCounter,
            DateTime dateUtc,
            string env) =>
            new MagicVersionSimple(major, minor, buildCounter, string.Empty, buildCounter, dateUtc, env);

        static MagicVersionSimple GetForStandard(int major, int minor, int patch, int buildCounter, DateTime dateUtc,
            string env) =>
            new MagicVersionSimple(major, minor, patch, string.Empty, buildCounter, dateUtc, env);

        static int GetCommitNumberAll()
        {
            var path = NukeBuild.RootDirectory;
            var result = GitLocalRunner.Run("rev-list --all --count", path, logOutput: false);
            var text = result.Select(x => x.Text).Take(1).Join(Environment.NewLine);
            try
            {
                var number = int.Parse(text);
                return number;
            }
            catch (Exception)
            {
            }

            return -1;
        }

        static int GetCommitNumberCurrentBranch()
        {
            var path = NukeBuild.RootDirectory;
            var result = GitLocalRunner.Run("rev-list HEAD --count", path, logOutput: false);
            var text = result.Select(x => x.Text).Take(1).Join(Environment.NewLine);
            try
            {
                var number = int.Parse(text);
                return number;
            }
            catch (Exception)
            {
            }

            return -1;
        }

        /// <summary>
        /// https://stackoverflow.com/a/49567820
        /// Follow only the first parent commit upon seeing a merge commit.
        /// This option can give a better overview when viewing the evolution of a particular topic branch,
        /// because merges into a topic branch tend to be only about adjusting to updated upstream from time to time,
        /// and this option allows you to ignore the individual commits brought in to your history by such a merge.
        /// Cannot be combined with --bisect.
        /// </summary>
        /// <returns></returns>
        static int GetCommitNumberCurrentBranchFirstParent()
        {
            var path = NukeBuild.RootDirectory;
            var result = GitLocalRunner.Run("rev-list HEAD --count --first-parent", path, logOutput: false);
            var text = result.Select(x => x.Text).Take(1).Join(Environment.NewLine);
            try
            {
                var number = int.Parse(text);
                return number;
            }
            catch (Exception)
            {
            }

            return -1;
        }


        static string GetTimestamp()
        {
            var path = NukeBuild.RootDirectory;
            var result = GitLocalRunner.Run("log --max-count=1 --pretty=format:%cI HEAD", path, logOutput: false);
            var text = result.Select(x => x.Text).Take(1).Join(Environment.NewLine);
            var date = DateTime.Parse(text).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            return date;
        }


        static string GetGitHash()
        {
            var path = NukeBuild.RootDirectory;
            var result = GitLocalRunner.Run("log --max-count=1 --pretty=format:%H HEAD", path, logOutput: false);
            var hash = result.Select(x => x.Text).Take(1).Join(Environment.NewLine);
            return hash;
        }


        static MagicVersionGitSubData GetGitSubData()
        {
            var hash = GetGitHash();
            var commitNumber = GetCommitNumberAll();
            var commitNumberCurrentBranch = GetCommitNumberCurrentBranch();
            var commitNumberCurrentBranchFirstParent = GetCommitNumberCurrentBranchFirstParent();
            var branch = GetGitBranch();
            var ret = new MagicVersionGitSubData(hash, commitNumber, branch, commitNumberCurrentBranch,
                commitNumberCurrentBranchFirstParent);
            return ret;
        }

        public static string GetGitBranch()
        {
            var path = NukeBuild.RootDirectory;
            var result1 = GitLocalRunner.Run("rev-parse --abbrev-ref HEAD", path, logOutput: false);

            var result2 = result1.Select(x => x.Text).Take(1).Join(Environment.NewLine);
            if (result2.IndexOf("HEAD", StringComparison.OrdinalIgnoreCase) == -1) return result2;

            var result3 = GitTasks.Git("symbolic-ref --short -q HEAD", path, logOutput: false);
            var result4 = result3.Select(x => x.Text).Take(1).Join(Environment.NewLine);
            if (result4.IndexOf("HEAD", StringComparison.OrdinalIgnoreCase) == -1) return result2;

            return string.Empty;
        }
    }
}