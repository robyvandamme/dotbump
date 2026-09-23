// Copyright © Roby Van Damme.

using DotBump.Commands;
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
        public void With_Minor_Type_Returns_New_Patch()
        {
            var currentSdk = new Sdk("1.1.0", "disable");
            var release = new Release("1.0", "1.1.14", "eol", false);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false);
            result.ShouldBe(release);
        }

        [Fact]
        public void With_Minor_Type_Returns_New_Minor()
        {
            var currentSdk = new Sdk("1.1.0", "disable");
            var release = new Release("1.0", "1.2.0", "eol", true);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false);
            result.ShouldBe(release);
        }

        [Fact]
        public void With_New_Major_And_Minor_Type_Returns_Null()
        {
            var currentSdk = new Sdk("1.1.0", "disable");
            var release = new Release("2.0", "2.2.0", "eol", false);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false);
            result.ShouldBe(null);
        }

        [Fact]
        public void With_Lower_Patch_And_Minor_Type_Returns_Null()
        {
            var currentSdk = new Sdk("1.1.205", "disable");
            var release = new Release("1.0", "1.1.105", "eol", false);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false);
            result.ShouldBe(null);
        }

        [Fact]
        public void With_Lower_Minor_And_Minor_Type_Returns_Null()
        {
            var currentSdk = new Sdk("1.1.205", "disable");
            var release = new Release("1.0", "1.0.105", "eol", false);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false);
            result.ShouldBe(null);
        }

        [Fact]
        public void With_Non_Security_Minor_And_SecurityOnly_True_Returns_Null()
        {
            var currentSdk = new Sdk("8.0.405", "disable");
            var release = new Release("8.0", "8.1.100", "active", false);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, true);
            result.ShouldBe(null);
        }

        [Fact]
        public void With_Security_Minor_And_SecurityOnly_True_Returns_Release()
        {
            var currentSdk = new Sdk("8.0.405", "disable");
            var release = new Release("8.0", "8.1.100", "active", true);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, true);
            result.ShouldBe(release);
        }

        [Fact]
        public void With_Non_Security_Patch_And_Minor_Type_And_SecurityOnly_True_Returns_Null()
        {
            var currentSdk = new Sdk("8.0.405", "disable");
            var release = new Release("8.0", "8.0.406", "active", false);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, true);
            result.ShouldBe(null);
        }

        [Fact]
        public void With_Security_Patch_And_Minor_Type_And_SecurityOnly_True_Returns_Release()
        {
            var currentSdk = new Sdk("8.0.405", "disable");
            var release = new Release("8.0", "8.0.406", "active", true);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, true);
            result.ShouldBe(release);
        }

        [Fact]
        public void With_Preview_Patch_And_Minor_Type_Returns_Release()
        {
            var currentSdk = new Sdk("10.0.100-preview.4.25258.110", "disable");
            var release = new Release("10.0", "10.0.100-preview.6.25358.103", "preview", false);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Minor;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false);
            result.ShouldBe(release);
        }

        [Fact]
        public void With_Patch_Type_Returns_New_Patch()
        {
            var currentSdk = new Sdk("1.1.0", "disable");
            var release = new Release("1.0", "1.1.14", "eol", false);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Patch;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false);
            result.ShouldBe(release);
        }

        [Fact]
        public void With_Lower_Patch_And_Patch_Type_Returns_Null()
        {
            var currentSdk = new Sdk("1.1.205", "disable");
            var release = new Release("1.0", "1.1.105", "eol", false);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Patch;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false);
            result.ShouldBe(null);
        }

        [Fact]
        public void With_New_Minor_And_Patch_Type_Returns_Null()
        {
            var currentSdk = new Sdk("1.1.0", "disable");
            var release = new Release("1.0", "1.2.0", "eol", false);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Patch;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false);
            result.ShouldBe(null);
        }

        [Fact]
        public void With_New_Major_And_Patch_Type_Returns_Null()
        {
            var currentSdk = new Sdk("1.1.0", "disable");
            var release = new Release("2.0", "2.2.0", "eol", false);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Patch;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false);
            result.ShouldBe(null);
        }

        [Fact]
        public void With_Non_Security_Patch_And_Patch_Type_And_SecurityOnly_True_Returns_Null()
        {
            var currentSdk = new Sdk("8.0.405", "disable");
            var release = new Release("8.0", "8.0.406", "active", false);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Patch;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, true);
            result.ShouldBe(null);
        }

        [Fact]
        public void With_Security_Patch_And_Patch_Type_And_SecurityOnly_True_Returns_Release()
        {
            var currentSdk = new Sdk("8.0.405", "disable");
            var release = new Release("8.0", "8.0.406", "active", true);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Patch;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, true);
            result.ShouldBe(release);
        }

        [Fact]
        public void With_Preview_Patch_And_Patch_Type_Returns_Release()
        {
            var currentSdk = new Sdk("10.0.100-preview.4.25258.110", "disable");
            var release = new Release("10.0", "10.0.100-preview.6.25358.103", "preview", false);
            var releases = new List<Release>() { release };
            var bumpType = BumpType.Patch;
            var loggerMock = new Mock<ILogger>();
            var finder = new ReleaseFinder(loggerMock.Object);
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false);
            result.ShouldBe(release);
        }
    }
}
