// Copyright © Roby Van Damme.

using System.ComponentModel.DataAnnotations;
using DotBump.Commands;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Reports;
using Shouldly;
using static DotBump.Tests.TestHelpers.TestPackageManifestFactory;

namespace DotBump.Tests.Reports;

public class MarkdownReportFormatterTests
{
    public class Format
    {
        [Fact]
        public void With_Packages_Changes_Returns_Heading_And_Sorted_List()
        {
            var manifest = CreateManifest(
                ("Newtonsoft.Json", "12.0.1"),
                ("dotnet-outdated-tool", "4.6.0"));
            manifest.SetVersion("Newtonsoft.Json", "13.0.3");
            manifest.SetVersion("dotnet-outdated-tool", "4.6.1");
            var report = new BumpReport(manifest, BumpType.Minor);
            report.ReportChanges(manifest);

            var markdown = MarkdownReportFormatter.Format(report);

            markdown.ShouldBe(
                "## Packages\n" +
                "\n" +
                "- dotnet-outdated-tool: 4.6.0 → 4.6.1\n" +
                "- Newtonsoft.Json: 12.0.1 → 13.0.3\n" +
                "\n");
        }

        [Fact]
        public void With_Sdk_Change_Returns_Heading_Without_Id()
        {
            var report = new BumpReport(new Sdk("8.0.405", "disable"), BumpType.Minor);
            report.ReportChanges(new Release("8.0", "8.0.406", "active", false));

            var markdown = MarkdownReportFormatter.Format(report);

            markdown.ShouldBe(
                "## SDK\n" +
                "\n" +
                "- 8.0.405 → 8.0.406\n" +
                "\n");
        }

        [Fact]
        public void With_Tools_Change_Returns_Heading_And_List()
        {
            var toolsManifest = new ToolsManifest
            {
                Tools = new Dictionary<string, ToolManifestEntry>
                {
                    ["dotnet-outdated-tool"] = new() { Version = "4.6.0", Commands = ["dotnet-outdated"] },
                },
            };
            var report = new BumpReport(toolsManifest, BumpType.Minor);
            toolsManifest.Tools["dotnet-outdated-tool"].Version = "4.6.1";
            report.ReportChanges(toolsManifest);

            var markdown = MarkdownReportFormatter.Format(report);

            markdown.ShouldBe(
                "## Tools\n" +
                "\n" +
                "- dotnet-outdated-tool: 4.6.0 → 4.6.1\n" +
                "\n");
        }

        [Fact]
        public void With_Unchanged_And_Changed_Entries_Only_Lists_Changed()
        {
            var manifest = CreateManifest(
                ("Changed.Package", "1.0.0"),
                ("Unchanged.Package", "1.0.0"));
            manifest.SetVersion("Changed.Package", "2.0.0");
            var report = new BumpReport(manifest, BumpType.Minor);
            report.ReportChanges(manifest);

            var markdown = MarkdownReportFormatter.Format(report);

            markdown.ShouldBe(
                "## Packages\n" +
                "\n" +
                "- Changed.Package: 1.0.0 → 2.0.0\n" +
                "\n");
        }

        [Fact]
        public void With_Warnings_Returns_Warnings_Section()
        {
            var report = new BumpReport(CreateManifest(), BumpType.Minor);
            report.ReportWarnings(["Skipping 'Floating.Package' because version '1.2.*' is not supported."]);

            var markdown = MarkdownReportFormatter.Format(report);

            markdown.ShouldBe(
                "## Packages\n" +
                "\n" +
                "### Warnings\n" +
                "\n" +
                "- Skipping 'Floating.Package' because version '1.2.\\*' is not supported.\n" +
                "\n");
        }

        [Fact]
        public void With_Errors_Returns_Errors_Section()
        {
            var report = new BumpReport(CreateManifest(), BumpType.Minor);
            report.ReportErrors([new ValidationResult("The file ./nuget.config does not exist.")]);

            var markdown = MarkdownReportFormatter.Format(report);

            markdown.ShouldBe(
                "## Packages\n" +
                "\n" +
                "### Errors\n" +
                "\n" +
                "- The file ./nuget.config does not exist.\n" +
                "\n");
        }

        [Fact]
        public void With_Id_Containing_Underscores_Escapes_Underscores()
        {
            var manifest = CreateManifest(("Company._Core_.Helpers", "1.0.0"));
            manifest.SetVersion("Company._Core_.Helpers", "2.0.0");
            var report = new BumpReport(manifest, BumpType.Minor);
            report.ReportChanges(manifest);

            var markdown = MarkdownReportFormatter.Format(report);

            markdown.ShouldBe(
                "## Packages\n" +
                "\n" +
                "- Company.\\_Core\\_.Helpers: 1.0.0 → 2.0.0\n" +
                "\n");
        }

        [Fact]
        public void With_Warning_Containing_Markdown_Metacharacters_Escapes_Them()
        {
            var report = new BumpReport(CreateManifest(), BumpType.Minor);
            report.ReportWarnings(
            [
                "Skipping 'Package' because version '[1.0.0, 2.0.0)' and `floating` are not supported."
            ]);

            var markdown = MarkdownReportFormatter.Format(report);

            markdown.ShouldBe(
                "## Packages\n" +
                "\n" +
                "### Warnings\n" +
                "\n" +
                "- Skipping 'Package' because version '\\[1.0.0, 2.0.0)' and \\`floating\\` are not supported.\n" +
                "\n");
        }

        [Fact]
        public void With_Warning_Containing_Line_Breaks_Returns_Single_Bullet_Line()
        {
            var report = new BumpReport(CreateManifest(), BumpType.Minor);
            report.ReportWarnings(["first line\n\n## Injected heading\nsecond line"]);

            var markdown = MarkdownReportFormatter.Format(report);

            markdown.ShouldBe(
                "## Packages\n" +
                "\n" +
                "### Warnings\n" +
                "\n" +
                "- first line ## Injected heading second line\n" +
                "\n");
        }

        [Fact]
        public void With_No_Changes_Warnings_Or_Errors_Returns_Null()
        {
            var report = new BumpReport(CreateManifest(("Unchanged.Package", "1.0.0")), BumpType.Minor);

            MarkdownReportFormatter.Format(report).ShouldBeNull();
        }

        [Fact]
        public void With_Changes_Returns_Lf_Only_Line_Endings()
        {
            var manifest = CreateManifest(("Newtonsoft.Json", "12.0.1"));
            manifest.SetVersion("Newtonsoft.Json", "13.0.3");
            var report = new BumpReport(manifest, BumpType.Minor);
            report.ReportChanges(manifest);

            var markdown = MarkdownReportFormatter.Format(report);

            markdown.ShouldNotBeNull();
            markdown.ShouldNotContain('\r');
        }

        [Fact]
        public void With_Null_Report_Throws_ArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => MarkdownReportFormatter.Format(null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void With_Empty_CommandName_Throws_ArgumentException(string? commandName)
        {
            var report = new BumpReport(CreateManifest(("Newtonsoft.Json", "12.0.1")), BumpType.Minor)
            {
                CommandName = commandName!,
            };

            Should.Throw<ArgumentException>(() => MarkdownReportFormatter.Format(report));
        }
    }
}
