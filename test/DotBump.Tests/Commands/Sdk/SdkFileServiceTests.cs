// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.Sdk;
using DotBump.Common;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.Sdk;

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
            currentSdk.ShouldNotBeNull();
            currentSdk.Version.ShouldNotBeNullOrWhiteSpace();
            currentSdk.SemanticVersion.ShouldNotBeNull();
        }

        [Fact]
        public void With_Incorrect_FilePath_Throws_ArgumentException()
        {
            var filePath = Directory.GetCurrentDirectory() + "/NotData/global.json";
            var loggerMock = new Mock<ILogger>();
            var service = new SdkFileService(loggerMock.Object);
            Should.Throw<DotBumpException>(() => service.GetCurrentSdkVersionFromFile(filePath));
            filePath = "./global.json";
            Should.Throw<DotBumpException>(() => service.GetCurrentSdkVersionFromFile(filePath));
        }

        [Fact]
        public void With_Bad_Version_Data_Throws_ArgumentException()
        {
            var filePath = Directory.GetCurrentDirectory() + "/Data/bad-global.json";
            var loggerMock = new Mock<ILogger>();
            var service = new SdkFileService(loggerMock.Object);
            Should.Throw<ArgumentException>(() => service.GetCurrentSdkVersionFromFile(filePath));
        }
    }
}
