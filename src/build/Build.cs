using System;
using AbcVersion;
using AbcVersionTool;
using Helpers;
using Helpers.MagicVersionService;
using NuGet.Versioning;
using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.NuGet.NuGetTasks;
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

    Project MainProject =>
        Solution.GetProject("NugetComposer").NotNull();


    AbsolutePath SourceDir => RootDirectory / "src";
    AbsolutePath ToolsDir => RootDirectory / "tools";
    AbsolutePath ArtifactsDir => RootDirectory / "artifacts";
    AbsolutePath DevDir => RootDirectory / "dev";
    AbsolutePath TmpBuild => TemporaryDirectory / "build";
    AbsolutePath LibzPath => ToolsDir / "LibZ.Tool" / "tools" / "libz.exe";
    AbsolutePath NugetPath => ToolsDir / "nuget" / "nuget.exe";
    AbsolutePath SevenZipPath => ToolsDir / "7-Zip.CommandLine" / "tools" / "7za.exe";

    MagicVersion MagicVersion => MagicVersionFactory.Make(1, 0, 0,
        BuildCounter,
        MagicVersionStrategy.PatchFromGitCommitsCurrentBranchFirstParent,
        BuildDate,
        MachineName);

    Target AbcVersionTarget => _ => _
        .Executes(() =>
        {

            var v = AbcVersionFactory.Create();
            Logger.Info(v.InformationalVersion);
        });


    Target Information => _ => _
        .Executes(() =>
        {

          
            var b = MagicVersion;
            Logger.Info($"Host: {Host}");
            Logger.Info($"Version: {b.SemVersion}");
            Logger.Info($"Version: {b.InformationalVersion}");
            Logger.Info($"Version: {b.GitCommitsCurrentBranchFirstParent}");
            SetVariable("NUGET_EXE", NugetPath);
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
                           MainProject.Name;
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
                           MainProject.Name;
            var margeOut = TmpBuild / CommonDir.Merge /
                           MainProject.Name;

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
                           MainProject.Name;

            var readyOut = TmpBuild / CommonDir.Ready /
                           MainProject.Name;


            EnsureExistingDirectory(readyOut);
            CopyFile(margeOut / "NugetComposer.exe", readyOut / "nuget-composer.exe");
        });

    Target MakeNuget => _ => _
        .DependsOn(CopyToReady)
        .Executes(() =>
        {
            var nugetOut = TmpBuild / CommonDir.Nuget / MainProject.Name;

            var readyOut = TmpBuild / CommonDir.Ready /
                           MainProject.Name;

            var scaffoldDir = TmpBuild / "nuget-scaffold" / MainProject.Name;
            var scaffoldToolDir = scaffoldDir / "tools";


            EnsureExistingDirectory(scaffoldDir);
            EnsureExistingDirectory(nugetOut);
            EnsureExistingDirectory(readyOut);
            EnsureExistingDirectory(scaffoldToolDir);
            CopyDirectoryRecursively(readyOut, scaffoldToolDir);


            GlobFiles(SourceDir / "build" / "nuget", "*.nuspec")
                .ForEach(x => NuGetPack(s => s
                    .SetTargetPath(x)
                    .SetConfiguration(Configuration)
                    .SetVersion(MagicVersion.NugetVersion)
                    .SetProperty("currentyear", DateTime.Now.Year.ToString())
                    .SetBasePath(scaffoldDir)
                    .SetOutputDirectory(nugetOut)
                    .EnableNoPackageAnalysis()));
        });

    Target MakeZip => _ => _
        .DependsOn(MakeNuget)
        .Executes(() =>
        {
            var readyOut = TmpBuild / CommonDir.Ready / MainProject.Name;
            var zipOut = TmpBuild / CommonDir.Zip / MainProject.Name;
            EnsureExistingDirectory(zipOut);
            var filename = $"{MainProject.Name}-{MagicVersion.SemVersion}.zip";
            var zipFullOut = zipOut / filename;
            var process = ProcessTasks.StartProcess(SevenZipPath, $" a {zipFullOut} .\\*", readyOut);
            process?.WaitForExit();

            
        });

    public static int Main() => Execute<Build>(x => x.AbcVersionTarget);
}