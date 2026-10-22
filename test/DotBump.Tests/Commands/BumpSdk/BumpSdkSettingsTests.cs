// Copyright © Roby Van Damme.

using DotBump.Commands;
using DotBump.Commands.BumpSdk;
using Shouldly;

namespace DotBump.Tests.Commands.BumpSdk;

public class BumpSdkSettingsTests
{
    public class Validate
    {
        [Fact]
        public void With_Default_Settings_Returns_Success()
        {
            var settings = new BumpSdkSettings();

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Major_Bump_Type_Returns_Success()
        {
            var settings = new BumpSdkSettings { Type = BumpType.Major };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_PreRelease_True_Returns_Success()
        {
            var settings = new BumpSdkSettings { PreRelease = true };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }
    }
}
