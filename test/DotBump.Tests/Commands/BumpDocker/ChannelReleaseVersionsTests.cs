// Copyright © Roby Van Damme.

using DotBump.Commands.BumpDocker;
using DotBump.Commands.BumpSdk.DataModel;
using Shouldly;

namespace DotBump.Tests.Commands.BumpDocker;

public class ChannelReleaseVersionsTests
{
    public class GetVersion
    {
        [Fact]
        public void With_Sdk_And_Sdks_Returns_Sdk_Version()
        {
            var release = new ChannelRelease(
                Security: false,
                Sdks: [new ChannelSdk("10.0.115")],
                Sdk: new ChannelComponent("10.0.404"));

            var result = ChannelReleaseVersions.GetVersion(release, DotnetImageVersionSource.Sdk);

            result.ShouldNotBeNull().ToString().ShouldBe("10.0.404");
        }

        [Fact]
        public void With_No_Sdk_And_Sdks_Returns_Highest_Sdk_Version()
        {
            var release = new ChannelRelease(
                Security: false,
                Sdks: [new ChannelSdk("10.0.115"), new ChannelSdk("10.0.404")]);

            var result = ChannelReleaseVersions.GetVersion(release, DotnetImageVersionSource.Sdk);

            result.ShouldNotBeNull().ToString().ShouldBe("10.0.404");
        }

        [Fact]
        public void With_Runtime_Returns_Runtime_Version()
        {
            var release = new ChannelRelease(
                Security: false,
                Sdks: null,
                ReleaseVersion: "10.0.14",
                Runtime: new ChannelComponent("10.0.15"));

            var result = ChannelReleaseVersions.GetVersion(release, DotnetImageVersionSource.Runtime);

            result.ShouldNotBeNull().ToString().ShouldBe("10.0.15");
        }

        [Fact]
        public void With_No_Runtime_And_Release_Version_Returns_Release_Version()
        {
            var release = new ChannelRelease(
                Security: false,
                Sdks: null,
                ReleaseVersion: "10.0.15");

            var result = ChannelReleaseVersions.GetVersion(release, DotnetImageVersionSource.Runtime);

            result.ShouldNotBeNull().ToString().ShouldBe("10.0.15");
        }

        [Fact]
        public void With_AspNetCore_Returns_AspNetCore_Runtime_Version()
        {
            var release = new ChannelRelease(
                Security: false,
                Sdks: null,
                Runtime: new ChannelComponent("10.0.15"),
                AspNetCoreRuntime: new ChannelComponent("10.0.16"));

            var result = ChannelReleaseVersions.GetVersion(release, DotnetImageVersionSource.AspNetCore);

            result.ShouldNotBeNull().ToString().ShouldBe("10.0.16");
        }

        [Fact]
        public void With_Missing_AspNetCore_Returns_Null()
        {
            var release = new ChannelRelease(
                Security: false,
                Sdks: null,
                Runtime: new ChannelComponent("10.0.15"));

            var result = ChannelReleaseVersions.GetVersion(release, DotnetImageVersionSource.AspNetCore);

            result.ShouldBeNull();
        }

        [Fact]
        public void With_Invalid_Version_Returns_Null()
        {
            var release = new ChannelRelease(
                Security: false,
                Sdks: null,
                Runtime: new ChannelComponent("not-a-version"));

            var result = ChannelReleaseVersions.GetVersion(release, DotnetImageVersionSource.Runtime);

            result.ShouldBeNull();
        }

        [Fact]
        public void With_Null_Release_Throws_ArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() =>
                ChannelReleaseVersions.GetVersion(null!, DotnetImageVersionSource.Sdk));
        }
    }
}
