// Copyright © Roby Van Damme.

using System.Text;
using System.Xml.Linq;
using DotBump.Commands.BumpPackages;
using DotBump.Commands.BumpPackages.DataModel;
using DotBump.Common;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Serilog.Events;
using Shouldly;

namespace DotBump.Tests.Commands.BumpPackages;

public class PackageFileServiceTests
{
    private const string FixtureDirectory = "Data/Packages";

    public class GetPackageManifest
    {
        [Fact]
        public void With_Project_File_Returns_Project_Package_Entries()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

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
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

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
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

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
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.GetPackageIds().ShouldSatisfyAllConditions(
                () => manifest.GetPackageIds().ShouldContain("Roslynator.Analyzers"),
                () => manifest.GetPackageIds().ShouldContain("Newtonsoft.Json"));
        }

        [Fact]
        public void With_Unsupported_Versions_Skips_And_Warns()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));
            var (service, sink) = CreateServiceWithSink();

            var manifest = service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.GetPackageIds().ShouldSatisfyAllConditions(
                () => manifest.GetPackageIds().ShouldNotContain("GitVersion.MsBuild"),
                () => manifest.GetPackageIds().ShouldNotContain("Shouldly"),
                () => manifest.GetPackageIds().ShouldNotContain("xunit"),
                () => manifest.Warnings.Count.ShouldBe(3));
            sink.Events.Count(logEvent => logEvent.Level == LogEventLevel.Warning).ShouldBe(3);
        }

        [Fact]
        public void With_Package_Without_Version_Skips_Without_Warning()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

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
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.Packages.Count(package => package.PackageId == "Newtonsoft.Json").ShouldBe(2);
        }

        [Fact]
        public void With_Excluded_Directory_Is_Skipped()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("obj", "Sample.csproj"));
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

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

            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

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

            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

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

            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.Packages.ShouldBeEmpty();
        }

        [Fact]
        public void With_Whitespace_Formatted_Child_Version_Returns_Trimmed_Version()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"Newtonsoft.Json\">\n      <Version>\n        3.0.0\n      </Version>\n    </PackageReference>\n  </ItemGroup>\n</Project>\n";
            File.WriteAllText(TempPath("WhitespaceVersion.csproj"), content);
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.Packages.ShouldContain(package =>
                package.PackageId == "Newtonsoft.Json" && package.Version == "3.0.0");
        }

        [Fact]
        public void With_Update_Attribute_Returns_Package_Entry()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <ItemGroup>\n    <PackageReference Update=\"Newtonsoft.Json\" Version=\"13.0.1\" />\n  </ItemGroup>\n</Project>\n";
            File.WriteAllText(TempPath("Update.csproj"), content);
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.Packages.ShouldContain(package =>
                package.PackageId == "Newtonsoft.Json"
                && package.Version == "13.0.1"
                && package.ElementName == "PackageReference");
        }

        [Fact]
        public void With_Entity_Encoded_Version_Returns_Decoded_Version()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"Newtonsoft.Json\" Version=\"13&#46;0&#46;1\" />\n  </ItemGroup>\n</Project>\n";
            File.WriteAllText(TempPath("Entities.csproj"), content);
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.Packages.ShouldContain(package =>
                package.PackageId == "Newtonsoft.Json"
                && package.OriginalVersion == "13.0.1"
                && package.Version == "13.0.1");
        }

        [Fact]
        public void With_Version_Element_Containing_Leading_Comment_Skips_And_Warns()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"Newtonsoft.Json\">\n      <Version> <!-- keep --> 3.0.0 </Version>\n    </PackageReference>\n  </ItemGroup>\n</Project>\n";
            File.WriteAllText(TempPath("CommentedVersion.csproj"), content);
            var (service, sink) = CreateServiceWithSink();

            var manifest = service.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.GetPackageIds().ShouldNotContain("Newtonsoft.Json");
            manifest.Warnings.ShouldContain(warning =>
                warning.Contains("could not be located", StringComparison.Ordinal));
            sink.Events.ShouldContain(logEvent =>
                logEvent.Level == LogEventLevel.Warning
                && logEvent.RenderMessage(null).Contains("could not be located", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Missing_Directory_Throws_DotBumpException()
        {
            ResetTempDirectory();
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            Should.Throw<DotBumpException>(() => packageFileService.GetPackageManifest(TempPath("does-not-exist")));
        }

        [Fact]
        public void With_Null_Path_Throws_ArgumentNullException()
        {
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            Should.Throw<ArgumentNullException>(() => packageFileService.GetPackageManifest(null!));
        }

        [Fact]
        public void With_Whitespace_Path_Throws_ArgumentException()
        {
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            Should.Throw<ArgumentException>(() => packageFileService.GetPackageManifest(" "));
        }
    }

    public class SavePackageManifest
    {
        [Fact]
        public void With_Null_Manifest_Throws_ArgumentNullException()
        {
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            Should.Throw<ArgumentNullException>(() => packageFileService.SavePackageManifest(null!));
        }

        [Fact]
        public void With_Changed_Version_Updates_All_Occurrences()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));
            CopyFixture("Directory.Packages.props", TempPath("Directory.Packages.props"));
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.3");
            packageFileService.SavePackageManifest(manifest);

            var projectContents = File.ReadAllText(TempPath("Sample.csproj"));
            var centralContents = File.ReadAllText(TempPath("Directory.Packages.props"));
            projectContents.ShouldSatisfyAllConditions(
                () => projectContents.ShouldContain("13.0.3"),
                () => projectContents.ShouldNotContain("13.0.1"),
                () => centralContents.ShouldContain("13.0.3"),
                () => centralContents.ShouldNotContain("13.0.1"));
        }

        [Fact]
        public void With_Ids_Differing_Only_By_Case_Bumps_All_Occurrences_To_One_Target()
        {
            ResetTempDirectory();
            var upperContent =
                "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.1\" />\n  </ItemGroup>\n</Project>\n";
            var lowerContent =
                "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"newtonsoft.json\" Version=\"13.0.1\" />\n  </ItemGroup>\n</Project>\n";
            var upperPath = TempPath("Upper.csproj");
            var lowerPath = TempPath("Lower.csproj");
            File.WriteAllText(upperPath, upperContent);
            File.WriteAllText(lowerPath, lowerContent);
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);

            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);
            manifest.GetPackageIds().ShouldBe(["Newtonsoft.Json"]);

            manifest.SetVersion("NEWTONSOFT.JSON", "13.0.2");
            packageFileService.SavePackageManifest(manifest);

            var upperResult = File.ReadAllText(upperPath);
            var lowerResult = File.ReadAllText(lowerPath);
            upperResult.ShouldBe(upperContent.Replace("13.0.1", "13.0.2", StringComparison.Ordinal));
            lowerResult.ShouldBe(lowerContent.Replace("13.0.1", "13.0.2", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Child_Element_Version_Updates_Value()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Serilog", "3.1.0");
            packageFileService.SavePackageManifest(manifest);

            File.ReadAllText(TempPath("Sample.csproj")).ShouldContain("<Version>3.1.0</Version>");
        }

        [Fact]
        public void With_Unchanged_Manifest_Leaves_File_Byte_For_Byte()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));
            var originalBytes = File.ReadAllBytes(TempPath("Sample.csproj"));
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            packageFileService.SavePackageManifest(manifest);

            File.ReadAllBytes(TempPath("Sample.csproj")).ShouldBe(originalBytes);
        }

        [Fact]
        public void With_Utf8_Bom_File_Modified_Preserves_Bom_And_Encoding()
        {
            AssertBomPreserved_On_Modification(new UTF8Encoding(true), "Simple.csproj");
        }

        [Fact]
        public void With_Utf16_Le_Bom_File_Modified_Preserves_Bom_And_Encoding()
        {
            AssertBomPreserved_On_Modification(new UnicodeEncoding(false, true), "Simple.csproj");
        }

        [Fact]
        public void With_Utf16_Be_Bom_File_Modified_Preserves_Bom_And_Encoding()
        {
            AssertBomPreserved_On_Modification(new UnicodeEncoding(true, true), "Simple.csproj");
        }

        [Fact]
        public void With_No_Bom_File_Modified_Does_Not_Add_Bom()
        {
            AssertBomPreserved_On_Modification(new UTF8Encoding(false), "Simple.csproj");
        }

        [Fact]
        public void With_Changed_Manifest_Preserves_Formatting()
        {
            ResetTempDirectory();
            CopyFixture("Sample.csproj", TempPath("Sample.csproj"));
            var originalContents = File.ReadAllText(TempPath("Sample.csproj"));
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            packageFileService.SavePackageManifest(manifest);

            var expectedContents = originalContents.Replace("13.0.1", "13.0.2", StringComparison.Ordinal);
            File.ReadAllText(TempPath("Sample.csproj")).ShouldBe(expectedContents);
        }

        [Fact]
        public void With_Xml_Declaration_Preserves_Declaration()
        {
            ResetTempDirectory();
            CopyFixture("Directory.Build.props", TempPath("Directory.Build.props"));
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Roslynator.Analyzers", "4.17.0");
            packageFileService.SavePackageManifest(manifest);

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
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

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
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            packageFileService.SavePackageManifest(manifest);

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
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            packageFileService.SavePackageManifest(manifest);

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
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            packageFileService.SavePackageManifest(manifest);

            File.ReadAllText(path).ShouldBe(content.Replace("13.0.1", "13.0.2", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Entity_Encoded_Version_Normalizes_Value_On_Bump()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"Newtonsoft.Json\" Version=\"13&#46;0&#46;1\" />\n  </ItemGroup>\n</Project>\n";
            var path = TempPath("EntityVersion.csproj");
            File.WriteAllText(path, content);
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            packageFileService.SavePackageManifest(manifest);

            File.ReadAllText(path).ShouldBe(content.Replace("13&#46;0&#46;1", "13.0.2", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Crlf_Preserves_Line_Endings()
        {
            ResetTempDirectory();
            var content =
                "<Project>\r\n  <ItemGroup>\r\n    <PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.1\" />\r\n  </ItemGroup>\r\n</Project>\r\n";
            var path = TempPath("CrLf.csproj");
            File.WriteAllText(path, content);
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            packageFileService.SavePackageManifest(manifest);

            File.ReadAllText(path).ShouldBe(content.Replace("13.0.1", "13.0.2", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Lone_Carriage_Return_Preserves_Line_Endings()
        {
            ResetTempDirectory();
            var content =
                "<Project>\r  <ItemGroup>\r    <PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.1\" />\r  </ItemGroup>\r</Project>\r";
            var path = TempPath("CarriageReturn.csproj");
            File.WriteAllText(path, content);
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            packageFileService.SavePackageManifest(manifest);

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
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("PackageB", "2.0.0");
            packageFileService.SavePackageManifest(manifest);

            var expected = content.Replace(
                "Include=\"PackageB\" Version=\"1.0.0\"",
                "Include=\"PackageB\" Version=\"2.0.0\"",
                StringComparison.Ordinal);
            File.ReadAllText(path).ShouldBe(expected);
        }

        [Fact]
        public void With_Child_Version_Condition_Containing_Same_Version_Updates_Only_Value()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"Newtonsoft.Json\">\n      <Version Condition=\"'$(UseV1)' == '13.0.1'\">13.0.1</Version>\n    </PackageReference>\n  </ItemGroup>\n</Project>\n";
            var path = TempPath("ChildCondition.csproj");
            File.WriteAllText(path, content);
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            packageFileService.SavePackageManifest(manifest);

            var expected = content.Replace(">13.0.1<", ">13.0.2<", StringComparison.Ordinal);
            File.ReadAllText(path).ShouldBe(expected);
        }

        [Fact]
        public void With_Attribute_Condition_Containing_Same_Version_Updates_Only_Version()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <ItemGroup>\n    <PackageReference Condition=\"'$(UseV1)' == '13.0.1'\" Include=\"Newtonsoft.Json\" Version=\"13.0.1\" />\n  </ItemGroup>\n</Project>\n";
            var path = TempPath("AttributeCondition.csproj");
            File.WriteAllText(path, content);
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            packageFileService.SavePackageManifest(manifest);

            var expected = content.Replace("Version=\"13.0.1\"", "Version=\"13.0.2\"", StringComparison.Ordinal);
            File.ReadAllText(path).ShouldBe(expected);
        }

        [Fact]
        public void With_Whitespace_Formatted_Child_Version_Preserves_Whitespace()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"Newtonsoft.Json\">\n      <Version>\n        3.0.0\n      </Version>\n    </PackageReference>\n  </ItemGroup>\n</Project>\n";
            var path = TempPath("WhitespaceVersion.csproj");
            File.WriteAllText(path, content);
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "3.0.1");
            packageFileService.SavePackageManifest(manifest);

            File.ReadAllText(path).ShouldBe(content.Replace("3.0.0", "3.0.1", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Whitespace_Padded_Attribute_Version_Preserves_Whitespace()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"Newtonsoft.Json\" Version=\" 3.0.0 \" />\n  </ItemGroup>\n</Project>\n";
            var path = TempPath("WhitespaceAttribute.csproj");
            File.WriteAllText(path, content);
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);

            manifest.SetVersion("Newtonsoft.Json", "3.0.1");
            packageFileService.SavePackageManifest(manifest);

            File.ReadAllText(path).ShouldBe(content.Replace(" 3.0.0 ", " 3.0.1 ", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Out_Of_Bounds_Span_Skips_And_Warns()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.1\" />\n  </ItemGroup>\n</Project>\n";
            var path = TempPath("InvalidSpan.csproj");
            File.WriteAllText(path, content);
            var originalBytes = File.ReadAllBytes(path);
            var (service, sink) = CreateServiceWithSink();
            var manifest = new PackageManifest();
            manifest.Add(
                new PackageVersionEntry
                {
                    PackageId = "Newtonsoft.Json",
                    OriginalVersion = "13.0.1",
                    Version = "13.0.1",
                    FilePath = path,
                    SourceKind = PackageSourceKind.Project,
                    ElementName = "PackageReference",
                    VersionStart = content.Length + 100,
                    VersionLength = 6,
                });
            manifest.RegisterFileText(path, content);
            manifest.SetVersion("Newtonsoft.Json", "13.0.2");

            service.SavePackageManifest(manifest);

            File.ReadAllBytes(path).ShouldBe(originalBytes);
            sink.Events.ShouldContain(logEvent =>
                logEvent.Level == LogEventLevel.Warning
                && logEvent.RenderMessage(null).Contains("recorded span is invalid", StringComparison.Ordinal));
        }

        [Fact]
        public void With_Unapplied_Span_Surfaces_Warning()
        {
            ResetTempDirectory();
            var content =
                "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.1\" />\n  </ItemGroup>\n</Project>\n";
            var path = TempPath("UnappliedSpan.csproj");
            File.WriteAllText(path, content);
            var originalBytes = File.ReadAllBytes(path);
            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = new PackageManifest();
            manifest.Add(
                new PackageVersionEntry
                {
                    PackageId = "Newtonsoft.Json",
                    OriginalVersion = "13.0.1",
                    Version = "13.0.1",
                    FilePath = path,
                    SourceKind = PackageSourceKind.Project,
                    ElementName = "PackageReference",
                    VersionStart = content.Length + 100,
                    VersionLength = 6,
                });
            manifest.RegisterFileText(path, content);

            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            packageFileService.SavePackageManifest(manifest);

            File.ReadAllBytes(path).ShouldBe(originalBytes);
            manifest.HasChanges.ShouldBeTrue();
            manifest.Warnings.ShouldContain(warning =>
                warning.Contains("recorded span is invalid", StringComparison.Ordinal));
        }

        /// <summary>
        /// Writes a minimal project file with the supplied encoding, applies a version bump via
        /// <see cref="PackageFileService"/>, and asserts the file is written back with the same
        /// preamble (byte order mark) and encoding, exercising the read/detect/write path.
        /// </summary>
        private static void AssertBomPreserved_On_Modification(Encoding encoding, string fileName)
        {
            ResetTempDirectory();
            var path = TempPath(fileName);
            const string content =
                "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.1\" />\n  </ItemGroup>\n</Project>\n";
            WriteEncodedFile(path, content, encoding);

            var packageFileService = new PackageFileService(new Mock<ILogger>().Object);
            var manifest = packageFileService.GetPackageManifest(TempDirectory.AbsolutePath);
            manifest.SetVersion("Newtonsoft.Json", "13.0.2");
            packageFileService.SavePackageManifest(manifest);

            var bytes = File.ReadAllBytes(path);
            var expectedPreamble = encoding.GetPreamble();

            bytes.Take(expectedPreamble.Length).ShouldBe(expectedPreamble);

            var decoded = encoding.GetString(bytes, expectedPreamble.Length, bytes.Length - expectedPreamble.Length);
            decoded.ShouldSatisfyAllConditions(
                () => decoded.ShouldContain("13.0.2"),
                () => decoded.ShouldNotContain("13.0.1"));
        }

        private static void WriteEncodedFile(string path, string content, Encoding encoding)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, content, encoding);
        }
    }

    public class GetOffset
    {
        [Fact]
        public void With_Line_Number_Out_Of_Range_Returns_No_Offset()
        {
            var offset = PackageFileService.GetOffset([0], 5, 1);

            offset.ShouldBe(-1);
        }

        [Fact]
        public void With_Valid_Line_And_Position_Returns_Offset()
        {
            var offset = PackageFileService.GetOffset([0, 10], 2, 3);

            offset.ShouldBe(12);
        }
    }

    public class GetVersionSpan
    {
        [Fact]
        public void With_Attribute_Line_Number_Out_Of_Range_Returns_No_Span()
        {
            const string text =
                "<Project>\n  <PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.1\" />\n</Project>\n";
            var attribute = XDocument.Parse(text, LoadOptions.SetLineInfo)
                .Descendants("PackageReference").Single().Attribute("Version")!;

            var (start, length) = PackageFileService.GetVersionSpan(text, [], attribute);

            start.ShouldBe(-1);
            length.ShouldBe(0);
        }

        [Fact]
        public void With_Attribute_Offset_Beyond_Text_Length_Returns_No_Span()
        {
            const string text =
                "<Project>\n  <PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.1\" />\n</Project>\n";
            var attribute = XDocument.Parse(text, LoadOptions.SetLineInfo)
                .Descendants("PackageReference").Single().Attribute("Version")!;

            var (start, length) = PackageFileService.GetVersionSpan(text, [0, text.Length + 100], attribute);

            start.ShouldBe(-1);
            length.ShouldBe(0);
        }

        [Fact]
        public void With_Element_Line_Number_Out_Of_Range_Returns_No_Span()
        {
            const string text = "<Project>\n  <Version>13.0.1</Version>\n</Project>\n";
            var versionElement = XDocument.Parse(text, LoadOptions.SetLineInfo)
                .Descendants("Version").Single();

            var (start, length) = PackageFileService.GetVersionSpan(text, [], versionElement);

            start.ShouldBe(-1);
            length.ShouldBe(0);
        }

        [Fact]
        public void With_Valid_Attribute_Returns_Span()
        {
            const string text = "<PackageReference Version=\"13.0.1\" />";
            var attribute = XDocument.Parse(text, LoadOptions.SetLineInfo)
                .Descendants("PackageReference").Single().Attribute("Version")!;

            var (start, length) = PackageFileService.GetVersionSpan(text, [0], attribute);

            text.Substring(start, length).ShouldBe("13.0.1");
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

    private static (PackageFileService Service, TestLogSink Sink) CreateServiceWithSink()
    {
        var sink = new TestLogSink();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();

        return (new PackageFileService(logger), sink);
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
