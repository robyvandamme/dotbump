// Copyright © Roby Van Damme.

using System.Net;
using DotBump.Commands;
using DotBump.Commands.BumpPackages;
using DotBump.Commands.BumpPackages.DataModel;
using DotBump.Commands.BumpPackages.Interfaces;
using DotBump.Common;
using DotBump.NuGet;
using DotBump.NuGet.DataModel.NuGetClientConfiguration;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.NuGet.DataModel.NuGetService;
using DotBump.NuGet.Interfaces;
using DotBump.Reports;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

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
        public async Task With_Markup_Unsafe_Repository_Path_Returns_0_And_Writes_Literal_Path()
        {
            using var testConsole = new TestConsole().Width(500);
            var handler = CreateHandler(CreateReport(("MyPackage", "1.0.0")));
            var command = new BumpPackagesCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteForTestAsync(
                CreateContext("packages"),
                new BumpPackagesSettings { RepositoryPath = "./[draft]/app" },
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => testConsole.Output.ShouldContain("[draft]"));
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
                .Setup(h => h.HandleAsync(It.IsAny<BumpType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>()))
                .ThrowsAsync(new InvalidOperationException("boom"));
            var command = new BumpPackagesCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteForTestAsync(
                CreateContext("packages"),
                new BumpPackagesSettings(),
                CancellationToken.None);

            result.ShouldBe(1);
        }

        [Fact]
        public async Task With_Service_Index_Failure_Returns_1_Without_Saving()
        {
            using var testConsole = new TestConsole();
            var client = new Mock<INuGetClient>();
            client
                .Setup(c => c.GetServiceIndexAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("connection refused"));
            var (command, fileService, commandLogger) = CreateCommandWithClient(testConsole, client.Object);

            var result = await command.ExecuteForTestAsync(
                CreateContext("packages"),
                new BumpPackagesSettings(),
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(1),
                () => fileService.Verify(s => s.SavePackageManifest(It.IsAny<PackageManifest>()), Times.Never),
                () => commandLogger.Verify(
                    l => l.Error(It.IsAny<Exception>(), "An error occurred while trying to bump the packages"),
                    Times.Once));
        }

        [Fact]
        public async Task With_Non_404_Package_Information_Failure_Returns_1_Without_Saving()
        {
            using var testConsole = new TestConsole();
            var client = new Mock<INuGetClient>();
            client
                .Setup(c => c.GetServiceIndexAsync(It.IsAny<string>()))
                .ReturnsAsync(new ServiceIndex
                {
                    Version = "3.0.0",
                    Resources = [new Resource { Id = "https://example.com/reg", Type = "RegistrationsBaseUrl" }],
                });
            client
                .Setup(c => c.GetPackageInformationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("server error", null, HttpStatusCode.InternalServerError));
            var (command, fileService, commandLogger) = CreateCommandWithClient(testConsole, client.Object);

            var result = await command.ExecuteForTestAsync(
                CreateContext("packages"),
                new BumpPackagesSettings(),
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(1),
                () => fileService.Verify(s => s.SavePackageManifest(It.IsAny<PackageManifest>()), Times.Never),
                () => commandLogger.Verify(
                    l => l.Error(It.IsAny<Exception>(), "An error occurred while trying to bump the packages"),
                    Times.Once));
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

        [Fact]
        public async Task With_Excluded_Directories_Forwards_Them_To_The_Handler()
        {
            using var testConsole = new TestConsole();
            var handler = CreateHandler(CreateReport(("MyPackage", "1.0.0")));
            var command = new BumpPackagesCommand(testConsole, Mock.Of<ILogger>(), handler.Object);
            var excludedPath = Path.GetFullPath("./excluded");

            var result = await command.ExecuteForTestAsync(
                CreateContext("packages"),
                new BumpPackagesSettings { Exclude = ["./excluded"] },
                CancellationToken.None);

            result.ShouldBe(0);
            handler.Verify(
                h => h.HandleAsync(
                    It.IsAny<BumpType>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.Is<IReadOnlyCollection<string>>(paths => paths.Contains(excludedPath))),
                Times.Once);
        }

        [Fact]
        public async Task With_Mixed_Case_Package_Id_Bumps_From_Live_NuGet_Feed()
        {
            var tempDirectory = new LocalDirectory("./temp/live-packages");
            tempDirectory.EnsureDirectoryDeleted();
            tempDirectory.EnsureDirectoryCreated();
            var projectPath = Path.Combine(tempDirectory.AbsolutePath, "App.csproj");
            var outputPath = Path.Combine(tempDirectory.AbsolutePath, "packages.md");
            await File.WriteAllTextAsync(
                projectPath,
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Newtonsoft.Json\" Version=\"12.0.1\" />\n  </ItemGroup>\n</Project>\n");

            try
            {
                var logger = new Mock<ILogger>().Object;
                using var testConsole = new TestConsole();
                var handler = new BumpPackagesHandler(
                    new PackageFileService(logger),
                    new NuGetConfigFileService(logger),
                    new PackageVersionResolver(new NuGetClientFactory(logger), new NuGetReleaseFinder(logger), logger),
                    new NuGetConfigValidator(logger),
                    logger);
                var command = new BumpPackagesCommand(testConsole, logger, handler);

                var result = await command.ExecuteForTestAsync(
                    CreateContext("packages"),
                    new BumpPackagesSettings { RepositoryPath = tempDirectory.AbsolutePath, Output = outputPath },
                    CancellationToken.None);

                result.ShouldBe(0);
                (await File.ReadAllTextAsync(projectPath)).ShouldContain("Version=\"12.0.3\"");
            }
            finally
            {
                tempDirectory.EnsureDirectoryDeleted();
            }
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
                .Setup(h => h.HandleAsync(It.IsAny<BumpType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>()))
                .ReturnsAsync(report);
            return handler;
        }

        private static (BumpPackagesCommand Command, Mock<IPackageFileService> FileService, Mock<ILogger> Logger)
            CreateCommandWithClient(TestConsole testConsole, INuGetClient client)
        {
            var fileService = new Mock<IPackageFileService>();
            fileService
                .Setup(s => s.GetPackageManifest(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>()))
                .Returns(TestPackageManifestFactory.CreateManifest(("MyPackage", "1.0.0")));

            var configFileService = new Mock<INuGetConfigFileService>();
            configFileService
                .Setup(s => s.GetNuGetConfiguration(It.IsAny<string>()))
                .Returns(new NuGetConfig
                {
                    PackageSources =
                    [
                        new PackageSource
                        {
                            Key = "feed",
                            Value = "https://feeds.example.com/index.json",
                            ProtocolVersion = "3",
                        },
                    ],
                });

            var validator = new Mock<INuGetConfigValidator>();
            validator.Setup(v => v.Validate(It.IsAny<NuGetConfig>())).Returns([]);

            var clientFactory = new Mock<INuGetClientFactory>();
            clientFactory.Setup(f => f.CreateNuGetClient(It.IsAny<NuGetClientConfig>())).Returns(client);

            var handlerLogger = new Mock<ILogger>().Object;
            var resolver = new PackageVersionResolver(
                clientFactory.Object,
                new NuGetReleaseFinder(handlerLogger),
                handlerLogger);
            var handler = new BumpPackagesHandler(
                fileService.Object,
                configFileService.Object,
                resolver,
                validator.Object,
                handlerLogger);

            var commandLogger = new Mock<ILogger>();
            var command = new BumpPackagesCommand(testConsole, commandLogger.Object, handler);
            return (command, fileService, commandLogger);
        }

        private static BumpReport CreateReport(params (string Id, string Version)[] packages)
        {
            return new BumpReport(TestPackageManifestFactory.CreateManifest(packages), BumpType.Minor);
        }

        private static PackageManifest CreateBumpedManifest(params (string Id, string Version)[] packages)
        {
            var manifest = TestPackageManifestFactory.CreateManifest(packages);
            foreach (var (id, version) in packages)
            {
                manifest.SetVersion(id, version);
            }

            return manifest;
        }
    }

    public class ExcludeOption
    {
        [Fact]
        public async Task With_Repeated_Exclude_Options_Forwards_Each_Directory()
        {
            var tempRoot = new LocalDirectory("./temp/exclude-option");
            tempRoot.EnsureDirectoryDeleted();
            var first = new LocalDirectory("./temp/exclude-option/first");
            var second = new LocalDirectory("./temp/exclude-option/second");
            first.EnsureDirectoryCreated();
            second.EnsureDirectoryCreated();

            try
            {
                using var testConsole = new TestConsole();
                var handler = new Mock<IBumpPackagesHandler>();
                handler
                    .Setup(h => h.HandleAsync(
                        It.IsAny<BumpType>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<IReadOnlyCollection<string>>()))
                    .ReturnsAsync(new BumpReport(
                        TestPackageManifestFactory.CreateManifest(("MyPackage", "1.0.0")),
                        BumpType.Minor));

                IAnsiConsole console = testConsole;
                var logger = Mock.Of<ILogger>();
                var bumpPackagesHandler = handler.Object;

                var app = new CommandApp();
                app.Configure(config =>
                {
                    config.PropagateExceptions();
                    config.Settings.Registrar.RegisterInstance(console);
                    config.Settings.Registrar.RegisterInstance(logger);
                    config.Settings.Registrar.RegisterInstance(bumpPackagesHandler);
                    config.AddCommand<BumpPackagesCommand>("packages");
                });

                var exitCode = await app.RunAsync(
                    ["packages", "--exclude", first.AbsolutePath, "--exclude", second.AbsolutePath]);

                exitCode.ShouldBe(0);
                handler.Verify(
                    h => h.HandleAsync(
                        It.IsAny<BumpType>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.Is<IReadOnlyCollection<string>>(paths =>
                            paths.Count == 2
                            && paths.Contains(first.AbsolutePath)
                            && paths.Contains(second.AbsolutePath))),
                    Times.Once);
            }
            finally
            {
                tempRoot.EnsureDirectoryDeleted();
            }
        }
    }
}
