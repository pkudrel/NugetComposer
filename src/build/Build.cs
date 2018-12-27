using System;
using Helpers;
using Helpers.MagicVersionService;
using NuGet.Common;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

class Build : NukeBuild
{
    [Parameter("Build counter from outside environment")] readonly int BuildCounter;

    readonly DateTime BuildDate = DateTime.UtcNow;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    

    [Solution("src/NugetComposer.sln")] readonly Solution Solution;

    Project CrdProject =>
        Solution.GetProject("NugetComposer").NotNull();


    AbsolutePath SourceDir => RootDirectory / "src";
    AbsolutePath ToolsDir => RootDirectory / "tools";
    AbsolutePath ArtifactsDir => RootDirectory / "artifacts";
    AbsolutePath DevDir => RootDirectory / "dev";
    AbsolutePath TmpBuild => TemporaryDirectory / "build";
    AbsolutePath LibzPath => ToolsDir / "LibZ.Tool" / "tools" / "libz.exe";
    AbsolutePath NugetPath => ToolsDir / "nuget" / "nuget.exe";

    MagicVersion MagicVersion => MagicVersionFactory.Make(1, 0, 0,
        BuildCounter,
        MagicVersionStrategy.PatchFromGitCommitsCurrentBranchFirstParent,
        BuildDate,
        MachineName);

    Target Information => _ => _
        .Executes(() =>
        {
            var b = MagicVersion;
            Logger.Info($"Host: {Host}");
            Logger.Info($"Version: {b.SemVersion}");
            Logger.Info($"Version: {b.InformationalVersion}");
            Logger.Info($"Version: {b.GitCommitsCurrentBranchFirstParent}");



            

            

        });


    Target CheckTools => _ => _
        .DependsOn(Information)
        .Executes(() =>
        {
           
            Downloader.DownloadIfNotExists("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe", NugetPath,
                "Nuget");
            var toolsNugetFile = ToolsDir / "packages.config";
            using (var process = ProcessTasks.StartProcess(
                NugetPath,
                $"install   {toolsNugetFile} -OutputDirectory {ToolsDir} -ExcludeVersion",
                SourceDir))
            {
                process.AssertWaitForExit();
                ControlFlow.AssertWarn(process.ExitCode == 0,
                    "Nuget restore report generation process exited with some errors.");
            }
        });

    Target Clean => _ => _
        .DependsOn(CheckTools)
        .Executes(() =>
        {
            EnsureExistingDirectory(TmpBuild);
            DeleteDirectories(GlobDirectories(TmpBuild, "**/*"));
            EnsureCleanDirectory(ArtifactsDir);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
        
            using (var process = ProcessTasks.StartProcess(
                NugetPath,
                $"restore  {Solution.Path}",
                SourceDir))
            {
                process.AssertWaitForExit();
                ControlFlow.AssertWarn(process.ExitCode == 0,
                    "Nuget restore report generation process exited with some errors.");
            }
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>

        {
            var buildOut = TmpBuild / CommonDir.Build /
                           CrdProject.Name;
            EnsureExistingDirectory(buildOut);

            MSBuild(s => s
                .SetTargetPath(Solution)
                .SetTargets("Rebuild")
                .SetOutDir(buildOut)
                .SetVerbosity(MSBuildVerbosity.Quiet)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(MagicVersion.AssemblyVersion)
                .SetFileVersion(MagicVersion.FileVersion)
                .SetInformationalVersion(MagicVersion.InformationalVersion)
                .SetMaxCpuCount(Environment.ProcessorCount)
                .SetNodeReuse(IsLocalBuild));
        });

    Target Marge => _ => _
        .DependsOn(Compile)
        .Executes(() =>

        {
            var buildOut = TmpBuild / CommonDir.Build /
                           CrdProject.Name;
            var margeOut = TmpBuild / CommonDir.Merge /
                           CrdProject.Name;

            EnsureExistingDirectory(margeOut);
            CopyDirectoryRecursively(buildOut, margeOut);

            using (var process = ProcessTasks.StartProcess(
                LibzPath,
                "inject-dll --assembly NugetComposer.exe --include *.dll --move",
                margeOut))
            {
                process.AssertWaitForExit();
                ControlFlow.AssertWarn(process.ExitCode == 0,
                    "Libz report generation process exited with some errors.");
            }
        });


    Target CopyToReady => _ => _
        .DependsOn(Marge)
        .Executes(() =>

        {
            var margeOut = TmpBuild / CommonDir.Merge /
                           CrdProject.Name;

            var readyOut = TmpBuild / CommonDir.Ready/
                           CrdProject.Name;


            EnsureExistingDirectory(readyOut);
            CopyFile(margeOut / "NugetComposer.exe", readyOut / "nuget-composer.exe");

        });


    public static int Main() => Execute<Build>(x => x.CopyToReady);
}