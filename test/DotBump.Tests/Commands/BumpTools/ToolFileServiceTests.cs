// Copyright © Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpTools;
using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Common;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpTools;

public class ToolFileServiceTests
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public class GetToolsManifest
    {
        [Fact]
        public void With_Missing_Manifest_Throws_FileNotFoundException()
        {
            var directory = new LocalDirectory("./.config");
            directory.EnsureFileDeleted("dotnet-tools.json");

            var service = new ToolFileService(new Mock<ILogger>().Object);

            Should.Throw<FileNotFoundException>(() => service.GetToolsManifest());
        }

        [Fact]
        public void When_File_Exists_Returns_ToolsManifest()
        {
            var directory = new LocalDirectory("./.config");
            var manifest = new ToolsManifest
            {
                Version = 1,
                IsRoot = true,
                Tools = new Dictionary<string, ToolManifestEntry>
                {
                    ["mytool"] = new() { Version = "1.0.0", Commands = ["mytool"], RollForward = false },
                },
            };
            directory.EnsureFileCreated("dotnet-tools.json", JsonSerializer.Serialize(manifest, s_serializerOptions));

            var service = new ToolFileService(new Mock<ILogger>().Object);
            var result = service.GetToolsManifest();

            result.ShouldSatisfyAllConditions(
                () => result.ShouldNotBeNull(),
                () => result.Version.ShouldBe(1),
                () => result.Tools.ShouldContainKey("mytool"),
                () => result.Tools["mytool"].Version.ShouldBe("1.0.0"));
        }

        [Fact]
        public void With_Invalid_Manifest_Throws_DotBumpException()
        {
            var directory = new LocalDirectory("./.config");
            directory.EnsureFileCreated("dotnet-tools.json", "null");

            var service = new ToolFileService(new Mock<ILogger>().Object);

            Should.Throw<DotBumpException>(() => service.GetToolsManifest());
        }
    }

    public class SaveToolsManifest
    {
        [Fact]
        public void With_Null_Manifest_Throws_ArgumentNullException()
        {
            var service = new ToolFileService(new Mock<ILogger>().Object);

            Should.Throw<ArgumentNullException>(() => service.SaveToolsManifest(null!));
        }

        [Fact]
        public void With_Missing_Directory_Throws_DotBumpException()
        {
            var directory = new LocalDirectory("./.config");
            directory.EnsureDirectoryDeleted();

            var manifest = new ToolsManifest
            {
                Version = 1,
                IsRoot = true,
                Tools = new Dictionary<string, ToolManifestEntry>
                {
                    ["mytool"] = new() { Version = "2.0.0", Commands = ["mytool"], RollForward = false },
                },
            };

            var service = new ToolFileService(new Mock<ILogger>().Object);

            try
            {
                Should.Throw<DotBumpException>(() => service.SaveToolsManifest(manifest));
            }
            finally
            {
                directory.EnsureDirectoryCreated();
            }
        }

        [Fact]
        public void With_Valid_Manifest_Persists_Manifest()
        {
            var directory = new LocalDirectory("./.config");
            directory.EnsureDirectoryCreated();
            var manifest = new ToolsManifest
            {
                Version = 1,
                IsRoot = true,
                Tools = new Dictionary<string, ToolManifestEntry>
                {
                    ["mytool"] = new() { Version = "2.0.0", Commands = ["mytool"], RollForward = false },
                },
            };

            var service = new ToolFileService(new Mock<ILogger>().Object);
            service.SaveToolsManifest(manifest);

            var loaded = service.GetToolsManifest();
            loaded.Tools["mytool"].Version.ShouldBe("2.0.0");
        }
    }
}
