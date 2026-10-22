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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, true, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, true, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, true, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, true, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, true, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, true, false);
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
            var result = finder.TryFindNewRelease(currentSdk, releases, bumpType, false, false);
            result.ShouldBe(release);
        }

        [Fact]
        public void With_Major_Type_And_Higher_Major_Returns_Release()
        {
            var currentSdk = new Sdk("8.0.405", "disable");
            var release = new Release("9.0", "9.0.200", "active", false);
            var releases = new List<Release>() { release };
            var finder = new ReleaseFinder(new Mock<ILogger>().Object);

            var result = finder.TryFindNewRelease(currentSdk, releases, BumpType.Major, false, false);

            result.ShouldBe(release);
        }

        [Fact]
        public void With_Major_Type_And_No_Higher_Major_Returns_New_Minor()
        {
            var currentSdk = new Sdk("8.0.405", "disable");
            var release = new Release("8.0", "8.0.500", "active", false);
            var releases = new List<Release>() { release };
            var finder = new ReleaseFinder(new Mock<ILogger>().Object);

            var result = finder.TryFindNewRelease(currentSdk, releases, BumpType.Major, false, false);

            result.ShouldBe(release);
        }

        [Fact]
        public void With_Major_Type_And_No_Newer_Version_Returns_Null()
        {
            var currentSdk = new Sdk("9.0.200", "disable");
            var release = new Release("9.0", "9.0.200", "active", false);
            var releases = new List<Release>() { release };
            var finder = new ReleaseFinder(new Mock<ILogger>().Object);

            var result = finder.TryFindNewRelease(currentSdk, releases, BumpType.Major, false, false);

            result.ShouldBeNull();
        }

        [Fact]
        public void With_Major_Type_Returns_Highest_Release_Regardless_Of_Order()
        {
            var currentSdk = new Sdk("8.0.405", "disable");
            var release9 = new Release("9.0", "9.0.200", "active", false);
            var release10 = new Release("10.0", "10.0.100", "active", false);
            var releases = new List<Release>() { release9, release10 };
            var finder = new ReleaseFinder(new Mock<ILogger>().Object);

            var result = finder.TryFindNewRelease(currentSdk, releases, BumpType.Major, false, false);

            result.ShouldBe(release10);
        }

        [Fact]
        public void With_Major_Type_And_Stable_Current_Ignores_PreRelease_Release()
        {
            var currentSdk = new Sdk("10.0.100", "disable");
            var release = new Release("11.0", "11.0.100-preview.1", "preview", false);
            var releases = new List<Release>() { release };
            var finder = new ReleaseFinder(new Mock<ILogger>().Object);

            var result = finder.TryFindNewRelease(currentSdk, releases, BumpType.Major, false, false);

            result.ShouldBeNull();
        }

        [Fact]
        public void With_Major_Type_And_PreRelease_Current_Prefers_Stable_Release()
        {
            var currentSdk = new Sdk("11.0.100-preview.1", "disable");
            var stableRelease = new Release("11.0", "11.0.100", "active", false);
            var previewRelease = new Release("12.0", "12.0.100-preview.1", "preview", false);
            var releases = new List<Release>() { previewRelease, stableRelease };
            var finder = new ReleaseFinder(new Mock<ILogger>().Object);

            var result = finder.TryFindNewRelease(currentSdk, releases, BumpType.Major, false, false);

            result.ShouldBe(stableRelease);
        }

        [Fact]
        public void With_Major_Type_And_PreRelease_Current_Only_PreRelease_Returns_PreRelease()
        {
            var currentSdk = new Sdk("11.0.100-preview.1", "disable");
            var release = new Release("12.0", "12.0.100-preview.1", "preview", false);
            var releases = new List<Release>() { release };
            var finder = new ReleaseFinder(new Mock<ILogger>().Object);

            var result = finder.TryFindNewRelease(currentSdk, releases, BumpType.Major, false, false);

            result.ShouldBe(release);
        }

        [Fact]
        public void With_Major_Type_And_SecurityOnly_True_Returns_Security_Release()
        {
            var currentSdk = new Sdk("8.0.405", "disable");
            var nonSecurityRelease = new Release("9.0", "9.0.200", "active", false);
            var securityRelease = new Release("8.0", "8.0.406", "active", true);
            var releases = new List<Release>() { nonSecurityRelease, securityRelease };
            var finder = new ReleaseFinder(new Mock<ILogger>().Object);

            var result = finder.TryFindNewRelease(currentSdk, releases, BumpType.Major, true, false);

            result.ShouldBe(securityRelease);
        }

        [Fact]
        public void With_Major_Type_And_PreRelease_True_And_Higher_PreRelease_Returns_PreRelease()
        {
            var currentSdk = new Sdk("8.0.405", "disable");
            var stableRelease = new Release("9.0", "9.0.200", "active", false);
            var previewRelease = new Release("10.0", "10.0.100-preview.1", "preview", false);
            var releases = new List<Release>() { stableRelease, previewRelease };
            var finder = new ReleaseFinder(new Mock<ILogger>().Object);

            var result = finder.TryFindNewRelease(currentSdk, releases, BumpType.Major, false, true);

            result.ShouldBe(previewRelease);
        }

        [Fact]
        public void With_Major_Type_And_PreRelease_True_And_Only_Stable_Returns_Highest_Stable()
        {
            var currentSdk = new Sdk("8.0.405", "disable");
            var release = new Release("9.0", "9.0.200", "active", false);
            var releases = new List<Release>() { release };
            var finder = new ReleaseFinder(new Mock<ILogger>().Object);

            var result = finder.TryFindNewRelease(currentSdk, releases, BumpType.Major, false, true);

            result.ShouldBe(release);
        }

        [Fact]
        public void With_Major_Type_And_PreRelease_True_And_Higher_Stable_Returns_Stable()
        {
            var currentSdk = new Sdk("8.0.405", "disable");
            var previewRelease = new Release("11.0", "11.0.100-preview.1", "preview", false);
            var stableRelease = new Release("12.0", "12.0.100", "active", false);
            var releases = new List<Release>() { previewRelease, stableRelease };
            var finder = new ReleaseFinder(new Mock<ILogger>().Object);

            var result = finder.TryFindNewRelease(currentSdk, releases, BumpType.Major, false, true);

            result.ShouldBe(stableRelease);
        }

        [Fact]
        public void With_Major_Type_And_PreRelease_True_And_GoLive_Release_Returns_PreRelease()
        {
            var currentSdk = new Sdk("10.0.100", "disable");
            var release = new Release("11.0", "11.0.100-rc.2", "go-live", false);
            var releases = new List<Release>() { release };
            var finder = new ReleaseFinder(new Mock<ILogger>().Object);

            var result = finder.TryFindNewRelease(currentSdk, releases, BumpType.Major, false, true);

            result.ShouldBe(release);
        }

        [Fact]
        public void With_Major_Type_And_PreRelease_True_And_SecurityOnly_True_Returns_Null_For_Non_Security_PreRelease()
        {
            var currentSdk = new Sdk("8.0.405", "disable");
            var release = new Release("10.0", "10.0.100-preview.1", "preview", false);
            var releases = new List<Release>() { release };
            var finder = new ReleaseFinder(new Mock<ILogger>().Object);

            var result = finder.TryFindNewRelease(currentSdk, releases, BumpType.Major, true, true);

            result.ShouldBeNull();
        }
    }
}
