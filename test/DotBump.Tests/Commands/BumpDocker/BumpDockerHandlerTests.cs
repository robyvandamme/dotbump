// Copyright © Roby Van Damme.

using DotBump.Commands;
using DotBump.Commands.BumpDocker;
using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Tests.Commands.BumpDocker.Fakes;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpDocker;

public class BumpDockerHandlerTests
{
    public class HandleAsync
    {
        [Fact]
        public async Task With_Newer_Minor_Release_Updates_Tag_And_Saves()
        {
            ResetTempDirectory();
            WriteDockerfile("FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build\n");
            var handler = CreateHandler(new Release("10.0", "10.0.101", "lts", false));

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            var result = report.Results.ShouldHaveSingleItem();
            report.ShouldSatisfyAllConditions(
                () => report.HasChanges.ShouldBeTrue(),
                () => result.Id.ShouldBe("mcr.microsoft.com/dotnet/sdk"),
                () => result.OldVersion.ShouldBe("10.0.100"),
                () => result.NewVersion.ShouldBe("10.0.101"),
                () => ReadDockerfile().ShouldContain("sdk:10.0.101 AS build"));
        }

        [Fact]
        public async Task With_No_Newer_Release_Leaves_Tag_Unchanged()
        {
            ResetTempDirectory();
            const string content = "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build\n";
            WriteDockerfile(content);
            var handler = CreateHandler(new Release("10.0", "10.0.100", "lts", false));

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.ShouldSatisfyAllConditions(
                () => report.HasChanges.ShouldBeFalse(),
                () => ReadDockerfile().ShouldBe(content));
        }

        [Fact]
        public async Task With_Major_Release_Updates_Tag()
        {
            ResetTempDirectory();
            WriteDockerfile("FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build\n");
            var handler = CreateHandler(
                new Release("10.0", "10.0.101", "lts", false),
                new Release("11.0", "11.0.100", "sts", false));

            var report = await handler.HandleAsync(BumpType.Major, TempDirectory.AbsolutePath);

            report.Results.ShouldHaveSingleItem().NewVersion.ShouldBe("11.0.100");
            ReadDockerfile().ShouldContain("sdk:11.0.100");
        }

        [Fact]
        public async Task With_Full_PreRelease_Sdk_Version_Maps_To_Image_Tag()
        {
            ResetTempDirectory();
            WriteDockerfile("FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.1 AS build\n");
            var handler = CreateHandler(new Release("11.0", "11.0.100-rc.3.26522.148", "go-live", false));

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.Results.ShouldHaveSingleItem().NewVersion.ShouldBe("11.0.100-rc.3");
            ReadDockerfile().ShouldContain("sdk:11.0.100-rc.3");
        }

        [Fact]
        public async Task With_Multiple_Occurrences_Does_Not_Downgrade()
        {
            ResetTempDirectory();
            WriteDockerfile(
                "FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.1 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3 AS publish\n");
            var handler = CreateHandler(new Release("11.0", "11.0.100-rc.3.26522.148", "go-live", false));

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.HasChanges.ShouldBeTrue();
            ReadDockerfile().ShouldBe(
                "FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3 AS publish\n");
        }

        [Fact]
        public async Task With_PreRelease_Current_And_Major_Bump_Prefers_Stable_Release()
        {
            ResetTempDirectory();
            WriteDockerfile("FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3 AS build\n");
            var handler = CreateHandler(
                new Release("11.0", "11.0.100-rc.4.26522.200", "go-live", false),
                new Release("11.0", "11.0.100", "sts", false));

            var report = await handler.HandleAsync(BumpType.Major, TempDirectory.AbsolutePath);

            report.Results.ShouldHaveSingleItem().NewVersion.ShouldBe("11.0.100");
        }

        [Fact]
        public async Task With_PreRelease_Flag_And_Major_Bump_Selects_Highest_PreRelease()
        {
            ResetTempDirectory();
            WriteDockerfile("FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3 AS build\n");
            var handler = CreateHandler(
                new Release("11.0", "11.0.100", "sts", false),
                new Release("11.1", "11.1.0-preview.1.26522.200", "go-live", false));

            var report = await handler.HandleAsync(
                BumpType.Major,
                TempDirectory.AbsolutePath,
                preRelease: true);

            report.Results.ShouldHaveSingleItem().NewVersion.ShouldBe("11.1.0-preview.1");
        }

        [Fact]
        public async Task With_Unsupported_Tag_Reports_Warning_And_No_Changes()
        {
            ResetTempDirectory();
            WriteDockerfile("FROM mcr.microsoft.com/dotnet/sdk:latest AS build\n");
            var handler = CreateHandler();

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.ShouldSatisfyAllConditions(
                () => report.HasChanges.ShouldBeFalse(),
                () => report.Results.ShouldBeEmpty(),
                () => report.Warnings.ShouldHaveSingleItem().ShouldContain("fully versioned"));
        }

        [Fact]
        public async Task With_No_Sdk_Image_Returns_No_Changes()
        {
            ResetTempDirectory();
            const string content = "FROM nginx:1.27.3\n";
            WriteDockerfile(content);
            var handler = CreateHandler();

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.ShouldSatisfyAllConditions(
                () => report.HasChanges.ShouldBeFalse(),
                () => report.Results.ShouldBeEmpty(),
                () => report.Warnings.ShouldBeEmpty(),
                () => ReadDockerfile().ShouldBe(content));
        }

        [Fact]
        public async Task With_Unmappable_Release_Adds_Warning_And_Leaves_Tag_Unchanged()
        {
            ResetTempDirectory();
            const string content = "FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.1 AS build\n";
            WriteDockerfile(content);
            var handler = CreateHandler(new Release("11.0", "11.0.101-custom.1", "go-live", false));

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.ShouldSatisfyAllConditions(
                () => report.HasChanges.ShouldBeFalse(),
                () => report.Warnings.ShouldHaveSingleItem().ShouldContain("cannot be mapped"),
                () => ReadDockerfile().ShouldBe(content));
        }

        private static BumpDockerHandler CreateHandler(params Release[] releases)
        {
            var logger = Mock.Of<ILogger>();

            return new BumpDockerHandler(
                new DockerfileService(logger),
                new FakeReleaseService(releases),
                new ReleaseFinder(logger),
                logger);
        }

        private static void WriteDockerfile(string content)
        {
            File.WriteAllText(Path.Combine(TempDirectory.AbsolutePath, "Dockerfile"), content);
        }

        private static string ReadDockerfile()
        {
            return File.ReadAllText(Path.Combine(TempDirectory.AbsolutePath, "Dockerfile"));
        }
    }

    private static LocalDirectory TempDirectory => new("./temp/docker-handler");

    private static void ResetTempDirectory()
    {
        TempDirectory.EnsureDirectoryDeleted();
        TempDirectory.EnsureDirectoryCreated();
    }
}
