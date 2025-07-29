// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpSdk;

public class BumpSdkHandlerTests
{
    public class HandleAsync
    {
        [Fact]
        public async Task Returns_True_Result_For_New_Release()
        {
            var fileService = new Mock<ISdkFileService>();
            var releaseService = new Mock<IReleaseService>();

            fileService.Setup(service => service.GetCurrentSdkVersionFromFile(It.IsAny<string>()))
                .Returns(new Sdk("1.1.0", "disable"));

            var releaseFinderMock = new Mock<IReleaseFinder>();
            releaseFinderMock
                .Setup(finder => finder.TryFindNewRelease(
                    It.IsAny<Sdk>(),
                    It.IsAny<IReadOnlyList<Release>>(),
                    It.IsAny<BumpType>()))
                .Returns(new Release("1.0", "1.2.0", "eol", "lts"));

            var loggerMock = new Mock<ILogger>();

            var handler = new BumpSdkHandler(
                fileService.Object,
                releaseService.Object,
                releaseFinderMock.Object,
                loggerMock.Object);
            var result = await handler.HandleAsync(BumpType.Minor, "filepath");
            result.ShouldBe(new BumpSdkResult(true, "1.1.0", "1.2.0"));
        }

        [Fact]
        public async Task Returns_False_Result_For_No_Release()
        {
            var fileService = new Mock<ISdkFileService>();
            var releaseService = new Mock<IReleaseService>();

            fileService.Setup(service => service.GetCurrentSdkVersionFromFile(It.IsAny<string>()))
                .Returns(new Sdk("1.1.0", "disable"));

            var releaseFinderMock = new Mock<IReleaseFinder>();
            releaseFinderMock
                .Setup(finder => finder.TryFindNewRelease(
                    It.IsAny<Sdk>(),
                    It.IsAny<IReadOnlyList<Release>>(),
                    It.IsAny<BumpType>()))
                .Returns((Release?)null);

            var loggerMock = new Mock<ILogger>();

            var handler = new BumpSdkHandler(
                fileService.Object,
                releaseService.Object,
                releaseFinderMock.Object,
                loggerMock.Object);
            var result = await handler.HandleAsync(BumpType.Minor, "filepath");
            result.ShouldBe(new BumpSdkResult(false, "1.1.0", "1.1.0"));
        }
    }
}
