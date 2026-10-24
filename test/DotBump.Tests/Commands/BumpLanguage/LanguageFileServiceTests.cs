// Copyright © Roby Van Damme.

using System.Text;
using DotBump.Commands.BumpLanguage;
using DotBump.Commands.BumpLanguage.DataModel;
using DotBump.Common;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpLanguage;

public class LanguageFileServiceTests
{
    public class GetLanguageManifest
    {
        [Fact]
        public void With_LangVersion_In_Project_Returns_Entry()
        {
            ResetTempDirectory();
            WriteFile("App.csproj", CreateProject("12.0"));
            var service = CreateService();

            var manifest = service.GetLanguageManifest(TempDirectory.AbsolutePath);

            var entry = manifest.Entries.ShouldHaveSingleItem();
            entry.Id.ShouldBe("App.csproj");
            entry.OriginalVersion.ShouldBe("12.0");
            entry.ParsedVersion.ShouldBe(new LanguageVersion(12, 0));
        }

        [Fact]
        public void With_LangVersion_In_Directory_Build_Props_Returns_Entry()
        {
            ResetTempDirectory();
            WriteFile("Directory.Build.props", CreateProject("12.0"));
            var service = CreateService();

            var manifest = service.GetLanguageManifest(TempDirectory.AbsolutePath);

            manifest.Entries.ShouldHaveSingleItem().OriginalVersion.ShouldBe("12.0");
        }

        [Fact]
        public void With_LangVersion_In_Directory_Build_Targets_Returns_Entry()
        {
            ResetTempDirectory();
            WriteFile("Directory.Build.targets", CreateProject("12.0"));
            var service = CreateService();

            var manifest = service.GetLanguageManifest(TempDirectory.AbsolutePath);

            manifest.Entries.ShouldHaveSingleItem().OriginalVersion.ShouldBe("12.0");
        }

        [Fact]
        public void With_Missing_LangVersion_Returns_Empty_Manifest()
        {
            ResetTempDirectory();
            WriteFile("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n</Project>\n");
            var service = CreateService();

            var manifest = service.GetLanguageManifest(TempDirectory.AbsolutePath);

            manifest.Entries.ShouldBeEmpty();
        }

        [Fact]
        public void With_Non_Numeric_LangVersion_Is_Ignored()
        {
            ResetTempDirectory();
            WriteFile("App.csproj", CreateProject("latest"));
            var service = CreateService();

            var manifest = service.GetLanguageManifest(TempDirectory.AbsolutePath);

            manifest.Entries.ShouldBeEmpty();
        }

        [Fact]
        public void With_Property_Expression_Is_Ignored()
        {
            ResetTempDirectory();
            WriteFile("App.csproj", CreateProject("$(LangVersion)"));
            var service = CreateService();

            var manifest = service.GetLanguageManifest(TempDirectory.AbsolutePath);

            manifest.Entries.ShouldBeEmpty();
        }

        [Fact]
        public void With_Visual_Basic_Project_Is_Ignored()
        {
            ResetTempDirectory();
            WriteFile("Library.vbproj", CreateProject("12.0"));
            var service = CreateService();

            var manifest = service.GetLanguageManifest(TempDirectory.AbsolutePath);

            manifest.Entries.ShouldBeEmpty();
        }

        [Fact]
        public void With_Excluded_Directory_Skips_File()
        {
            ResetTempDirectory();
            WriteFile("App.csproj", CreateProject("12.0"));
            var excludedDirectory = Path.Combine(TempDirectory.AbsolutePath, "excluded");
            Directory.CreateDirectory(excludedDirectory);
            File.WriteAllText(Path.Combine(excludedDirectory, "Other.csproj"), CreateProject("12.0"));
            var service = CreateService();

            var manifest = service.GetLanguageManifest(
                TempDirectory.AbsolutePath,
                [excludedDirectory]);

            manifest.Entries.ShouldHaveSingleItem().Id.ShouldBe("App.csproj");
        }

        [Fact]
        public void With_Comment_Split_LangVersion_Skips_Entry_And_Adds_Warning()
        {
            ResetTempDirectory();
            const string content =
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                "  <PropertyGroup>\n" +
                "    <LangVersion>12<!-- keep -->.0</LangVersion>\n" +
                "  </PropertyGroup>\n" +
                "</Project>\n";
            WriteFile("App.csproj", content);
            var service = CreateService();

            var manifest = service.GetLanguageManifest(TempDirectory.AbsolutePath);

            manifest.Entries.ShouldBeEmpty();
            manifest.Warnings.ShouldContain(warning =>
                warning.Contains("could not be located", StringComparison.Ordinal));
            File.ReadAllText(Path.Combine(TempDirectory.AbsolutePath, "App.csproj")).ShouldBe(content);
        }

        [Fact]
        public void With_Invalid_Xml_Throws_DotBumpException()
        {
            ResetTempDirectory();
            WriteFile("App.csproj", "<Project><PropertyGroup>");
            var service = CreateService();

            Should.Throw<DotBumpException>(() => service.GetLanguageManifest(TempDirectory.AbsolutePath));
        }
    }

    public class SaveLanguageManifest
    {
        [Fact]
        public void With_Change_Updates_Only_LangVersion_Value()
        {
            ResetTempDirectory();
            const string original = "<Project Sdk=\"Microsoft.NET.Sdk\">\r\n  <PropertyGroup>\r\n    <LangVersion>12.0</LangVersion>\r\n  </PropertyGroup>\r\n</Project>\r\n";
            WriteFile("App.csproj", original);
            var service = CreateService();
            var manifest = service.GetLanguageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion(manifest.Entries.Single(), "14.0");
            service.SaveLanguageManifest(manifest);

            var updated = File.ReadAllText(Path.Combine(TempDirectory.AbsolutePath, "App.csproj"));
            updated.ShouldBe(original.Replace("12.0", "14.0", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Multiple_LangVersion_Elements_Updates_All()
        {
            ResetTempDirectory();
            const string original =
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                "  <PropertyGroup Condition=\"'$(OS)' == 'Windows_NT'\">\n" +
                "    <LangVersion>12.0</LangVersion>\n" +
                "  </PropertyGroup>\n" +
                "  <PropertyGroup Condition=\"'$(OS)' != 'Windows_NT'\">\n" +
                "    <LangVersion>11.0</LangVersion>\n" +
                "  </PropertyGroup>\n" +
                "</Project>\n";
            WriteFile("App.csproj", original);
            var service = CreateService();
            var manifest = service.GetLanguageManifest(TempDirectory.AbsolutePath);

            foreach (var entry in manifest.Entries)
            {
                manifest.SetVersion(entry, "14.0");
            }

            service.SaveLanguageManifest(manifest);

            var updated = File.ReadAllText(Path.Combine(TempDirectory.AbsolutePath, "App.csproj"));
            updated.ShouldSatisfyAllConditions(
                () => updated.ShouldContain("<LangVersion>14.0</LangVersion>"),
                () => updated.ShouldNotContain("12.0"),
                () => updated.ShouldNotContain("11.0"));
        }

        [Fact]
        public void With_Bom_Preserves_Bom()
        {
            ResetTempDirectory();
            var path = Path.Combine(TempDirectory.AbsolutePath, "App.csproj");
            File.WriteAllText(path, CreateProject("12.0"), new UTF8Encoding(true));
            var service = CreateService();
            var manifest = service.GetLanguageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion(manifest.Entries.Single(), "14.0");
            service.SaveLanguageManifest(manifest);

            var bytes = File.ReadAllBytes(path);
            bytes.ShouldSatisfyAllConditions(
                () => bytes[0].ShouldBe((byte)0xEF),
                () => bytes[1].ShouldBe((byte)0xBB),
                () => bytes[2].ShouldBe((byte)0xBF));
        }
    }

    private static LocalDirectory TempDirectory => new("./temp/language");

    private static void ResetTempDirectory()
    {
        TempDirectory.EnsureDirectoryDeleted();
        TempDirectory.EnsureDirectoryCreated();
    }

    private static void WriteFile(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(TempDirectory.AbsolutePath, fileName), content);
    }

    private static string CreateProject(string languageVersion)
    {
        return
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
            "  <PropertyGroup>\n" +
            $"    <LangVersion>{languageVersion}</LangVersion>\n" +
            "  </PropertyGroup>\n" +
            "</Project>\n";
    }

    private static LanguageFileService CreateService()
    {
        return new LanguageFileService(Mock.Of<ILogger>());
    }
}
