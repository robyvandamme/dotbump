// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpSdk.DataModel;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpSdk;

public class ReleaseFinderTests
{
    public class TryFindNewRelease
    {
        [Fact]
        public void Finds_New_Patch_For_Minor_Type()
        {
            var currentSdk = new Sdk("1.1.0", "disable");
            var release = new Release("1.0", "1.1.14", "eol", "lts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(release);
        }

        [Fact]
        public void Finds_New_Minor_For_Minor_Type()
        {
            var currentSdk = new Sdk("1.1.0", "disable");
            var release = new Release("1.0", "1.2.0", "eol", "lts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(release);
        }

        [Fact]
        public void Ignores_New_Major_For_Minor_Type()
        {
            var currentSdk = new Sdk("1.1.0", "disable");
            var release = new Release("2.0", "2.2.0", "eol", "sts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(null);
        }

        [Fact]
        public void Ignores_Lower_Patch_For_Minor_Type()
        {
            var currentSdk = new Sdk("1.1.205", "disable");
            var release = new Release("1.0", "1.1.105", "eol", "lts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(null);
        }

        [Fact]
        public void Ignores_Lower_Minor_For_Minor_Type()
        {
            var currentSdk = new Sdk("1.1.205", "disable");
            var release = new Release("1.0", "1.0.105", "eol", "lts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(null);
        }

        [Fact]
        public void Finds_New_Patch_For_Patch_Type()
        {
            var currentSdk = new Sdk("1.1.0", "disable");
            var release = new Release("1.0", "1.1.14", "eol", "lts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Patch;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(release);
        }

        [Fact]
        public void Ignores_Lower_Patch_For_Patch_Type()
        {
            var currentSdk = new Sdk("1.1.205", "disable");
            var release = new Release("1.0", "1.1.105", "eol", "lts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Patch;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(null);
        }

        [Fact]
        public void Ignores_New_Minor_For_Patch_Type()
        {
            var currentSdk = new Sdk("1.1.0", "disable");
            var release = new Release("1.0", "1.2.0", "eol", "sts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Patch;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(null);
        }

        [Fact]
        public void Ignores_New_Major_For_Patch_Type()
        {
            var currentSdk = new Sdk("1.1.0", "disable");
            var release = new Release("2.0", "2.2.0", "eol", "sts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Patch;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(null);
        }

        [Fact]
        public void Ignores_New_Stable_For_Lts()
        {
            var currentSdk = new Sdk("8.0.0", "disable");
            var release = new Release("9.0", "9.0.105", "active", "sts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Lts;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(null);
        }

        [Fact]
        public void Ignores_New_Preview_For_Lts()
        {
            var currentSdk = new Sdk("8.0.0", "disable");
            var release = new Release("10.0", "10.0.100-preview.1.25120.13", "preview", "lts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Lts;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(null);
        }

        [Fact]
        public void Finds_New_Major_Lts()
        {
            var currentSdk = new Sdk("6.0.428", "disable");
            var release = new Release("8.0", "8.0.406", "active", "lts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Lts;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(release);
        }

        [Fact]
        public void Finds_New_Minor_Lts()
        {
            var currentSdk = new Sdk("8.0.406", "disable");
            var release = new Release("8.0", "8.1.0", "active", "lts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Lts;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(release);
        }

        [Fact]
        public void Finds_New_Patch_Lts()
        {
            var currentSdk = new Sdk("8.0.406", "disable");
            var release = new Release("8.0", "8.0.410", "active", "lts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Lts;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(release);
        }

        [Fact]
        public void Ignores_Current_Sdk_For_Lts()
        {
            var currentSdk = new Sdk("8.0.100", "disable");
            var release = new Release("8.0", "8.0.100", "active", "lts");
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Lts;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType);
            result.ShouldBe(null);
        }
    }
}
