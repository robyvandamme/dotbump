// Copyright © Roby Van Damme.

using DotBump.Commands.BumpDocker;
using Shouldly;

namespace DotBump.Tests.Commands.BumpDocker;

public class DotnetImageTagTests
{
    public class TryParse
    {
        [Theory]
        [InlineData("11.0.100")]
        [InlineData("11.0.100-rc.3")]
        [InlineData("11.0.100-preview.1")]
        [InlineData("10.0.403")]
        [InlineData("10.0.15")]
        public void With_Supported_Tag_Returns_True(string tag)
        {
            var result = DotnetImageTag.TryParse(tag, out var version);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBeTrue(),
                () => version.IsValid.ShouldBeTrue());
        }

        [Theory]
        [InlineData("latest")]
        [InlineData("11.0")]
        [InlineData("11.0.100-rc.3-noble")]
        [InlineData("11.0.100-rc.3-alpine3.24")]
        [InlineData("11.0.100.1")]
        [InlineData("11.0.100-servicing.25256.1")]
        public void With_Unsupported_Tag_Returns_False(string tag)
        {
            var result = DotnetImageTag.TryParse(tag, out _);

            result.ShouldBeFalse();
        }

        [Fact]
        public void With_Empty_Tag_Throws_ArgumentException()
        {
            Should.Throw<ArgumentException>(() => DotnetImageTag.TryParse(string.Empty, out _));
        }
    }

    public class TryMapFromVersion
    {
        [Theory]
        [InlineData("10.0.403", "10.0.403")]
        [InlineData("10.0.15", "10.0.15")]
        [InlineData("11.0.100-rc.3", "11.0.100-rc.3")]
        [InlineData("11.0.100-rc.3.26522.148", "11.0.100-rc.3")]
        [InlineData("11.0.100-preview.1.25120.13", "11.0.100-preview.1")]
        [InlineData("10.0.100-beta.2.25120.1", "10.0.100-beta.2")]
        public void With_Supported_Release_Version_Returns_Image_Tag(string releaseVersion, string expected)
        {
            var result = DotnetImageTag.TryMapFromVersion(releaseVersion);

            result.ShouldBe(expected);
        }

        [Theory]
        [InlineData("11.0.100-servicing.26522.148")]
        [InlineData("11.0.100-custom.1")]
        [InlineData("not-a-version")]
        public void With_Unsupported_Release_Version_Returns_Null(string releaseVersion)
        {
            var result = DotnetImageTag.TryMapFromVersion(releaseVersion);

            result.ShouldBeNull();
        }
    }
}
