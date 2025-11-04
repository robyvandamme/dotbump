// Copyright © 2025 Roby Van Damme.

using DotBump.Common;
using Shouldly;

namespace DotBump.Tests.Common;

public class SemanticVersionTests
{
    [Fact]
    public void Valid_Version_Successfully_Parses()
    {
        var version = new SemanticVersion("1.2.3");

        version.Major.ShouldBe(1);
        version.Minor.ShouldBe(2);
        version.Patch.ShouldBe(3);
        version.IsPreRelease.ShouldBeFalse();
        version.PreRelease.ShouldBeNull();
    }

    [Fact]
    public void Valid_PreRelease_Version_Successfully_Parses()
    {
        var version = new SemanticVersion("1.2.3-beta.4");

        version.Major.ShouldBe(1);
        version.Minor.ShouldBe(2);
        version.Patch.ShouldBe(3);
        version.IsPreRelease.ShouldBeTrue();
        version.PreRelease.ShouldBe("beta.4");
    }

    [Fact]
    public void Complex_PreRelease_Version_Successfully_Parses()
    {
        var version = new SemanticVersion("10.0.0-preview.1.25080.5");

        version.Major.ShouldBe(10);
        version.Minor.ShouldBe(0);
        version.Patch.ShouldBe(0);
        version.IsPreRelease.ShouldBeTrue();
        version.PreRelease.ShouldBe("preview.1.25080.5");
    }

    [Fact]
    public void Null_Version_Throws_ArgumentException()
    {
        Should.Throw<ArgumentException>(() => new SemanticVersion(null!));
    }

    [Fact]
    public void Empty_Version_Throws_ArgumentException()
    {
        Should.Throw<ArgumentException>(() => new SemanticVersion(string.Empty));
    }

    [Fact]
    public void Whitespace_Version_Throws_ArgumentException()
    {
        Should.Throw<ArgumentException>(() => new SemanticVersion("   "));
    }

    [Theory]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    [InlineData("1.2.3.beta")]
    [InlineData("version1")]
    [InlineData("a.b.c")]
    public void Invalid_Format_Returns_0_Version(string version)
    {
        var semanticVersion = new SemanticVersion(version);
        semanticVersion.IsValid.ShouldBeFalse();
        semanticVersion.Major.ShouldBe(0);
        semanticVersion.Minor.ShouldBe(0);
        semanticVersion.Patch.ShouldBe(0);
    }

    public new class ToString
    {
        [Fact]
        public void Regular_Version_Returns_Correct_String()
        {
            var version = new SemanticVersion("1.2.3");

            var result = version.ToString();

            result.ShouldBe("1.2.3");
        }

        [Fact]
        public void PreRelease_Version_Returns_Correct_String()
        {
            var version = new SemanticVersion("1.2.3-beta.4");

            var result = version.ToString();

            result.ShouldBe("1.2.3-beta.4");
        }
    }

    public class CompareTo
    {
        [Fact]
        public void Greater_Major_Version_Returns_Positive()
        {
            var v1 = new SemanticVersion("2.0.0");
            var v2 = new SemanticVersion("1.0.0");

            v1.CompareTo(v2).ShouldBeGreaterThan(0);
            (v1 > v2).ShouldBeTrue();
        }

        [Fact]
        public void Greater_Minor_Version_Returns_Positive()
        {
            var v1 = new SemanticVersion("1.2.0");
            var v2 = new SemanticVersion("1.1.0");

            v1.CompareTo(v2).ShouldBeGreaterThan(0);
            (v1 > v2).ShouldBeTrue();
        }

        [Fact]
        public void Greater_Patch_Version_Returns_Positive()
        {
            var v1 = new SemanticVersion("1.0.2");
            var v2 = new SemanticVersion("1.0.1");

            v1.CompareTo(v2).ShouldBeGreaterThan(0);
            (v1 > v2).ShouldBeTrue();
        }

        [Fact]
        public void Equal_Versions_Returns_Zero()
        {
            var v1 = new SemanticVersion("1.2.3");
            var v2 = new SemanticVersion("1.2.3");

            v1.CompareTo(v2).ShouldBe(0);
            (v1 == v2).ShouldBeTrue();
        }

        [Fact]
        public void Release_Greater_Than_PreRelease_With_Same_Version()
        {
            var release = new SemanticVersion("1.0.0");
            var preRelease = new SemanticVersion("1.0.0-beta");

            release.CompareTo(preRelease).ShouldBeGreaterThan(0);
            (release > preRelease).ShouldBeTrue();
        }

        [Fact]
        public void Alpha_Less_Than_Beta_PreRelease_Version()
        {
            var alpha = new SemanticVersion("1.0.0-alpha");
            var beta = new SemanticVersion("1.0.0-beta");

            alpha.CompareTo(beta).ShouldBeLessThan(0);
            (alpha < beta).ShouldBeTrue();
        }

        [Fact]
        public void PreRelease_With_More_Identifiers_Is_Greater()
        {
            var v1 = new SemanticVersion("1.0.0-alpha.1");
            var v2 = new SemanticVersion("1.0.0-alpha");

            v1.CompareTo(v2).ShouldBeGreaterThan(0);
            (v1 > v2).ShouldBeTrue();
        }

        [Fact]
        public void PreRelease_Numeric_Identifiers_Compare_Numerically()
        {
            var v1 = new SemanticVersion("1.0.0-alpha.2");
            var v2 = new SemanticVersion("1.0.0-alpha.11");

            v1.CompareTo(v2).ShouldBeLessThan(0);
            (v1 < v2).ShouldBeTrue();
        }

        [Fact]
        public void PreRelease_Numeric_Identifier_Less_Than_Alphabetic()
        {
            var numeric = new SemanticVersion("1.0.0-1");
            var alphabetic = new SemanticVersion("1.0.0-alpha");

            numeric.CompareTo(alphabetic).ShouldBeLessThan(0);
            (numeric < alphabetic).ShouldBeTrue();
        }

        [Fact]
        public void Complex_PreRelease_Comparison_Works_Correctly()
        {
            var v1 = new SemanticVersion("10.0.0-preview.1.25080.5");
            var v2 = new SemanticVersion("10.0.0-preview.1.25080.4");
            var v3 = new SemanticVersion("10.0.0-preview.2.25080.1");

            (v1 > v2).ShouldBeTrue();
            (v1 < v3).ShouldBeTrue();
            (v2 < v3).ShouldBeTrue();
        }

        [Fact]
        public void Compare_With_Null_Returns_Positive()
        {
            var version = new SemanticVersion("1.0.0");

            version.CompareTo(null).ShouldBeGreaterThan(0);
        }
    }

    public class GetNewerVersion
    {
        [Fact]
        public void Returns_Correct_Version_When_First_Is_Newer()
        {
            var v1 = new SemanticVersion("2.0.0");
            var v2 = new SemanticVersion("1.0.0");

            var result = SemanticVersion.GetNewerVersion(v1, v2);

            result.ShouldBe(v1);
        }

        [Fact]
        public void Returns_Correct_Version_When_Second_Is_Newer()
        {
            var v1 = new SemanticVersion("1.0.0");
            var v2 = new SemanticVersion("1.1.0");

            var result = SemanticVersion.GetNewerVersion(v1, v2);

            result.ShouldBe(v2);
        }

        [Fact]
        public void Returns_Correct_Version_When_Both_Equal()
        {
            var v1 = new SemanticVersion("1.0.0");
            var v2 = new SemanticVersion("1.0.0");

            var result = SemanticVersion.GetNewerVersion(v1, v2);

            result.ShouldBe(v1);
            result.ShouldBe(v2); // Both should be equal
        }

        [Fact]
        public void Handles_PreRelease_Versions_Correctly()
        {
            var v1 = new SemanticVersion("1.0.0-beta.1");
            var v2 = new SemanticVersion("1.0.0-beta.2");
            var v3 = new SemanticVersion("1.0.0");

            SemanticVersion.GetNewerVersion(v1, v2).ShouldBe(v2);
            SemanticVersion.GetNewerVersion(v2, v3).ShouldBe(v3);
        }
    }
}
