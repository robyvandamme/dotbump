// Copyright © Roby Van Damme.

using DotBump.NuGet;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.NuGet.DataModel.PackageResolution;
using Shouldly;

namespace DotBump.Tests.NuGet;

public class PackageSourceMappingResolverTests
{
    public class Resolve
    {
        [Fact]
        public void With_No_Package_Source_Mapping_Returns_All_Sources()
        {
            // Arrange
            var nugetConfig = CreateConfig(
                [("nuget.org", "https://api.nuget.org/v3/index.json"), ("private", "https://private/index.json")]);
            var resolver = new PackageSourceMappingResolver();

            // Act
            var result = resolver.Resolve(["Some.Package"], nugetConfig);

            // Assert
            result.Warnings.ShouldBeEmpty();
            result.Matches["Some.Package"].ShouldSatisfyAllConditions(
                () => GetSourceKeys(result, "Some.Package").ShouldBe(["nuget.org", "private"]),
                () => result.Matches["Some.Package"].MatchedPattern.ShouldBeNull());
        }

        [Fact]
        public void With_Exact_Pattern_Returns_Exact_Source_Only()
        {
            // Arrange
            var nugetConfig = CreateConfig(
                [("nuget.org", "https://api.nuget.org/v3/index.json"), ("robyvandamme", "https://private/index.json")],
                ("nuget.org", ["*"]),
                ("robyvandamme", ["dotbump"]));
            var resolver = new PackageSourceMappingResolver();

            // Act
            var result = resolver.Resolve(["dotbump"], nugetConfig);

            // Assert
            result.Warnings.ShouldBeEmpty();
            result.Matches["dotbump"].ShouldSatisfyAllConditions(
                () => GetSourceKeys(result, "dotbump").ShouldBe(["robyvandamme"]),
                () => result.Matches["dotbump"].MatchedPattern.ShouldBe("dotbump"));
        }

        [Fact]
        public void With_Wildcard_Pattern_Returns_Wildcard_Source()
        {
            // Arrange
            var nugetConfig = CreateConfig(
                [("nuget.org", "https://api.nuget.org/v3/index.json"), ("robyvandamme", "https://private/index.json")],
                ("nuget.org", ["*"]),
                ("robyvandamme", ["dotbump"]));
            var resolver = new PackageSourceMappingResolver();

            // Act
            var result = resolver.Resolve(["Newtonsoft.Json"], nugetConfig);

            // Assert
            result.Warnings.ShouldBeEmpty();
            result.Matches["Newtonsoft.Json"].ShouldSatisfyAllConditions(
                () => GetSourceKeys(result, "Newtonsoft.Json").ShouldBe(["nuget.org"]),
                () => result.Matches["Newtonsoft.Json"].MatchedPattern.ShouldBe("*"));
        }

        [Fact]
        public void With_Longer_Prefix_Returns_Most_Specific_Source()
        {
            // Arrange
            var nugetConfig = CreateConfig(
                [("broad", "https://broad/index.json"), ("specific", "https://specific/index.json")],
                ("broad", ["Microsoft.*"]),
                ("specific", ["Microsoft.Extensions.*"]));
            var resolver = new PackageSourceMappingResolver();

            // Act
            var result = resolver.Resolve(["Microsoft.Extensions.Logging", "Microsoft.AspNetCore.Mvc"], nugetConfig);

            // Assert
            result.Warnings.ShouldBeEmpty();
            result.ShouldSatisfyAllConditions(
                () => GetSourceKeys(result, "Microsoft.Extensions.Logging").ShouldBe(["specific"]),
                () => result.Matches["Microsoft.Extensions.Logging"].MatchedPattern.ShouldBe(
                    "Microsoft.Extensions.*"),
                () => GetSourceKeys(result, "Microsoft.AspNetCore.Mvc").ShouldBe(["broad"]),
                () => result.Matches["Microsoft.AspNetCore.Mvc"].MatchedPattern.ShouldBe("Microsoft.*"));
        }

        [Fact]
        public void With_Exact_And_Prefix_Patterns_Exact_Pattern_Wins()
        {
            // Arrange
            var nugetConfig = CreateConfig(
                [("wildcard", "https://wildcard/index.json"), ("exact", "https://exact/index.json")],
                ("wildcard", ["Contoso.*"]),
                ("exact", ["Contoso.Core"]));
            var resolver = new PackageSourceMappingResolver();

            // Act
            var result = resolver.Resolve(["Contoso.Core"], nugetConfig);

            // Assert
            result.Warnings.ShouldBeEmpty();
            result.Matches["Contoso.Core"].ShouldSatisfyAllConditions(
                () => GetSourceKeys(result, "Contoso.Core").ShouldBe(["exact"]),
                () => result.Matches["Contoso.Core"].MatchedPattern.ShouldBe("Contoso.Core"));
        }

        [Fact]
        public void With_Same_Pattern_On_Multiple_Sources_Returns_All_Sources()
        {
            // Arrange
            var nugetConfig = CreateConfig(
                [("feed1", "https://feed1/index.json"), ("feed2", "https://feed2/index.json")],
                ("feed1", ["Contoso.*"]),
                ("feed2", ["Contoso.*"]));
            var resolver = new PackageSourceMappingResolver();

            // Act
            var result = resolver.Resolve(["Contoso.Core"], nugetConfig);

            // Assert
            result.Warnings.ShouldBeEmpty();
            GetSourceKeys(result, "Contoso.Core").ShouldBe(["feed1", "feed2"]);
        }

        [Fact]
        public void With_Unmapped_Package_Returns_No_Sources_And_Warning()
        {
            // Arrange
            var nugetConfig = CreateConfig(
                [("private", "https://private/index.json")],
                ("private", ["dotbump"]));
            var resolver = new PackageSourceMappingResolver();

            // Act
            var result = resolver.Resolve(["Newtonsoft.Json"], nugetConfig);

            // Assert
            result.Matches["Newtonsoft.Json"].Sources.ShouldBeEmpty();
            result.Warnings.ShouldContain(warning => warning.Contains(
                "does not match any packageSourceMapping pattern",
                StringComparison.Ordinal));
        }

        [Fact]
        public void With_Unknown_Source_Key_Returns_Warning()
        {
            // Arrange
            var nugetConfig = CreateConfig(
                [("nuget.org", "https://api.nuget.org/v3/index.json")],
                ("ghost", ["ghost.package"]),
                ("nuget.org", ["*"]));
            var resolver = new PackageSourceMappingResolver();

            // Act
            var result = resolver.Resolve(["ghost.package", "Newtonsoft.Json"], nugetConfig);

            // Assert
            result.Matches["ghost.package"].Sources.ShouldBeEmpty();
            result.Warnings.ShouldContain(warning => warning.Contains(
                "package source 'ghost' does not match any package source",
                StringComparison.Ordinal));
            result.Warnings.ShouldContain(warning => warning.Contains(
                "only mapped to undefined package source",
                StringComparison.Ordinal));
        }

        [Fact]
        public void With_Mixed_Case_Package_Id_Matches_Case_Insensitively()
        {
            // Arrange
            var nugetConfig = CreateConfig(
                [("private", "https://private/index.json")],
                ("private", ["DotBump"]));
            var resolver = new PackageSourceMappingResolver();

            // Act
            var result = resolver.Resolve(["dotbump"], nugetConfig);

            // Assert
            result.Warnings.ShouldBeEmpty();
            GetSourceKeys(result, "dotbump").ShouldBe(["private"]);
        }

        [Fact]
        public void With_Mixed_Case_Source_Key_Resolves_Source()
        {
            // Arrange
            var nugetConfig = CreateConfig(
                [("private", "https://private/index.json")],
                ("PRIVATE", ["dotbump"]));
            var resolver = new PackageSourceMappingResolver();

            // Act
            var result = resolver.Resolve(["dotbump"], nugetConfig);

            // Assert
            result.Warnings.ShouldBeEmpty();
            GetSourceKeys(result, "dotbump").ShouldBe(["private"]);
        }

        [Fact]
        public void With_Wildcard_Not_At_End_Uses_Prefix_Before_Wildcard()
        {
            // Arrange
            var nugetConfig = CreateConfig(
                [("private", "https://private/index.json")],
                ("private", ["Contoso*Suffix"]));
            var resolver = new PackageSourceMappingResolver();

            // Act
            var result = resolver.Resolve(["Contoso.Core", "Other"], nugetConfig);

            // Assert
            result.ShouldSatisfyAllConditions(
                () => GetSourceKeys(result, "Contoso.Core").ShouldBe(["private"]),
                () => result.Matches["Contoso.Core"].MatchedPattern.ShouldBe("Contoso*Suffix"),
                () => result.Matches["Other"].Sources.ShouldBeEmpty());
        }

        [Fact]
        public void With_Duplicate_Source_Key_Entries_Merges_Patterns()
        {
            // Arrange
            var nugetConfig = CreateConfig(
                [("private", "https://private/index.json")],
                ("private", ["dotbump"]),
                ("private", ["dotadr"]));
            var resolver = new PackageSourceMappingResolver();

            // Act
            var result = resolver.Resolve(["dotadr"], nugetConfig);

            // Assert
            result.Warnings.ShouldBeEmpty();
            GetSourceKeys(result, "dotadr").ShouldBe(["private"]);
        }

        [Fact]
        public void With_Empty_Package_List_Returns_Empty_Matches()
        {
            // Arrange
            var nugetConfig = CreateConfig([("nuget.org", "https://api.nuget.org/v3/index.json")]);
            var resolver = new PackageSourceMappingResolver();

            // Act
            var result = resolver.Resolve([], nugetConfig);

            // Assert
            result.ShouldSatisfyAllConditions(
                () => result.Matches.ShouldBeEmpty(),
                () => result.Warnings.ShouldBeEmpty());
        }
    }

    private static string[] GetSourceKeys(PackageSourceMappingResult result, string packageId)
    {
        return result.Matches[packageId].Sources.Select(o => o.Key).ToArray();
    }

    private static NuGetConfig CreateConfig(
        (string Key, string Url)[] sources,
        params (string SourceKey, string[] Patterns)[] mappings)
    {
        return new NuGetConfig
        {
            PackageSources = sources
                .Select(source => new PackageSource
                {
                    Key = source.Key,
                    Value = source.Url,
                    ProtocolVersion = "3",
                })
                .ToList(),
            PackageSourceMappings = mappings
                .Select(mapping => new PackageSourceMappingEntry
                {
                    SourceKey = mapping.SourceKey,
                    Patterns = mapping.Patterns.ToList(),
                })
                .ToList(),
        };
    }
}
