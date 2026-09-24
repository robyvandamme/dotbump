// Copyright © Roby Van Damme.

using DotBump.Commands.BumpPackages;
using DotBump.Commands.BumpPackages.DataModel;
using DotBump.Common;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpPackages;

public class PackageFileServiceTests
{
    private const string FixtureDirectory = "Data/Packages";

    private static readonly PackageFileService s_service = new(new Mock<ILogger>().Object);

    public class GetPackageManifest
    {
        [Fact]
        public void With_Project_File_Returns_Project_Package_Entries()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));

            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.Packages.ShouldSatisfyAllConditions(
                () => manifest.Packages.Count.ShouldBe(3),
                () => manifest.Packages.ShouldContain(package =>
                    package.PackageId == "Newtonsoft.Json"
                    && package.Version == "13.0.1"
                    && package.OriginalVersion == "13.0.1"
                    && package.SourceKind == PackageSourceKind.Project
                    && package.ElementName == "PackageReference"),
                () => manifest.Packages.ShouldContain(package =>
                    package.PackageId == "Serilog" && package.Version == "3.0.0"),
                () => manifest.Packages.ShouldContain(package =>
                    package.PackageId == "Serilog.Sinks.Console" && package.Version == "5.0.0"),
                () => manifest.GetPackageIds().ShouldBe(
                    ["Newtonsoft.Json", "Serilog", "Serilog.Sinks.Console"],
                    ignoreOrder: true));
        }

        [Fact]
        public void With_Central_Package_Management_Returns_Central_Entries()
        {
            ResetTempDirectory();
            CopyFixture("Directory.Packages.props", TempPath("Directory.Packages.props"));

            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.Packages.ShouldSatisfyAllConditions(
                () => manifest.Packages.Count.ShouldBe(3),
                () => manifest.Packages.ShouldContain(package =>
                    package.PackageId == "Newtonsoft.Json"
                    && package.Version == "13.0.1"
                    && package.ElementName == "PackageVersion"
                    && package.SourceKind == PackageSourceKind.CentralPackageManagement),
                () => manifest.Packages.ShouldContain(package =>
                    package.PackageId == "Serilog"
                    && package.Version == "3.0.0"
                    && package.ElementName == "PackageVersion"),
                () => manifest.Packages.ShouldContain(package =>
                    package.PackageId == "StyleCop.Analyzers"
                    && package.Version == "1.2.0"
                    && package.ElementName == "GlobalPackageReference"));
        }

        [Fact]
        public void With_Shared_MsBuild_Files_Returns_Shared_Entries()
        {
            ResetTempDirectory();
            CopyFixture("Directory.Build.props", TempPath("Directory.Build.props"));
            CopyFixture("Directory.Build.targets", TempPath("Directory.Build.targets"));

            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.Packages.ShouldSatisfyAllConditions(
                () => manifest.Packages.Count.ShouldBe(2),
                () => manifest.Packages.ShouldContain(package =>
                    package.PackageId == "Roslynator.Analyzers"
                    && package.Version == "4.16.1"
                    && package.SourceKind == PackageSourceKind.SharedMsBuild),
                () => manifest.Packages.ShouldContain(package =>
                    package.PackageId == "Microsoft.SourceLink.GitHub"
                    && package.Version == "8.0.0"
                    && package.SourceKind == PackageSourceKind.SharedMsBuild));
        }

        [Fact]
        public void With_Nested_Project_And_Root_Shared_File_Discovers_Both()
        {
            ResetTempDirectory();
            CopyFixture("Directory.Build.props", TempPath("Directory.Build.props"));
            CopyFixture("Sample.csproj", TempPath("src", "App", "Sample.csproj"));

            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.GetPackageIds().ShouldSatisfyAllConditions(
                () => manifest.GetPackageIds().ShouldContain("Roslynator.Analyzers"),
                () => manifest.GetPackageIds().ShouldContain("Newtonsoft.Json"));
        }

        [Fact]
        public void With_Unsupported_Versions_Skips_And_Warns()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));

            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.GetPackageIds().ShouldSatisfyAllConditions(
                () => manifest.GetPackageIds().ShouldNotContain("GitVersion.MsBuild"),
                () => manifest.GetPackageIds().ShouldNotContain("Shouldly"),
                () => manifest.GetPackageIds().ShouldNotContain("xunit"),
                () => manifest.Warnings.Count.ShouldBe(3));
        }

        [Fact]
        public void With_Package_Without_Version_Skips_Without_Warning()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));

            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.GetPackageIds().ShouldSatisfyAllConditions(
                () => manifest.GetPackageIds().ShouldNotContain("Moq"),
                () => manifest.Warnings.ShouldNotContain(warning => warning.Contains("Moq", StringComparison.Ordinal)));
        }

        [Fact]
        public void With_Same_Package_In_Multiple_Files_Returns_Multiple_Entries()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));
            CopyFixture("Directory.Packages.props", TempPath("Directory.Packages.props"));

            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.Packages.Count(package => package.PackageId == "Newtonsoft.Json").ShouldBe(2);
        }

        [Fact]
        public void With_Excluded_Directory_Is_Skipped()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("obj", "Sample.csproj"));

            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.Packages.ShouldBeEmpty();
        }

        [Fact]
        public void With_Symlinked_File_Outside_Root_Is_Skipped()
        {
            ResetTempDirectory();
            var outsidePath = OutsidePath("Outside.csproj");
            CopyFixture("Sample.csproj", outsidePath);
            if (!TryCreateSymbolicLink(TempPath("Linked.csproj"), outsidePath, directory: false))
            {
                return;
            }

            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.Packages.ShouldBeEmpty();
        }

        [Fact]
        public void With_Symlinked_File_Inside_Root_Is_Skipped_But_Target_Still_Found()
        {
            ResetTempDirectory();
            var targetPath = TempPath("Real.csproj");
            CopyFixture("Sample.csproj", targetPath);
            if (!TryCreateSymbolicLink(TempPath("Linked.csproj"), targetPath, directory: false))
            {
                return;
            }

            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.Packages.ShouldSatisfyAllConditions(
                () => manifest.Packages.Count(package => package.PackageId == "Newtonsoft.Json").ShouldBe(1),
                () => manifest.Packages.First(package => package.PackageId == "Newtonsoft.Json")
                    .FilePath.ShouldEndWith("Real.csproj"));
        }

        [Fact]
        public void With_Symlinked_Directory_Is_Skipped()
        {
            ResetTempDirectory();
            var outsideDirectory = OutsideDirectory.AbsolutePath;
            CopyFixture("Sample.csproj", Path.Combine(outsideDirectory, "Outside.csproj"));
            if (!TryCreateSymbolicLink(TempPath("linked-dir"), outsideDirectory, directory: true))
            {
                return;
            }

            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.Packages.ShouldBeEmpty();
        }

        [Fact]
        public void With_Missing_Directory_Throws_DotBumpException()
        {
            ResetTempDirectory();

            Should.Throw<DotBumpException>(() => s_service.GetPackageManifest(TempPath("does-not-exist")));
        }

        [Fact]
        public void With_Null_Path_Throws_ArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => s_service.GetPackageManifest(null!));
        }

        [Fact]
        public void With_Whitespace_Path_Throws_ArgumentException()
        {
            Should.Throw<ArgumentException>(() => s_service.GetPackageManifest(" "));
        }
    }

    public class SavePackageManifest
    {
        [Fact]
        public void With_Null_Manifest_Throws_ArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => s_service.SavePackageManifest(null!));
        }

        [Fact]
        public void With_Changed_Version_Updates_All_Occurrences()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));
            CopyFixture("Directory.Packages.props", TempPath("Directory.Packages.props"));
            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.3");
            s_service.SavePackageManifest(manifest);

            var projectContents = File.ReadAllText(TempPath("Sample.csproj"));
            var centralContents = File.ReadAllText(TempPath("Directory.Packages.props"));
            projectContents.ShouldSatisfyAllConditions(
                () => projectContents.ShouldContain("13.0.3"),
                () => projectContents.ShouldNotContain("13.0.1"),
                () => centralContents.ShouldContain("13.0.3"),
                () => centralContents.ShouldNotContain("13.0.1"));
        }

        [Fact]
        public void With_Child_Element_Version_Updates_Value()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));
            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Serilog", "3.1.0");
            s_service.SavePackageManifest(manifest);

            File.ReadAllText(TempPath("Sample.csproj")).ShouldContain("<Version>3.1.0</Version>");
        }

        [Fact]
        public void With_Unchanged_Manifest_Leaves_File_Byte_For_Byte()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));
            var originalBytes = File.ReadAllBytes(TempPath("Sample.csproj"));
            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            s_service.SavePackageManifest(manifest);

            File.ReadAllBytes(TempPath("Sample.csproj")).ShouldBe(originalBytes);
        }

        [Fact]
        public void With_Changed_Manifest_Preserves_Formatting()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));
            var originalContents = File.ReadAllText(TempPath("Sample.csproj"));
            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            s_service.SavePackageManifest(manifest);

            var expectedContents = originalContents.Replace("13.0.1", "13.0.2", StringComparison.Ordinal);
            File.ReadAllText(TempPath("Sample.csproj")).ShouldBe(expectedContents);
        }

        [Fact]
        public void With_Xml_Declaration_Preserves_Declaration()
        {
            ResetTempDirectory();
            CopyFixture("Directory.Build.props", TempPath("Directory.Build.props"));
            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Roslynator.Analyzers", "4.17.0");
            s_service.SavePackageManifest(manifest);

            var contents = File.ReadAllText(TempPath("Directory.Build.props"));
            contents.ShouldSatisfyAllConditions(
                () => contents.ShouldStartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>"),
                () => contents.ShouldContain("4.17.0"));
        }

        [Fact]
        public void With_No_Changes_Reports_No_Changes()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));
            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.HasChanges.ShouldBeFalse();

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");

            manifest.HasChanges.ShouldBeTrue();
        }

        [Fact]
        public void With_Multiline_Attributes_Preserves_Formatting()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <ItemGroup>\n    <PackageReference\n        Include=\"Newtonsoft.Json\"\n        Version=\"13.0.1\" />\n  </ItemGroup>\n</Project>\n";
            var path = TempPath("Multi.csproj");
            File.WriteAllText(path, content);
            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            s_service.SavePackageManifest(manifest);

            File.ReadAllText(path).ShouldBe(content.Replace("13.0.1", "13.0.2", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Single_Quoted_Attributes_Preserves_Quotes()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <ItemGroup>\n    <PackageReference Include='Newtonsoft.Json' Version='13.0.1' />\n  </ItemGroup>\n</Project>\n";
            var path = TempPath("Single.csproj");
            File.WriteAllText(path, content);
            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            s_service.SavePackageManifest(manifest);

            File.ReadAllText(path).ShouldBe(content.Replace("13.0.1", "13.0.2", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Entities_And_Empty_Elements_Preserves_Them()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <PropertyGroup></PropertyGroup>\n  <ItemGroup Condition=\"'$(X)' &gt;= '1.0'\">\n    <PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.1\" />\n  </ItemGroup>\n</Project>\n";
            var path = TempPath("Entities.csproj");
            File.WriteAllText(path, content);
            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            s_service.SavePackageManifest(manifest);

            File.ReadAllText(path).ShouldBe(content.Replace("13.0.1", "13.0.2", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Crlf_Preserves_Line_Endings()
        {
            ResetTempDirectory();
            var content =
                "<Project>\r\n  <ItemGroup>\r\n    <PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.1\" />\r\n  </ItemGroup>\r\n</Project>\r\n";
            var path = TempPath("CrLf.csproj");
            File.WriteAllText(path, content);
            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            s_service.SavePackageManifest(manifest);

            File.ReadAllText(path).ShouldBe(content.Replace("13.0.1", "13.0.2", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Repeated_Versions_Updates_Only_Targeted_Occurrence()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"PackageA\" Version=\"1.0.0\" />\n    <PackageReference Include=\"PackageB\" Version=\"1.0.0\" />\n  </ItemGroup>\n</Project>\n";
            var path = TempPath("Repeated.csproj");
            File.WriteAllText(path, content);
            var manifest = s_service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("PackageB", "2.0.0");
            s_service.SavePackageManifest(manifest);

            var expected = content.Replace(
                "Include=\"PackageB\" Version=\"1.0.0\"",
                "Include=\"PackageB\" Version=\"2.0.0\"",
                StringComparison.Ordinal);
            File.ReadAllText(path).ShouldBe(expected);
        }
    }

    private static LocalDirectory TempDirectory => new("./temp/packages");

    private static LocalDirectory OutsideDirectory => new("./temp/outside");

    private static void ResetTempDirectory()
    {
        TempDirectory.EnsureDirectoryDeleted();
        OutsideDirectory.EnsureDirectoryDeleted();
        TempDirectory.EnsureDirectoryCreated();
    }

    private static string TempPath(params string[] segments)
    {
        return CombinePath(TempDirectory.AbsolutePath, segments);
    }

    private static string OutsidePath(params string[] segments)
    {
        return CombinePath(OutsideDirectory.AbsolutePath, segments);
    }

    private static string CombinePath(string root, params string[] segments)
    {
        var path = root;
        foreach (var segment in segments)
        {
            path = Path.Combine(path, segment);
        }

        return path;
    }

    private static void CopyFixture(string fixtureFileName, string destinationPath)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        File.Copy(
            Path.Combine(Directory.GetCurrentDirectory(), FixtureDirectory, fixtureFileName),
            destinationPath,
            overwrite: true);
    }

    private static bool TryCreateSymbolicLink(string linkPath, string targetPath, bool directory)
    {
        try
        {
            if (directory)
            {
                Directory.CreateSymbolicLink(linkPath, targetPath);
            }
            else
            {
                File.CreateSymbolicLink(linkPath, targetPath);
            }

            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            // Creating symlinks on Windows requires Developer Mode or elevation, so tolerate that case.
            // On any other platform a failure is unexpected and should fail the test.
            if (!OperatingSystem.IsWindows())
            {
                throw new InvalidOperationException(
                    $"Failed to create symbolic link '{linkPath}' -> '{targetPath}'.",
                    e);
            }

            return false;
        }
    }
}
