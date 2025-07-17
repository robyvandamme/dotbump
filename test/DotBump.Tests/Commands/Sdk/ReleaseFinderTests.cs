// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.Sdk;
using DotBump.Commands.Sdk.DataModel;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.Sdk;

public class ReleaseFinderTests
{
    public class TryFindNewRelease
    {
        [Fact]
        public void Finds_New_Patch()
        {
            var currentSdk = new DotBump.Commands.Sdk.DataModel.Sdk("1.1.0", "disable");
            var release = new Release("1.0", "1.1.14", "eol");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(release);
        }

        [Fact]
        public void Finds_New_Minor()
        {
            var currentSdk = new DotBump.Commands.Sdk.DataModel.Sdk("1.1.0", "disable");
            var release = new Release("1.0", "1.2.0", "eol");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(release);
        }

        [Fact]
        public void Ignores_New_Major()
        {
            var currentSdk = new DotBump.Commands.Sdk.DataModel.Sdk("1.1.0", "disable");
            var release = new Release("2.0", "2.2.0", "eol");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(null);
        }

        [Fact]
        public void Ignores_Lower_Patch()
        {
            var currentSdk = new DotBump.Commands.Sdk.DataModel.Sdk("1.1.205", "disable");
            var release = new Release("1.0", "1.1.105", "eol");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(null);
        }

        [Fact]
        public void Ignores_Lower_Minor()
        {
            var currentSdk = new DotBump.Commands.Sdk.DataModel.Sdk("1.1.205", "disable");
            var release = new Release("1.0", "1.0.105", "eol");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(null);
        }
    }
}
