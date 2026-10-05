// Copyright © Roby Van Damme.

using System.Text.Json;
using DotBump.Commands;
using DotBump.Commands.BumpPackages.DataModel;
using DotBump.Reports;
using DotBump.Tests.TestHelpers;
using Shouldly;
using static DotBump.Tests.TestHelpers.TestPackageManifestFactory;

namespace DotBump.Tests.Reports;

public class BumpReportTests
{
    public class ReportWarnings
    {
        [Fact]
        public void With_Warnings_Adds_Them_To_The_Report()
        {
            var report = CreateReport();

            report.ReportWarnings(
            [
                "Skipping 'Package.A' because version '1.0.*' is not supported.",
                "Skipping 'Package.B' because version '2.0.0.1' cannot be parsed as a semantic version."
            ]);

            report.Warnings.ShouldSatisfyAllConditions(
                () => report.Warnings.ShouldContain("Skipping 'Package.A' because version '1.0.*' is not supported."),
                () => report.Warnings.ShouldContain(
                    "Skipping 'Package.B' because version '2.0.0.1' cannot be parsed as a semantic version."));
        }

        [Fact]
        public void With_Null_Warnings_Throws_ArgumentNullException()
        {
            var report = CreateReport();

            Should.Throw<ArgumentNullException>(() => report.ReportWarnings(null!));
        }
    }

    public class WriteToFileAsync
    {
        [Fact]
        public async Task With_Warnings_Serializes_Warnings_Array()
        {
            const string warning =
                "Skipping 'Other.Package' because version '1.0.*' is not supported.";
            var report = CreateReport();
            report.ReportWarnings([warning]);
            var outputDirectory = new LocalDirectory("./temp/report");
            Directory.CreateDirectory(outputDirectory.AbsolutePath);
            var path = Path.Combine(outputDirectory.AbsolutePath, "report.json");

            try
            {
                await report.WriteToFileAsync(path);

                using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
                document.RootElement.TryGetProperty("warnings", out var warnings).ShouldBeTrue();
                warnings.EnumerateArray()
                    .Select(entry => entry.GetString())
                    .ShouldContain(warning);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task With_Apostrophes_Serializes_Without_Unicode_Escaping()
        {
            const string warning =
                "Skipping 'Other.Package' because version '1.0.*' is not supported.";
            var report = CreateReport();
            report.ReportWarnings([warning]);
            var outputDirectory = new LocalDirectory("./temp/report");
            Directory.CreateDirectory(outputDirectory.AbsolutePath);
            var path = Path.Combine(outputDirectory.AbsolutePath, "report.json");

            try
            {
                await report.WriteToFileAsync(path);

                var json = await File.ReadAllTextAsync(path);
                json.ShouldSatisfyAllConditions(
                    () => json.ShouldContain("'Other.Package'"),
                    () => json.ShouldNotContain("\\u0027"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task With_Markdown_Output_And_Content_Writes_Markdown_Report()
        {
            var manifest = CreateManifest(("Newtonsoft.Json", "12.0.1"));
            manifest.SetVersion("Newtonsoft.Json", "13.0.3");
            var report = new BumpReport(manifest, BumpType.Minor);
            report.ReportChanges(manifest);
            var outputDirectory = new LocalDirectory("./temp/report");
            Directory.CreateDirectory(outputDirectory.AbsolutePath);
            var path = Path.Combine(outputDirectory.AbsolutePath, "report.md");

            try
            {
                await report.WriteToFileAsync(path);

                File.Exists(path).ShouldBeTrue();
                var markdown = await File.ReadAllTextAsync(path);
                markdown.ShouldBe(
                    "## Packages\n" +
                    "\n" +
                    "- Newtonsoft.Json: 12.0.1 → 13.0.3\n" +
                    "\n");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task With_Markdown_Output_And_No_Content_Does_Not_Write_File()
        {
            var report = new BumpReport(CreateManifest(("Unchanged.Package", "1.0.0")), BumpType.Minor);
            var outputDirectory = new LocalDirectory("./temp/report");
            Directory.CreateDirectory(outputDirectory.AbsolutePath);
            var path = Path.Combine(outputDirectory.AbsolutePath, "empty-report.md");
            File.Delete(path);

            await report.WriteToFileAsync(path);

            File.Exists(path).ShouldBeFalse();
        }

        [Fact]
        public async Task With_Markdown_Output_And_No_Content_Deletes_Existing_File()
        {
            var report = new BumpReport(CreateManifest(("Unchanged.Package", "1.0.0")), BumpType.Minor);
            var outputDirectory = new LocalDirectory("./temp/report");
            Directory.CreateDirectory(outputDirectory.AbsolutePath);
            var path = Path.Combine(outputDirectory.AbsolutePath, "stale-report.md");
            await File.WriteAllTextAsync(path, "report left over from a previous run");

            try
            {
                await report.WriteToFileAsync(path);

                File.Exists(path).ShouldBeFalse();
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public async Task With_Unknown_Extension_Writes_Json()
        {
            var report = CreateReport();
            report.ReportWarnings(["Skipping 'Other.Package' because version '1.0.*' is not supported."]);
            var outputDirectory = new LocalDirectory("./temp/report");
            Directory.CreateDirectory(outputDirectory.AbsolutePath);
            var path = Path.Combine(outputDirectory.AbsolutePath, "report.txt");

            try
            {
                await report.WriteToFileAsync(path);

                using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
                document.RootElement.TryGetProperty("warnings", out _).ShouldBeTrue();
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task With_Json_Output_Does_Not_Write_Bom()
        {
            var report = CreateReport();
            report.ReportWarnings(["Skipping 'Other.Package' because version '1.0.*' is not supported."]);
            var outputDirectory = new LocalDirectory("./temp/report");
            Directory.CreateDirectory(outputDirectory.AbsolutePath);
            var path = Path.Combine(outputDirectory.AbsolutePath, "no-bom-report.json");

            try
            {
                await report.WriteToFileAsync(path);

                HasUtf8Bom(await File.ReadAllBytesAsync(path)).ShouldBeFalse();
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task With_Markdown_Output_Does_Not_Write_Bom()
        {
            var manifest = CreateManifest(("Newtonsoft.Json", "12.0.1"));
            manifest.SetVersion("Newtonsoft.Json", "13.0.3");
            var report = new BumpReport(manifest, BumpType.Minor);
            report.ReportChanges(manifest);
            var outputDirectory = new LocalDirectory("./temp/report");
            Directory.CreateDirectory(outputDirectory.AbsolutePath);
            var path = Path.Combine(outputDirectory.AbsolutePath, "no-bom-report.md");

            try
            {
                await report.WriteToFileAsync(path);

                HasUtf8Bom(await File.ReadAllBytesAsync(path)).ShouldBeFalse();
            }
            finally
            {
                File.Delete(path);
            }
        }
    }

    private static bool HasUtf8Bom(byte[] bytes)
    {
        return bytes is [0xEF, 0xBB, 0xBF, ..];
    }

    private static BumpReport CreateReport()
    {
        return new BumpReport(new PackageManifest(), BumpType.Minor);
    }
}
