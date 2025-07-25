// Copyright © 2025 Roby Van Damme.

using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;

namespace DotBump.Build;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution]
    readonly Solution Solution;

    readonly AbsolutePath ArtifactsDirectory = RootDirectory / "artifacts";

    readonly AbsolutePath TestReportsDirectory = RootDirectory / "artifacts" / "test-reports";

    [PathVariable]
    readonly Tool dotnet;

    [Parameter]
    readonly string NuGetApiKey;

    [Parameter]
    readonly string NuGetFeed;

    [Parameter]
    readonly bool PackAndPublish = true;

    Target RestoreTools => t => t
        .Executes(() =>
        {
            Log.Information("Restoring tools");
            DotNetTasks.DotNetToolRestore();
        });

    /// <summary>
    /// Sets the version properties for all the projects using the current GitVersion projected version.
    /// </summary>
    Target SetVersion => t => t
        .DependsOn(RestoreTools)
        .OnlyWhenStatic(() => PackAndPublish)
        .Executes(() =>
        {
            // var version = GitVersionTasks.GitVersion().Result.SemVer;
            // dotnet($"giversion"); // TODO: how can we get the correct version in here?

            // Log.Information("Setting version: {Version}", version);

            // NOTE: for some reason /updateprojectfiles only works (locally) when I add the verbosity argument...
            // GitVersionTasks.GitVersion($"path {Solution.Directory} /verbosity Normal /updateprojectfiles");

            dotnet($"giversion /updateprojectfiles");
        });

    Target Clean => t => t
        .Requires(() => Solution)
        .DependsOn(RestoreTools)
        .Executes(() =>
        {
            Log.Information("Cleaning solution: {Solution}", Solution.Path);
            DotNetTasks.DotNetClean(o => o.SetProject(Solution.Path));
        });

    Target Restore => t => t
        .Requires(() => Solution)
        .DependsOn(Clean)
        .Executes(() =>
        {
            Log.Information("Restoring solution: {Solution}", Solution.Path);
            DotNetTasks.DotNetRestore(o => o.SetProjectFile(Solution.Path));
        });

    Target Compile => t => t
        .Requires(() => Solution)
        .DependsOn(Restore)
        .DependsOn(SetVersion)
        .Triggers(RunTests)
        .Executes(() =>
        {
            Log.Information("Building solution: {Solution}", Solution.Path);
            DotNetTasks.DotNetBuild(o => o
                .SetProjectFile(Solution.Path)
                .SetConfiguration(Configuration));
        });

    Target RunTests => t => t
        .Requires(() => Solution)
        .Executes(() =>
        {
            Log.Information("Looking for tests in solution");
            var tests = Solution.AllProjects.Where(p => p.Name.Contains("Tests")).ToList();
            if (tests.Any())
            {
                var reportsDirectory = new DirectoryInfo(TestReportsDirectory);
                if (!reportsDirectory.Exists)
                {
                    reportsDirectory.Create();
                }
                else
                {
                    reportsDirectory.Delete(true);
                    reportsDirectory.Create();
                }

                foreach (var test in tests)
                {
                    Log.Information("Executing tests: {Project}", test.Path);
                    DotNetTasks.DotNetTest(o => o
                        .SetProjectFile(test.Path)
                        .EnableNoBuild()
                        .SetConfiguration(Configuration)
                        .SetDataCollector("XPlat Code Coverage")
                        .SetResultsDirectory(TestReportsDirectory)
                        .SetLoggers($"html;logfilename={TestReportsDirectory}/testResults.html"));
                }

                Log.Information("Creating coverage reports");
                dotnet(
                    $"reportgenerator -reports:{TestReportsDirectory}/*/*.xml -targetdir:{TestReportsDirectory}/coverage");
            }
            else
            {
                Log.Information("No tests found in solution");
            }
        });

    Target Pack => t => t
        .Triggers(Publish)
        .OnlyWhenStatic(() => PackAndPublish)
        .Executes(() =>
        {
            Log.Information("Packing...");
            DotNetTasks.DotNetPack(o => o
                .SetNoBuild(true)
                .SetConfiguration(Configuration)
                .SetProject($"{Solution.Directory}/src/DotBump/DotBump.csproj")
                .SetOutputDirectory(ArtifactsDirectory));
        });

    Target Publish => t => t
        // .Requires(() => NuGetFeed, () => NuGetApiKey)
        .Executes(() =>
        {
            // Log.Information("Publishing...");
            // var packagePath = $"{ArtifactsDirectory}/*.nupkg";
            //
            // if (!IsLocalBuild)
            // {
            //     // TODO: review - Ask copilot
            //     // For pushing we use a PAT for now since passing in the GITHUB_TOKEN did not work in initial testing.
            //     dotnet($"nuget push -s {NuGetFeed} -k {NuGetApiKey} {packagePath}");
            // }
            // else
            // {
            //     // push to the local package feed
            //     dotnet($"nuget push -s http://localhost:9500/v3/index.json {packagePath} --skip-duplicate");
            // }
        });
}
