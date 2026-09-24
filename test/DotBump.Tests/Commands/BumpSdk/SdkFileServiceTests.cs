// Copyright © Roby Van Damme.

using DotBump.Commands.BumpSdk;
using DotBump.Common;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpSdk;

public class SdkFileServiceTests
{
    public class GetCurrentSdkVersionFromFile
    {
        [Fact]
        public void With_Correct_FilePath_Returns_Sdk_Information()
        {
            var filePath = Directory.GetCurrentDirectory() + "/Data/global.json";
            var loggerMock = new Mock<ILogger>();
            var service = new SdkFileService(loggerMock.Object);
            var currentSdk = service.GetCurrentSdkVersionFromFile(filePath);

            currentSdk.ShouldSatisfyAllConditions(
                () => currentSdk.ShouldNotBeNull(),
                () => currentSdk.Version.ShouldNotBeNullOrWhiteSpace(),
                () => currentSdk.SemanticVersion.ShouldNotBeNull());
        }

        [Theory]
        [InlineData("/NotData/global.json")]
        [InlineData("./global.json")]
        public void With_Incorrect_FilePath_Throws_DotBumpException(string relativeOrSubPath)
        {
            var filePath = Directory.GetCurrentDirectory() + relativeOrSubPath;
            var loggerMock = new Mock<ILogger>();
            var service = new SdkFileService(loggerMock.Object);

            Should.Throw<DotBumpException>(() => service.GetCurrentSdkVersionFromFile(filePath));
        }

        [Fact]
        public void With_Bad_Version_Data_Returns_Zero_Version()
        {
            var filePath = Directory.GetCurrentDirectory() + "/Data/bad-global.json";
            var loggerMock = new Mock<ILogger>();
            var service = new SdkFileService(loggerMock.Object);
            var currentSdk = service.GetCurrentSdkVersionFromFile(filePath);

            currentSdk.ShouldSatisfyAllConditions(
                () => currentSdk.ShouldNotBeNull(),
                () => currentSdk.SemanticVersion.IsValid.ShouldBeFalse(),
                () => currentSdk.SemanticVersion.Major.ShouldBe(0),
                () => currentSdk.SemanticVersion.Minor.ShouldBe(0),
                () => currentSdk.SemanticVersion.Patch.ShouldBe(0));
        }
    }
}
