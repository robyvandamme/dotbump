// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpSdk;

public class ReleaseWebServiceTests
{
    public class GetReleasesAsync
    {
        [Fact]
        public async Task Returns_Releases()
        {
            var loggerMock = new Mock<ILogger>();
            var service = new ReleaseWebService(loggerMock.Object);
            var result = (await service.GetReleasesAsync()).ToList();
            result.ShouldNotBeNull();
            result.ShouldNotBeEmpty();
        }
    }
}
