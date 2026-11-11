// Copyright © Roby Van Damme.

using DotBump.Commands;
using DotBump.Common;
using Shouldly;

namespace DotBump.Tests.Commands;

public class ReleaseSelectorTests
{
    public class TryFindNewest
    {
        [Fact]
        public void With_Minor_Type_Returns_Newest_In_Same_Major()
        {
            var result = ReleaseSelector.TryFindNewest(
                new SemanticVersion("10.0.403"),
                [Row("10.0.405"), Row("10.0.404"), Row("11.0.100")],
                row => row.Version,
                BumpType.Minor,
                false);

            result.ShouldBe(Row("10.0.405"));
        }

        [Fact]
        public void With_Minor_Type_And_New_Major_Returns_Null()
        {
            var result = ReleaseSelector.TryFindNewest(
                new SemanticVersion("10.0.403"),
                [Row("11.0.100")],
                row => row.Version,
                BumpType.Minor,
                false);

            result.ShouldBeNull();
        }

        [Fact]
        public void With_Patch_Type_Returns_Newest_In_Same_Major_And_Minor()
        {
            var result = ReleaseSelector.TryFindNewest(
                new SemanticVersion("10.0.403"),
                [Row("10.1.100"), Row("10.0.405")],
                row => row.Version,
                BumpType.Patch,
                false);

            result.ShouldBe(Row("10.0.405"));
        }

        [Fact]
        public void With_Patch_Type_And_Newer_Minor_Returns_Null()
        {
            var result = ReleaseSelector.TryFindNewest(
                new SemanticVersion("10.0.403"),
                [Row("10.1.100")],
                row => row.Version,
                BumpType.Patch,
                false);

            result.ShouldBeNull();
        }

        [Fact]
        public void With_Major_Type_Returns_Newest_Across_Majors()
        {
            var result = ReleaseSelector.TryFindNewest(
                new SemanticVersion("10.0.403"),
                [Row("10.0.406"), Row("11.0.100"), Row("12.0.100")],
                row => row.Version,
                BumpType.Major,
                false);

            result.ShouldBe(Row("12.0.100"));
        }

        [Fact]
        public void With_Major_Type_And_Stable_Current_Ignores_PreRelease()
        {
            var result = ReleaseSelector.TryFindNewest(
                new SemanticVersion("10.0.100"),
                [Row("11.0.100-preview.1")],
                row => row.Version,
                BumpType.Major,
                false);

            result.ShouldBeNull();
        }

        [Fact]
        public void With_Major_Type_And_PreRelease_Current_Prefers_Stable()
        {
            var result = ReleaseSelector.TryFindNewest(
                new SemanticVersion("11.0.100-preview.1"),
                [Row("11.0.100"), Row("12.0.100-preview.1")],
                row => row.Version,
                BumpType.Major,
                false);

            result.ShouldBe(Row("11.0.100"));
        }

        [Fact]
        public void With_Major_Type_And_PreRelease_Flag_Returns_Highest()
        {
            var result = ReleaseSelector.TryFindNewest(
                new SemanticVersion("10.0.100"),
                [Row("11.0.100"), Row("12.0.100-preview.1")],
                row => row.Version,
                BumpType.Major,
                true);

            result.ShouldBe(Row("12.0.100-preview.1"));
        }

        [Fact]
        public void With_Null_And_Invalid_Versions_Ignores_Those_Candidates()
        {
            var result = ReleaseSelector.TryFindNewest(
                new SemanticVersion("10.0.403"),
                [new Candidate("invalid", null), Row("10.0.405")],
                row => row.Version,
                BumpType.Minor,
                false);

            result.ShouldBe(Row("10.0.405"));
        }

        [Fact]
        public void With_Null_Current_Version_Throws_ArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => ReleaseSelector.TryFindNewest(
                null!,
                [Row("10.0.405")],
                row => row.Version,
                BumpType.Minor,
                false));
        }
    }

    private static Candidate Row(string version)
    {
        return new Candidate(version, new SemanticVersion(version));
    }

    private sealed record Candidate(string Name, SemanticVersion? Version);
}
