namespace Helpers.MagicVersionService
{
    public class MagicVersionStrategy
    {
        public MagicVersionStrategy(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public static MagicVersionStrategy Standard { get; } = new MagicVersionStrategy(nameof(Standard));

        public static MagicVersionStrategy PatchFromCounter { get; } =
            new MagicVersionStrategy(nameof(PatchFromCounter));

        public static MagicVersionStrategy PatchFromGitCommitsCurrentBranchFirstParent { get; } =
            new MagicVersionStrategy(nameof(PatchFromGitCommitsCurrentBranchFirstParent));
    }
}