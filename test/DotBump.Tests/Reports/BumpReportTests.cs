// Copyright © Roby Van Damme.

using System.Text.Json;
using DotBump.Commands;
using DotBump.Commands.BumpPackages.DataModel;
using DotBump.Reports;
using DotBump.Tests.TestHelpers;
using Shouldly;

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
    }

    private static BumpReport CreateReport()
    {
        return new BumpReport(new PackageManifest(), BumpType.Minor);
    }
}
