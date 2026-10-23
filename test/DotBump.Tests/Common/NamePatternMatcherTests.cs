// Copyright © Roby Van Damme.

using DotBump.Common;
using Shouldly;

namespace DotBump.Tests.Common;

public class NamePatternMatcherTests
{
    public class TryMatch
    {
        [Fact]
        public void With_Exact_Pattern_Matches_Case_Insensitively()
        {
            var matcher = NamePatternMatcher.Create(["Microsoft.Extensions.Logging"]);

            var result = matcher.TryMatch("microsoft.extensions.logging", out var matchedPattern);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBeTrue(),
                () => matchedPattern.ShouldBe("Microsoft.Extensions.Logging"));
        }

        [Fact]
        public void With_Wildcard_Pattern_Matches_Suffixed_Id()
        {
            var matcher = NamePatternMatcher.Create(["Microsoft.*"]);

            var result = matcher.TryMatch("Microsoft.Extensions.Logging", out var matchedPattern);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBeTrue(),
                () => matchedPattern.ShouldBe("Microsoft.*"));
        }

        [Fact]
        public void With_Wildcard_Pattern_Does_Not_Match_Unanchored_Id()
        {
            var matcher = NamePatternMatcher.Create(["Microsoft.*"]);

            var result = matcher.TryMatch("MyMicrosoft.Extensions", out var matchedPattern);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBeFalse(),
                () => matchedPattern.ShouldBeNull());
        }

        [Fact]
        public void With_Question_Mark_Matches_Exactly_One_Character()
        {
            var matcher = NamePatternMatcher.Create(["Package.?"]);

            var result = matcher.TryMatch("Package.A", out _);

            result.ShouldBeTrue();
        }

        [Fact]
        public void With_Question_Mark_Does_Not_Match_Multiple_Characters()
        {
            var matcher = NamePatternMatcher.Create(["Package.?"]);

            var result = matcher.TryMatch("Package.AB", out _);

            result.ShouldBeFalse();
        }

        [Fact]
        public void With_Regex_Special_Characters_Treats_Them_Literally()
        {
            var matcher = NamePatternMatcher.Create(["My.Package[1]*"]);

            var result = matcher.TryMatch("My.Package[1].Core", out _);

            result.ShouldBeTrue();
        }

        [Fact]
        public void With_Regex_Special_Characters_Does_Not_Treat_Dot_As_Any_Character()
        {
            var matcher = NamePatternMatcher.Create(["My.Package"]);

            var result = matcher.TryMatch("MyXPackage", out _);

            result.ShouldBeFalse();
        }

        [Fact]
        public void With_Comma_Separated_Patterns_Matches_Any_Value()
        {
            var matcher = NamePatternMatcher.Create(["Microsoft.*, System.*"]);

            var result = matcher.TryMatch("System.Text.Json", out var matchedPattern);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBeTrue(),
                () => matchedPattern.ShouldBe("System.*"));
        }

        [Fact]
        public void With_Multiple_Patterns_Returns_First_Match()
        {
            var matcher = NamePatternMatcher.Create(["Microsoft.*", "Microsoft.Extensions.*"]);

            var result = matcher.TryMatch("Microsoft.Extensions.Logging", out var matchedPattern);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBeTrue(),
                () => matchedPattern.ShouldBe("Microsoft.*"));
        }

        [Fact]
        public void With_No_Patterns_Does_Not_Match()
        {
            var matcher = NamePatternMatcher.Create(null);

            var result = matcher.TryMatch("MyPackage", out var matchedPattern);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBeFalse(),
                () => matchedPattern.ShouldBeNull());
        }

        [Fact]
        public void With_Empty_Name_Throws_ArgumentException()
        {
            var matcher = NamePatternMatcher.Create(["MyPackage"]);

            Should.Throw<ArgumentException>(() => matcher.TryMatch(" ", out _));
        }
    }

    public class Normalize
    {
        [Fact]
        public void With_Null_Returns_Empty()
        {
            var result = NamePatternMatcher.Normalize(null);

            result.ShouldBeEmpty();
        }

        [Fact]
        public void With_Comma_Separated_Values_Splits_Trims_And_Deduplicates()
        {
            var result = NamePatternMatcher.Normalize([" Microsoft.* , System.* , microsoft.* "]);

            result.ShouldBe(["Microsoft.*", "System.*"]);
        }

        [Fact]
        public void With_Empty_Segments_Removes_Them()
        {
            var result = NamePatternMatcher.Normalize(["Microsoft.*,,", " , System.*"]);

            result.ShouldBe(["Microsoft.*", "System.*"]);
        }
    }
}
