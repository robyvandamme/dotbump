// Copyright © Roby Van Damme.

using DotBump.Commands;
using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpSdk;

public class BumpSdkHandlerTests
{
    private const string ChannelReleasesUrl =
        "https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json";

    public class HandleAsync
    {
        [Fact]
        public async Task With_New_Release_Returns_Report_With_Changes()
        {
            var fileService = new Mock<ISdkFileService>();
            var releaseService = new Mock<IReleaseService>();
            var securityReleaseEvaluator = new Mock<ISecurityReleaseEvaluator>();

            fileService.Setup(service => service.GetCurrentSdkVersionFromFile(It.IsAny<string>()))
                .Returns(new Sdk("1.1.0", "disable"));
            releaseService.Setup(service => service.GetReleasesAsync())
                .ReturnsAsync(new List<Release>());

            var releaseFinderMock = new Mock<IReleaseFinder>();
            releaseFinderMock
                .Setup(finder => finder.TryFindNewRelease(
                    It.IsAny<Sdk>(),
                    It.IsAny<IReadOnlyList<Release>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<bool>()))
                .Returns(new Release("1.0", "1.2.0", "eol", false));

            var loggerMock = new Mock<ILogger>();

            var handler = new BumpSdkHandler(
                fileService.Object,
                releaseService.Object,
                releaseFinderMock.Object,
                securityReleaseEvaluator.Object,
                loggerMock.Object);
            var result = await handler.HandleAsync(BumpType.Minor, "filepath", false, false);

            result.ShouldSatisfyAllConditions(
                () => result.HasChanges.ShouldBeTrue(),
                () => result.Results.First().OldVersion.ShouldBe("1.1.0"),
                () => result.Results.First().NewVersion.ShouldBe("1.2.0"));
        }

        [Fact]
        public async Task With_Major_Bump_Type_Forwards_Type_And_Applies_Release()
        {
            var fileService = new Mock<ISdkFileService>();
            var releaseService = new Mock<IReleaseService>();
            fileService.Setup(service => service.GetCurrentSdkVersionFromFile(It.IsAny<string>()))
                .Returns(new Sdk("8.0.405", "disable"));
            releaseService.Setup(service => service.GetReleasesAsync())
                .ReturnsAsync(new List<Release>());

            var releaseFinderMock = new Mock<IReleaseFinder>();
            releaseFinderMock
                .Setup(finder => finder.TryFindNewRelease(
                    It.IsAny<Sdk>(),
                    It.IsAny<IReadOnlyList<Release>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<bool>()))
                .Returns(new Release("9.0", "9.0.200", "active", false));

            var handler = new BumpSdkHandler(
                fileService.Object,
                releaseService.Object,
                releaseFinderMock.Object,
                new Mock<ISecurityReleaseEvaluator>().Object,
                new Mock<ILogger>().Object);

            var result = await handler.HandleAsync(BumpType.Major, "filepath", false, false);

            result.HasChanges.ShouldBeTrue();
            fileService.Verify(service => service.UpdateSdkVersion("8.0.405", "9.0.200", "filepath"), Times.Once);
            releaseFinderMock.Verify(
                finder => finder.TryFindNewRelease(
                    It.IsAny<Sdk>(),
                    It.IsAny<IReadOnlyList<Release>>(),
                    BumpType.Major,
                    false),
                Times.Once);
        }

        [Fact]
        public async Task With_PreRelease_True_Forwards_PreRelease_To_ReleaseFinder()
        {
            var fileService = new Mock<ISdkFileService>();
            var releaseService = new Mock<IReleaseService>();
            fileService.Setup(service => service.GetCurrentSdkVersionFromFile(It.IsAny<string>()))
                .Returns(new Sdk("8.0.405", "disable"));
            releaseService.Setup(service => service.GetReleasesAsync())
                .ReturnsAsync(new List<Release>());

            var releaseFinderMock = new Mock<IReleaseFinder>();
            releaseFinderMock
                .Setup(finder => finder.TryFindNewRelease(
                    It.IsAny<Sdk>(),
                    It.IsAny<IReadOnlyList<Release>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<bool>()))
                .Returns((Release?)null);

            var handler = new BumpSdkHandler(
                fileService.Object,
                releaseService.Object,
                releaseFinderMock.Object,
                new Mock<ISecurityReleaseEvaluator>().Object,
                new Mock<ILogger>().Object);

            await handler.HandleAsync(BumpType.Major, "filepath", false, true);

            releaseFinderMock.Verify(
                finder => finder.TryFindNewRelease(
                    It.IsAny<Sdk>(),
                    It.IsAny<IReadOnlyList<Release>>(),
                    BumpType.Major,
                    true),
                Times.Once);
        }

        [Fact]
        public async Task With_No_Release_Returns_Report_Without_Changes()
        {
            var fileService = new Mock<ISdkFileService>();
            var releaseService = new Mock<IReleaseService>();

            fileService.Setup(service => service.GetCurrentSdkVersionFromFile(It.IsAny<string>()))
                .Returns(new Sdk("1.1.0", "disable"));
            releaseService.Setup(service => service.GetReleasesAsync())
                .ReturnsAsync(new List<Release>());

            var releaseFinderMock = new Mock<IReleaseFinder>();
            releaseFinderMock
                .Setup(finder => finder.TryFindNewRelease(
                    It.IsAny<Sdk>(),
                    It.IsAny<IReadOnlyList<Release>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<bool>()))
                .Returns((Release?)null);

            var loggerMock = new Mock<ILogger>();

            var handler = new BumpSdkHandler(
                fileService.Object,
                releaseService.Object,
                releaseFinderMock.Object,
                new Mock<ISecurityReleaseEvaluator>().Object,
                loggerMock.Object);
            var result = await handler.HandleAsync(BumpType.Minor, "filepath", false, false);

            result.ShouldSatisfyAllConditions(
                () => result.HasChanges.ShouldBeFalse(),
                () => result.Results.First().OldVersion.ShouldBe("1.1.0"),
                () => result.Results.First().NewVersion.ShouldBe("1.1.0"));
        }

        [Fact]
        public async Task With_SecurityOnly_True_And_Security_Release_Does_Not_Fetch_Channel_Releases()
        {
            var fileService = new Mock<ISdkFileService>();
            var releaseService = new Mock<IReleaseService>();
            fileService.Setup(service => service.GetCurrentSdkVersionFromFile(It.IsAny<string>()))
                .Returns(new Sdk("10.0.402", "disable"));
            releaseService.Setup(service => service.GetReleasesAsync())
                .ReturnsAsync(new List<Release>());

            var releaseFinderMock = new Mock<IReleaseFinder>();
            releaseFinderMock
                .Setup(finder => finder.TryFindNewRelease(
                    It.IsAny<Sdk>(),
                    It.IsAny<IReadOnlyList<Release>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<bool>()))
                .Returns(new Release("10.0", "10.0.403", "active", true));

            var handler = new BumpSdkHandler(
                fileService.Object,
                releaseService.Object,
                releaseFinderMock.Object,
                new Mock<ISecurityReleaseEvaluator>().Object,
                new Mock<ILogger>().Object);

            var result = await handler.HandleAsync(BumpType.Minor, "filepath", true, false);

            result.HasChanges.ShouldBeTrue();
            releaseService.Verify(
                service => service.GetChannelReleasesAsync(It.IsAny<Uri>()),
                Times.Never);
        }

        [Fact]
        public async Task With_SecurityOnly_True_And_Missed_Security_Release_Bumps_To_Target()
        {
            var fileService = new Mock<ISdkFileService>();
            var releaseService = new Mock<IReleaseService>();
            var securityReleaseEvaluator = new Mock<ISecurityReleaseEvaluator>();

            fileService.Setup(service => service.GetCurrentSdkVersionFromFile(It.IsAny<string>()))
                .Returns(new Sdk("10.0.402", "disable"));
            releaseService.Setup(service => service.GetReleasesAsync())
                .ReturnsAsync(new List<Release>());

            var target = new Release("10.0", "10.0.404", "active", false) { ReleasesJson = ChannelReleasesUrl };
            var releaseFinderMock = new Mock<IReleaseFinder>();
            releaseFinderMock
                .Setup(finder => finder.TryFindNewRelease(
                    It.IsAny<Sdk>(),
                    It.IsAny<IReadOnlyList<Release>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<bool>()))
                .Returns(target);

            var channelReleases = new ChannelReleases([]);
            releaseService.Setup(service => service.GetChannelReleasesAsync(new Uri(ChannelReleasesUrl)))
                .ReturnsAsync(channelReleases);
            securityReleaseEvaluator
                .Setup(evaluator => evaluator.IncludesNewerSecurityRelease(
                    It.IsAny<SemanticVersion>(),
                    It.IsAny<SemanticVersion>(),
                    channelReleases))
                .Returns(true);

            var handler = new BumpSdkHandler(
                fileService.Object,
                releaseService.Object,
                releaseFinderMock.Object,
                securityReleaseEvaluator.Object,
                new Mock<ILogger>().Object);

            var result = await handler.HandleAsync(BumpType.Minor, "filepath", true, false);

            result.ShouldSatisfyAllConditions(
                () => result.HasChanges.ShouldBeTrue(),
                () => result.Results.First().NewVersion.ShouldBe("10.0.404"));
            fileService.Verify(service => service.UpdateSdkVersion("10.0.402", "10.0.404", "filepath"), Times.Once);
        }

        [Fact]
        public async Task With_SecurityOnly_True_And_No_Missed_Security_Release_Does_Not_Bump()
        {
            var fileService = new Mock<ISdkFileService>();
            var releaseService = new Mock<IReleaseService>();
            var securityReleaseEvaluator = new Mock<ISecurityReleaseEvaluator>();

            fileService.Setup(service => service.GetCurrentSdkVersionFromFile(It.IsAny<string>()))
                .Returns(new Sdk("10.0.402", "disable"));
            releaseService.Setup(service => service.GetReleasesAsync())
                .ReturnsAsync(new List<Release>());

            var target = new Release("10.0", "10.0.404", "active", false) { ReleasesJson = ChannelReleasesUrl };
            var releaseFinderMock = new Mock<IReleaseFinder>();
            releaseFinderMock
                .Setup(finder => finder.TryFindNewRelease(
                    It.IsAny<Sdk>(),
                    It.IsAny<IReadOnlyList<Release>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<bool>()))
                .Returns(target);

            var channelReleases = new ChannelReleases([]);
            releaseService.Setup(service => service.GetChannelReleasesAsync(new Uri(ChannelReleasesUrl)))
                .ReturnsAsync(channelReleases);
            securityReleaseEvaluator
                .Setup(evaluator => evaluator.IncludesNewerSecurityRelease(
                    It.IsAny<SemanticVersion>(),
                    It.IsAny<SemanticVersion>(),
                    channelReleases))
                .Returns(false);

            var handler = new BumpSdkHandler(
                fileService.Object,
                releaseService.Object,
                releaseFinderMock.Object,
                securityReleaseEvaluator.Object,
                new Mock<ILogger>().Object);

            var result = await handler.HandleAsync(BumpType.Minor, "filepath", true, false);

            result.HasChanges.ShouldBeFalse();
            fileService.Verify(
                service => service.UpdateSdkVersion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task With_SecurityOnly_True_And_Missing_Releases_Json_Throws_DotBumpException()
        {
            var fileService = new Mock<ISdkFileService>();
            var releaseService = new Mock<IReleaseService>();

            fileService.Setup(service => service.GetCurrentSdkVersionFromFile(It.IsAny<string>()))
                .Returns(new Sdk("10.0.402", "disable"));
            releaseService.Setup(service => service.GetReleasesAsync())
                .ReturnsAsync(new List<Release>());

            var releaseFinderMock = new Mock<IReleaseFinder>();
            releaseFinderMock
                .Setup(finder => finder.TryFindNewRelease(
                    It.IsAny<Sdk>(),
                    It.IsAny<IReadOnlyList<Release>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<bool>()))
                .Returns(new Release("10.0", "10.0.404", "active", false));

            var handler = new BumpSdkHandler(
                fileService.Object,
                releaseService.Object,
                releaseFinderMock.Object,
                new Mock<ISecurityReleaseEvaluator>().Object,
                new Mock<ILogger>().Object);

            await Should.ThrowAsync<DotBumpException>(
                () => handler.HandleAsync(BumpType.Minor, "filepath", true, false));
        }

        [Fact]
        public async Task With_SecurityOnly_True_And_Channel_Releases_Unavailable_Throws()
        {
            var fileService = new Mock<ISdkFileService>();
            var releaseService = new Mock<IReleaseService>();

            fileService.Setup(service => service.GetCurrentSdkVersionFromFile(It.IsAny<string>()))
                .Returns(new Sdk("10.0.402", "disable"));
            releaseService.Setup(service => service.GetReleasesAsync())
                .ReturnsAsync(new List<Release>());
            releaseService.Setup(service => service.GetChannelReleasesAsync(It.IsAny<Uri>()))
                .ThrowsAsync(new HttpRequestException("metadata unavailable"));

            var releaseFinderMock = new Mock<IReleaseFinder>();
            releaseFinderMock
                .Setup(finder => finder.TryFindNewRelease(
                    It.IsAny<Sdk>(),
                    It.IsAny<IReadOnlyList<Release>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<bool>()))
                .Returns(new Release("10.0", "10.0.404", "active", false) { ReleasesJson = ChannelReleasesUrl });

            var handler = new BumpSdkHandler(
                fileService.Object,
                releaseService.Object,
                releaseFinderMock.Object,
                new Mock<ISecurityReleaseEvaluator>().Object,
                new Mock<ILogger>().Object);

            await Should.ThrowAsync<HttpRequestException>(
                () => handler.HandleAsync(BumpType.Minor, "filepath", true, false));
            fileService.Verify(
                service => service.UpdateSdkVersion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }
    }
}
