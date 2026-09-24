// Copyright © Roby Van Damme.

using System.ComponentModel.DataAnnotations;
using DotBump.Commands;
using DotBump.Commands.BumpPackages;
using DotBump.Commands.BumpPackages.DataModel;
using DotBump.Commands.BumpPackages.Interfaces;
using DotBump.Common;
using DotBump.Reports;
using Moq;
using Serilog;
using Shouldly;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace DotBump.Tests.Commands.BumpPackages;

public class BumpPackagesCommandTests
{
    public class ExecuteForTestAsync
    {
        [Fact]
        public async Task With_Valid_Settings_Returns_0()
        {
            using var testConsole = new TestConsole();
            var handler = CreateHandler(CreateReport(("MyPackage", "1.0.0")));
            var command = new BumpPackagesCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteForTestAsync(
                CreateContext("packages"),
                new BumpPackagesSettings(),
                CancellationToken.None);

            result.ShouldBe(0);
        }

        [Fact]
        public async Task With_Bumped_Packages_Returns_0_And_Writes_Report()
        {
            using var testConsole = new TestConsole();
            var report = CreateReport(("MyPackage", "1.0.0"));
            report.ReportChanges(CreateBumpedManifest(("MyPackage", "1.1.0")));
            var handler = CreateHandler(report);
            var command = new BumpPackagesCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteForTestAsync(
                CreateContext("packages"),
                new BumpPackagesSettings(),
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => testConsole.Output.ShouldContain("Package versions bumped"));
        }

        [Fact]
        public async Task With_Report_Errors_Returns_1()
        {
            using var testConsole = new TestConsole();
            var report = CreateReport(("MyPackage", "1.0.0"));
            report.ReportErrors([new ValidationResult("bad config")]);
            var handler = CreateHandler(report);
            var command = new BumpPackagesCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteForTestAsync(
                CreateContext("packages"),
                new BumpPackagesSettings(),
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(1),
                () => testConsole.Output.ShouldContain("An error occurred bumping package versions."));
        }

        [Fact]
        public async Task With_Handler_Throws_Returns_1()
        {
            using var testConsole = new TestConsole();
            var handler = new Mock<IBumpPackagesHandler>();
            handler
                .Setup(h => h.HandleAsync(It.IsAny<BumpType>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("boom"));
            var command = new BumpPackagesCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteForTestAsync(
                CreateContext("packages"),
                new BumpPackagesSettings(),
                CancellationToken.None);

            result.ShouldBe(1);
        }

        [Fact]
        public async Task With_Unexpected_Command_Name_Throws_DotBumpException()
        {
            using var testConsole = new TestConsole();
            var handler = CreateHandler(CreateReport(("MyPackage", "1.0.0")));
            var command = new BumpPackagesCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            await Should.ThrowAsync<DotBumpException>(() => command.ExecuteForTestAsync(
                CreateContext("tools"),
                new BumpPackagesSettings(),
                CancellationToken.None));
        }

        private static CommandContext CreateContext(string commandName)
        {
            var remainingArguments = new Mock<IRemainingArguments>();
            return new CommandContext(["bump", commandName], remainingArguments.Object, commandName, null);
        }

        private static Mock<IBumpPackagesHandler> CreateHandler(BumpReport report)
        {
            var handler = new Mock<IBumpPackagesHandler>();
            handler
                .Setup(h => h.HandleAsync(It.IsAny<BumpType>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(report);
            return handler;
        }

        private static BumpReport CreateReport(params (string Id, string Version)[] packages)
        {
            return new BumpReport(CreateManifest(packages), BumpType.Minor);
        }

        private static PackageManifest CreateBumpedManifest(params (string Id, string Version)[] packages)
        {
            var manifest = CreateManifest(packages);
            foreach (var (id, version) in packages)
            {
                manifest.SetVersion(id, version);
            }

            return manifest;
        }

        private static PackageManifest CreateManifest(params (string Id, string Version)[] packages)
        {
            var manifest = new PackageManifest();

            foreach (var (id, version) in packages)
            {
                manifest.Add(
                    new PackageVersionEntry
                    {
                        PackageId = id,
                        OriginalVersion = version,
                        Version = version,
                        FilePath = $"{id}.csproj",
                        SourceKind = PackageSourceKind.Project,
                        ElementName = "PackageReference",
                        VersionAnchor = 0,
                    });
            }

            return manifest;
        }
    }
}
