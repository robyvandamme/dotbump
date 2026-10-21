// Copyright © Roby Van Damme.

using System.Text;
using DotBump.Commands.BumpFrameworks;
using DotBump.Common;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpFrameworks;

public class FrameworkFileServiceTests
{
    public class GetFrameworkManifest
    {
        [Fact]
        public void With_TargetFramework_Returns_Entry_With_Suffix()
        {
            ResetTempDirectory();
            WriteFile("App.csproj", CreateProject("net8.0-windows10.0.19041.0"));
            var service = CreateService();

            var manifest = service.GetFrameworkManifest(TempDirectory.AbsolutePath);

            var entry = manifest.Frameworks.ShouldHaveSingleItem();
            entry.Id.ShouldBe("App.csproj");
            entry.OriginalFramework.ShouldBe("net8.0-windows10.0.19041.0");
            entry.Suffix.ShouldBe("-windows10.0.19041.0");
        }

        [Fact]
        public void With_TargetFrameworks_Skips_File_And_Adds_Warning()
        {
            ResetTempDirectory();
            WriteFile(
                "App.csproj",
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFrameworks>net8.0;net9.0</TargetFrameworks>\n  </PropertyGroup>\n</Project>\n");
            var service = CreateService();

            var manifest = service.GetFrameworkManifest(TempDirectory.AbsolutePath);

            manifest.Frameworks.ShouldBeEmpty();
            manifest.Warnings.ShouldContain(warning =>
                warning.Contains("App.csproj", StringComparison.Ordinal)
                && warning.Contains("TargetFrameworks", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Unsupported_TargetFramework_Is_Ignored()
        {
            ResetTempDirectory();
            WriteFile("Library.csproj", CreateProject("netstandard2.0"));
            var service = CreateService();

            var manifest = service.GetFrameworkManifest(TempDirectory.AbsolutePath);

            manifest.Frameworks.ShouldBeEmpty();
        }

        [Fact]
        public void With_Property_Expression_Is_Ignored()
        {
            ResetTempDirectory();
            WriteFile("App.csproj", CreateProject("$(MyTargetFramework)"));
            var service = CreateService();

            var manifest = service.GetFrameworkManifest(TempDirectory.AbsolutePath);

            manifest.Frameworks.ShouldBeEmpty();
        }

        [Fact]
        public void With_Directory_Build_Props_Returns_Entry()
        {
            ResetTempDirectory();
            WriteFile("Directory.Build.props", CreateProject("net8.0"));
            var service = CreateService();

            var manifest = service.GetFrameworkManifest(TempDirectory.AbsolutePath);

            manifest.Frameworks.ShouldHaveSingleItem().OriginalFramework.ShouldBe("net8.0");
        }

        [Fact]
        public void With_Excluded_Directory_Skips_File()
        {
            ResetTempDirectory();
            WriteFile("App.csproj", CreateProject("net8.0"));
            var excludedDirectory = Path.Combine(TempDirectory.AbsolutePath, "excluded");
            Directory.CreateDirectory(excludedDirectory);
            File.WriteAllText(Path.Combine(excludedDirectory, "Other.csproj"), CreateProject("net8.0"));
            var service = CreateService();

            var manifest = service.GetFrameworkManifest(
                TempDirectory.AbsolutePath,
                [excludedDirectory]);

            manifest.Frameworks.ShouldHaveSingleItem().Id.ShouldBe("App.csproj");
        }

        [Fact]
        public void With_Invalid_Xml_Throws_DotBumpException()
        {
            ResetTempDirectory();
            WriteFile("App.csproj", "<Project><PropertyGroup>");
            var service = CreateService();

            Should.Throw<DotBumpException>(() => service.GetFrameworkManifest(TempDirectory.AbsolutePath));
        }
    }

    public class SaveFrameworkManifest
    {
        [Fact]
        public void With_Change_Updates_Only_TargetFramework_Value()
        {
            ResetTempDirectory();
            const string original = "<Project Sdk=\"Microsoft.NET.Sdk\">\r\n  <PropertyGroup>\r\n    <TargetFramework>net8.0</TargetFramework>\r\n  </PropertyGroup>\r\n</Project>\r\n";
            WriteFile("App.csproj", original);
            var service = CreateService();
            var manifest = service.GetFrameworkManifest(TempDirectory.AbsolutePath);

            manifest.SetFramework(manifest.Frameworks.Single(), "net10.0");
            service.SaveFrameworkManifest(manifest);

            var updated = File.ReadAllText(Path.Combine(TempDirectory.AbsolutePath, "App.csproj"));
            updated.ShouldBe(original.Replace("net8.0", "net10.0", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Multiple_TargetFramework_Elements_Updates_All()
        {
            ResetTempDirectory();
            const string original =
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                "  <PropertyGroup Condition=\"'$(OS)' == 'Windows_NT'\">\n" +
                "    <TargetFramework>net8.0-windows</TargetFramework>\n" +
                "  </PropertyGroup>\n" +
                "  <PropertyGroup Condition=\"'$(OS)' != 'Windows_NT'\">\n" +
                "    <TargetFramework>net8.0</TargetFramework>\n" +
                "  </PropertyGroup>\n" +
                "</Project>\n";
            WriteFile("App.csproj", original);
            var service = CreateService();
            var manifest = service.GetFrameworkManifest(TempDirectory.AbsolutePath);

            foreach (var entry in manifest.Frameworks)
            {
                manifest.SetFramework(entry, "net10.0" + entry.Suffix);
            }

            service.SaveFrameworkManifest(manifest);

            var updated = File.ReadAllText(Path.Combine(TempDirectory.AbsolutePath, "App.csproj"));
            updated.ShouldSatisfyAllConditions(
                () => updated.ShouldContain("<TargetFramework>net10.0-windows</TargetFramework>"),
                () => updated.ShouldContain("<TargetFramework>net10.0</TargetFramework>"));
        }

        [Fact]
        public void With_Bom_Preserves_Bom()
        {
            ResetTempDirectory();
            var path = Path.Combine(TempDirectory.AbsolutePath, "App.csproj");
            File.WriteAllText(path, CreateProject("net8.0"), new UTF8Encoding(true));
            var service = CreateService();
            var manifest = service.GetFrameworkManifest(TempDirectory.AbsolutePath);

            manifest.SetFramework(manifest.Frameworks.Single(), "net10.0");
            service.SaveFrameworkManifest(manifest);

            var bytes = File.ReadAllBytes(path);
            bytes.ShouldSatisfyAllConditions(
                () => bytes[0].ShouldBe((byte)0xEF),
                () => bytes[1].ShouldBe((byte)0xBB),
                () => bytes[2].ShouldBe((byte)0xBF));
        }
    }

    private static LocalDirectory TempDirectory => new("./temp/frameworks");

    private static void ResetTempDirectory()
    {
        TempDirectory.EnsureDirectoryDeleted();
        TempDirectory.EnsureDirectoryCreated();
    }

    private static void WriteFile(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(TempDirectory.AbsolutePath, fileName), content);
    }

    private static string CreateProject(string targetFramework)
    {
        return
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
            "  <PropertyGroup>\n" +
            $"    <TargetFramework>{targetFramework}</TargetFramework>\n" +
            "  </PropertyGroup>\n" +
            "</Project>\n";
    }

    private static FrameworkFileService CreateService()
    {
        return new FrameworkFileService(Mock.Of<ILogger>());
    }
}
