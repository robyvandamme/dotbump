// Copyright © Roby Van Damme.

using System.Collections.ObjectModel;
using DotBump.Commands;
using DotBump.Commands.BumpDocker;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using DotBump.Tests.Commands.BumpDocker.Fakes;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpDocker;

public class BumpDockerHandlerTests
{
    private const string ReleaseMetadataBaseUrl =
        "https://builds.dotnet.microsoft.com/dotnet/release-metadata";

    public class HandleAsync
    {
        [Fact]
        public async Task With_Newer_Minor_Release_Updates_Tag_And_Saves()
        {
            ResetTempDirectory();
            WriteDockerfile("FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build\n");
            var handler = CreateHandler(CreateRelease("10.0", "10.0.101"));

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
            var handler = CreateHandler(CreateRelease("10.0", "10.0.100"));

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
                CreateRelease("10.0", "10.0.101"),
                CreateRelease("11.0", "11.0.100"));

            var report = await handler.HandleAsync(BumpType.Major, TempDirectory.AbsolutePath);

            report.Results.ShouldHaveSingleItem().NewVersion.ShouldBe("11.0.100");
            ReadDockerfile().ShouldContain("sdk:11.0.100");
        }

        [Fact]
        public async Task With_Full_PreRelease_Sdk_Version_Maps_To_Image_Tag()
        {
            ResetTempDirectory();
            WriteDockerfile("FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.1 AS build\n");
            var handler = CreateHandler(CreateRelease("11.0", "11.0.100-rc.3.26522.148"));

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
            var handler = CreateHandler(CreateRelease("11.0", "11.0.100-rc.3.26522.148"));

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.HasChanges.ShouldBeTrue();
            ReadDockerfile().ShouldBe(
                "FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3 AS publish\n");
        }

        [Fact]
        public async Task With_Mixed_Majors_And_Patch_Bump_Bumps_Each_Major_To_Its_Latest()
        {
            ResetTempDirectory();
            WriteDockerfile(
                "FROM mcr.microsoft.com/dotnet/sdk:8.0.404 AS legacy\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build\n");
            var handler = CreateHandler(
                CreateRelease("8.0", "8.0.427"),
                CreateRelease("10.0", "10.0.406"));

            var report = await handler.HandleAsync(BumpType.Patch, TempDirectory.AbsolutePath);

            report.Results.ShouldSatisfyAllConditions(
                () => report.Results.ShouldContain(result =>
                    result.OldVersion == "8.0.404" && result.NewVersion == "8.0.427"),
                () => report.Results.ShouldContain(result =>
                    result.OldVersion == "10.0.403" && result.NewVersion == "10.0.406"));

            ReadDockerfile().ShouldBe(
                "FROM mcr.microsoft.com/dotnet/sdk:8.0.427 AS legacy\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.406 AS build\n");
        }

        [Fact]
        public async Task With_Mixed_Majors_And_Minor_Bump_Bumps_Each_Major_Independently()
        {
            ResetTempDirectory();
            WriteDockerfile(
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.1 AS publish\n");
            var handler = CreateHandler(
                CreateRelease("10.0", "10.0.406"),
                CreateRelease("11.0", "11.0.100-rc.3.26522.148"));

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.Results.ShouldSatisfyAllConditions(
                () => report.Results.ShouldContain(result =>
                    result.OldVersion == "10.0.403" && result.NewVersion == "10.0.406"),
                () => report.Results.ShouldContain(result =>
                    result.OldVersion == "11.0.100-rc.1" && result.NewVersion == "11.0.100-rc.3"));

            ReadDockerfile().ShouldBe(
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.406 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3 AS publish\n");
        }

        [Fact]
        public async Task With_Mixed_Majors_And_Major_Bump_Moves_All_To_Latest()
        {
            ResetTempDirectory();
            WriteDockerfile(
                "FROM mcr.microsoft.com/dotnet/sdk:8.0.404 AS legacy\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build\n");
            var handler = CreateHandler(
                CreateRelease("8.0", "8.0.427"),
                CreateRelease("10.0", "10.0.406"),
                CreateRelease("11.0", "11.0.100"));

            var report = await handler.HandleAsync(BumpType.Major, TempDirectory.AbsolutePath);

            report.Results.ShouldAllBe(result => result.NewVersion == "11.0.100");
            ReadDockerfile().ShouldBe(
                "FROM mcr.microsoft.com/dotnet/sdk:11.0.100 AS legacy\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:11.0.100 AS build\n");
        }

        [Fact]
        public async Task With_Mixed_Majors_And_Major_Bump_With_PreRelease_Moves_All_To_Highest()
        {
            ResetTempDirectory();
            WriteDockerfile(
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.1 AS publish\n");
            var handler = CreateHandler(
                CreateRelease("10.0", "10.0.406"),
                CreateRelease("11.0", "11.0.100"),
                CreateRelease("12.0", "12.0.100-preview.1.26522.200"));

            var report = await handler.HandleAsync(
                BumpType.Major,
                TempDirectory.AbsolutePath,
                preRelease: true);

            report.Results.ShouldAllBe(result => result.NewVersion == "12.0.100-preview.1");
            ReadDockerfile().ShouldBe(
                "FROM mcr.microsoft.com/dotnet/sdk:12.0.100-preview.1 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:12.0.100-preview.1 AS publish\n");
        }

        [Fact]
        public async Task With_Mixed_Minors_And_Patch_Bump_Keeps_Each_Minor()
        {
            ResetTempDirectory();
            WriteDockerfile(
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:10.1.100 AS publish\n");
            var handler = CreateHandler(
                CreateRelease("10.0", "10.0.406"),
                CreateRelease("10.1", "10.1.103"));

            var report = await handler.HandleAsync(BumpType.Patch, TempDirectory.AbsolutePath);

            report.Results.ShouldSatisfyAllConditions(
                () => report.Results.ShouldContain(result =>
                    result.OldVersion == "10.0.100" && result.NewVersion == "10.0.406"),
                () => report.Results.ShouldContain(result =>
                    result.OldVersion == "10.1.100" && result.NewVersion == "10.1.103"));

            ReadDockerfile().ShouldBe(
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.406 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:10.1.103 AS publish\n");
        }

        [Fact]
        public async Task With_Mixed_Patches_In_One_Major_Converges_To_The_Latest_Patch()
        {
            ResetTempDirectory();
            WriteDockerfile(
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.404 AS publish\n");
            var handler = CreateHandler(CreateRelease("10.0", "10.0.406"));

            var report = await handler.HandleAsync(BumpType.Patch, TempDirectory.AbsolutePath);

            report.Results.ShouldAllBe(result => result.NewVersion == "10.0.406");
            ReadDockerfile().ShouldBe(
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.406 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.406 AS publish\n");
        }

        [Fact]
        public async Task With_PreRelease_Current_And_Major_Bump_Prefers_Stable_Release()
        {
            ResetTempDirectory();
            WriteDockerfile("FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3 AS build\n");
            var handler = CreateHandler(
                CreateRelease("11.0", "11.0.100-rc.4.26522.200"),
                CreateRelease("11.0", "11.0.100"));

            var report = await handler.HandleAsync(BumpType.Major, TempDirectory.AbsolutePath);

            report.Results.ShouldHaveSingleItem().NewVersion.ShouldBe("11.0.100");
        }

        [Fact]
        public async Task With_PreRelease_Flag_And_Major_Bump_Selects_Highest_PreRelease()
        {
            ResetTempDirectory();
            WriteDockerfile("FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3 AS build\n");
            var handler = CreateHandler(
                CreateRelease("11.0", "11.0.100"),
                CreateRelease("11.1", "11.1.0-preview.1.26522.200"));

            var report = await handler.HandleAsync(
                BumpType.Major,
                TempDirectory.AbsolutePath,
                preRelease: true);

            report.Results.ShouldHaveSingleItem().NewVersion.ShouldBe("11.1.0-preview.1");
        }

        [Fact]
        public async Task With_AspNetCore_Image_Uses_AspNetCore_Runtime_Version()
        {
            ResetTempDirectory();
            WriteDockerfile("FROM mcr.microsoft.com/dotnet/aspnet:10.0.15 AS base\n");
            var releaseService = new FakeReleaseService(CreateRelease("10.0", "10.0.404"))
                .WithChannelReleases(
                    "10.0",
                    CreateChannelRelease("10.0.404", "10.0.15", "10.0.16"));
            var handler = CreateHandler(releaseService);

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            var result = report.Results.ShouldHaveSingleItem();
            report.ShouldSatisfyAllConditions(
                () => result.Id.ShouldBe("mcr.microsoft.com/dotnet/aspnet"),
                () => result.NewVersion.ShouldBe("10.0.16"),
                () => ReadDockerfile().ShouldContain("aspnet:10.0.16"));
        }

        [Fact]
        public async Task With_Runtime_Image_Uses_Runtime_Version()
        {
            ResetTempDirectory();
            WriteDockerfile("FROM mcr.microsoft.com/dotnet/runtime:10.0.14 AS base\n");
            var releaseService = new FakeReleaseService(CreateRelease("10.0", "10.0.404"))
                .WithChannelReleases(
                    "10.0",
                    CreateChannelRelease("10.0.404", "10.0.15", "10.0.16"));
            var handler = CreateHandler(releaseService);

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.Results.ShouldHaveSingleItem().NewVersion.ShouldBe("10.0.15");
            ReadDockerfile().ShouldContain("runtime:10.0.15");
        }

        [Fact]
        public async Task With_RuntimeDeps_Image_Uses_Runtime_Version()
        {
            ResetTempDirectory();
            WriteDockerfile("FROM mcr.microsoft.com/dotnet/runtime-deps:10.0.14 AS base\n");
            var releaseService = new FakeReleaseService(CreateRelease("10.0", "10.0.404"))
                .WithChannelReleases(
                    "10.0",
                    CreateChannelRelease("10.0.404", "10.0.15", "10.0.16"));
            var handler = CreateHandler(releaseService);

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.Results.ShouldHaveSingleItem().NewVersion.ShouldBe("10.0.15");
            ReadDockerfile().ShouldContain("runtime-deps:10.0.15");
        }

        [Fact]
        public async Task With_Mixed_Families_In_One_File_Bumps_Each_Family()
        {
            ResetTempDirectory();
            WriteDockerfile(
                "FROM mcr.microsoft.com/dotnet/aspnet:10.0.15 AS base\n" +
                "FROM mcr.microsoft.com/dotnet/runtime:10.0.14 AS tools\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.404 AS build\n");
            var releaseService = new FakeReleaseService(CreateRelease("10.0", "10.0.404"))
                .WithChannelReleases(
                    "10.0",
                    CreateChannelRelease("10.0.405", "10.0.15", "10.0.16"));
            var handler = CreateHandler(releaseService);

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.Results.Count.ShouldBe(3);
            ReadDockerfile().ShouldBe(
                "FROM mcr.microsoft.com/dotnet/aspnet:10.0.16 AS base\n" +
                "FROM mcr.microsoft.com/dotnet/runtime:10.0.15 AS tools\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.405 AS build\n");
        }

        [Fact]
        public async Task With_AspNetCore_Already_At_Target_Leaves_Tag_Unchanged()
        {
            ResetTempDirectory();
            const string content = "FROM mcr.microsoft.com/dotnet/aspnet:10.0.16 AS base\n";
            WriteDockerfile(content);
            var releaseService = new FakeReleaseService(CreateRelease("10.0", "10.0.404"))
                .WithChannelReleases(
                    "10.0",
                    CreateChannelRelease("10.0.404", "10.0.15", "10.0.16"));
            var handler = CreateHandler(releaseService);

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.ShouldSatisfyAllConditions(
                () => report.HasChanges.ShouldBeFalse(),
                () => ReadDockerfile().ShouldBe(content));
        }

        [Fact]
        public async Task With_Mixed_Majors_And_AspNetCore_Patch_Bump_Bumps_Each_Channel()
        {
            ResetTempDirectory();
            WriteDockerfile(
                "FROM mcr.microsoft.com/dotnet/aspnet:10.0.14 AS legacy\n" +
                "FROM mcr.microsoft.com/dotnet/aspnet:11.0.1 AS current\n");
            var releaseService = new FakeReleaseService(
                    CreateRelease("10.0", "10.0.404"),
                    CreateRelease("11.0", "11.0.100"))
                .WithChannelReleases("10.0", CreateChannelRelease("10.0.404", "10.0.15"))
                .WithChannelReleases("11.0", CreateChannelRelease("11.0.100", "11.0.2"));
            var handler = CreateHandler(releaseService);

            var report = await handler.HandleAsync(BumpType.Patch, TempDirectory.AbsolutePath);

            report.Results.ShouldSatisfyAllConditions(
                () => report.Results.ShouldContain(result =>
                    result.OldVersion == "10.0.14" && result.NewVersion == "10.0.15"),
                () => report.Results.ShouldContain(result =>
                    result.OldVersion == "11.0.1" && result.NewVersion == "11.0.2"));

            ReadDockerfile().ShouldBe(
                "FROM mcr.microsoft.com/dotnet/aspnet:10.0.15 AS legacy\n" +
                "FROM mcr.microsoft.com/dotnet/aspnet:11.0.2 AS current\n");
        }

        [Fact]
        public async Task With_Full_PreRelease_AspNetCore_Version_Maps_To_Image_Tag()
        {
            ResetTempDirectory();
            WriteDockerfile("FROM mcr.microsoft.com/dotnet/aspnet:10.0.0-preview.1 AS base\n");
            var releaseService = new FakeReleaseService(CreateRelease("10.0", "10.0.404"))
                .WithChannelReleases(
                    "10.0",
                    CreateChannelRelease("10.0.404", "10.0.15", "10.0.0-preview.2.25120.3"));
            var handler = CreateHandler(releaseService);

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.Results.ShouldHaveSingleItem().NewVersion.ShouldBe("10.0.0-preview.2");
            ReadDockerfile().ShouldContain("aspnet:10.0.0-preview.2");
        }

        [Fact]
        public async Task With_AspNetCore_Release_Missing_Version_Adds_Warning()
        {
            ResetTempDirectory();
            const string content = "FROM mcr.microsoft.com/dotnet/aspnet:10.0.14 AS base\n";
            WriteDockerfile(content);
            var releaseService = new FakeReleaseService(CreateRelease("10.0", "10.0.404"))
                .WithChannelReleases(
                    "10.0",
                    new ChannelRelease(
                        Security: false,
                        Sdks: null,
                        ReleaseVersion: "10.0.15",
                        Sdk: new ChannelComponent("10.0.404"),
                        Runtime: new ChannelComponent("10.0.15")));
            var handler = CreateHandler(releaseService);

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.ShouldSatisfyAllConditions(
                () => report.HasChanges.ShouldBeFalse(),
                () => report.Warnings.ShouldHaveSingleItem().ShouldContain("does not contain a supported version"),
                () => ReadDockerfile().ShouldBe(content));
        }

        [Fact]
        public async Task With_Channel_History_Failure_Adds_Warning_And_Skips_Channel()
        {
            ResetTempDirectory();
            WriteDockerfile(
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS legacy\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:11.0.100 AS build\n");
            var releaseService = new FakeReleaseService(
                    CreateRelease("10.0", "10.0.101"),
                    CreateRelease("11.0", "11.0.101"))
                .WithChannelReleases("11.0", CreateChannelRelease("11.0.101", "11.0.1"))
                .WithChannelReleasesException("10.0", new DotBumpException("boom"));
            var handler = CreateHandler(releaseService);

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.ShouldSatisfyAllConditions(
                () => report.Warnings.ShouldHaveSingleItem().ShouldContain("could not be read"),
                () => report.Results.ShouldContain(result =>
                    result.OldVersion == "11.0.100" && result.NewVersion == "11.0.101"),
                () => ReadDockerfile().ShouldBe(
                    "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS legacy\n" +
                    "FROM mcr.microsoft.com/dotnet/sdk:11.0.101 AS build\n"));
        }

        [Fact]
        public async Task With_Channel_Missing_From_Index_Adds_Warning_And_Leaves_Tags()
        {
            ResetTempDirectory();
            const string content = "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build\n";
            WriteDockerfile(content);
            var releaseService = new FakeReleaseService(CreateRelease("11.0", "11.0.100"));
            var handler = CreateHandler(releaseService);

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.ShouldSatisfyAllConditions(
                () => report.HasChanges.ShouldBeFalse(),
                () => report.Warnings.ShouldHaveSingleItem().ShouldContain("does not list that channel"),
                () => ReadDockerfile().ShouldBe(content));
        }

        [Fact]
        public async Task With_Channel_Missing_From_Index_And_Major_Bump_Adds_Warning_And_Leaves_Tags()
        {
            ResetTempDirectory();
            const string content = "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build\n";
            WriteDockerfile(content);
            var releaseService = new FakeReleaseService(CreateRelease("11.0", "11.0.100"))
                .WithChannelReleases("11.0", CreateChannelRelease("11.0.100", "11.0.1"));
            var handler = CreateHandler(releaseService);

            var report = await handler.HandleAsync(BumpType.Major, TempDirectory.AbsolutePath);

            report.ShouldSatisfyAllConditions(
                () => report.HasChanges.ShouldBeFalse(),
                () => report.Warnings.ShouldContain(warning =>
                    warning.Contains("does not list that channel", StringComparison.Ordinal)),
                () => ReadDockerfile().ShouldBe(content));
        }

        [Fact]
        public async Task With_Channel_History_Failure_And_Major_Bump_Skips_Only_That_Channel()
        {
            ResetTempDirectory();
            WriteDockerfile(
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS legacy\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:11.0.100 AS build\n");
            var releaseService = new FakeReleaseService(
                    CreateRelease("10.0", "10.0.101"),
                    CreateRelease("11.0", "11.0.101"))
                .WithChannelReleases("11.0", CreateChannelRelease("11.0.101", "11.0.1"))
                .WithChannelReleasesException("10.0", new DotBumpException("boom"));
            var handler = CreateHandler(releaseService);

            var report = await handler.HandleAsync(BumpType.Major, TempDirectory.AbsolutePath);

            report.ShouldSatisfyAllConditions(
                () => report.Warnings.ShouldHaveSingleItem().ShouldContain("could not be read"),
                () => report.Results.ShouldContain(result =>
                    result.OldVersion == "11.0.100" && result.NewVersion == "11.0.101"),
                () => ReadDockerfile().ShouldBe(
                    "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS legacy\n" +
                    "FROM mcr.microsoft.com/dotnet/sdk:11.0.101 AS build\n"));
        }

        [Fact]
        public async Task With_Channel_History_Requested_Once_Per_Channel()
        {
            ResetTempDirectory();
            WriteDockerfile(
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.404 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/aspnet:10.0.15 AS base\n" +
                "FROM mcr.microsoft.com/dotnet/runtime:10.0.15 AS tools\n");
            var releaseService = new Mock<IReleaseService>();
            releaseService
                .Setup(service => service.GetReleasesAsync())
                .ReturnsAsync([CreateRelease("10.0", "10.0.404")]);
            releaseService
                .Setup(service => service.GetChannelReleasesAsync(It.IsAny<Uri>()))
                .ReturnsAsync(new ChannelReleases(new Collection<ChannelRelease>(
                    [CreateChannelRelease("10.0.405", "10.0.16")])));
            var handler = CreateHandler(releaseService.Object);

            await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            releaseService.Verify(
                service => service.GetChannelReleasesAsync(It.IsAny<Uri>()),
                Times.Once);
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
        public async Task With_No_Supported_Image_Returns_No_Changes()
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
            var handler = CreateHandler(CreateRelease("11.0", "11.0.101-custom.1"));

            var report = await handler.HandleAsync(BumpType.Minor, TempDirectory.AbsolutePath);

            report.ShouldSatisfyAllConditions(
                () => report.HasChanges.ShouldBeFalse(),
                () => report.Warnings.ShouldHaveSingleItem().ShouldContain("cannot be mapped"),
                () => ReadDockerfile().ShouldBe(content));
        }
    }

    private static Release CreateRelease(string channel, string sdk)
    {
        return new Release(channel, sdk, "lts", false)
        {
            ReleasesJson = $"{ReleaseMetadataBaseUrl}/{channel}/releases.json",
        };
    }

    private static ChannelRelease CreateChannelRelease(
        string sdk,
        string runtime,
        string? aspNetCore = null)
    {
        return new ChannelRelease(
            Security: false,
            Sdks: null,
            ReleaseVersion: runtime,
            Sdk: new ChannelComponent(sdk),
            Runtime: new ChannelComponent(runtime),
            AspNetCoreRuntime: new ChannelComponent(aspNetCore ?? runtime));
    }

    private static BumpDockerHandler CreateHandler(params Release[] releases)
    {
        var releaseService = new FakeReleaseService(releases);

        foreach (var group in releases.GroupBy(release => release.ChannelVersion))
        {
            releaseService.WithChannelReleases(
                group.Key,
                group
                    .Select(release => CreateChannelRelease(release.LatestSdk, release.LatestSdk))
                    .ToArray());
        }

        return CreateHandler(releaseService);
    }

    private static BumpDockerHandler CreateHandler(IReleaseService releaseService)
    {
        var logger = Mock.Of<ILogger>();

        return new BumpDockerHandler(
            new DockerfileService(logger),
            releaseService,
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

    private static LocalDirectory TempDirectory => new("./temp/docker-handler");

    private static void ResetTempDirectory()
    {
        TempDirectory.EnsureDirectoryDeleted();
        TempDirectory.EnsureDirectoryCreated();
    }
}
