// Copyright © Roby Van Damme.

using DotBump.Commands.BumpLanguage.DataModel;
using Shouldly;

namespace DotBump.Tests.Commands.BumpLanguage;

public class LanguageVersionTests
{
    public class TryParse
    {
        [Theory]
        [InlineData("12", 12, 0)]
        [InlineData("12.0", 12, 0)]
        [InlineData("7.3", 7, 3)]
        public void With_Numeric_Value_Returns_Parsed_Version(string value, int expectedMajor, int expectedMinor)
        {
            var result = LanguageVersion.TryParse(value, out var version);

            result.ShouldBeTrue();
            version.ShouldBe(new LanguageVersion(expectedMajor, expectedMinor));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("latest")]
        [InlineData("preview")]
        [InlineData("12.0.0")]
        [InlineData("12.")]
        [InlineData(".0")]
        [InlineData("$(LangVersion)")]
        public void With_Non_Numeric_Value_Returns_False(string value)
        {
            var result = LanguageVersion.TryParse(value, out _);

            result.ShouldBeFalse();
        }
    }

    public class FromSdkMajor
    {
        [Theory]
        [InlineData(5, 9, 0)]
        [InlineData(8, 12, 0)]
        [InlineData(10, 14, 0)]
        public void With_Sdk_Major_Returns_Offset_Language_Version(
            int sdkMajor,
            int expectedMajor,
            int expectedMinor)
        {
            var result = LanguageVersion.FromSdkMajor(sdkMajor);

            result.ShouldBe(new LanguageVersion(expectedMajor, expectedMinor));
        }
    }

    public class CompareTo
    {
        [Theory]
        [InlineData(13, 0, 14, 0, -1)]
        [InlineData(14, 0, 14, 0, 0)]
        [InlineData(14, 1, 14, 0, 1)]
        public void With_Versions_Returns_Expected_Ordering(
            int leftMajor,
            int leftMinor,
            int rightMajor,
            int rightMinor,
            int expectedSign)
        {
            var left = new LanguageVersion(leftMajor, leftMinor);
            var right = new LanguageVersion(rightMajor, rightMinor);

            Math.Sign(left.CompareTo(right)).ShouldBe(expectedSign);
        }
    }
}
