// Copyright © Roby Van Damme.

using DotBump.Commands.BumpTools;
using DotBump.Common;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpTools;

public class ToolManifestLocatorTests
{
    public class Resolve
    {
        [Fact]
        public void With_Explicit_Path_Returns_Full_Path()
        {
            var locator = new ToolManifestLocator(new Mock<ILogger>().Object);

            var result = locator.Resolve("./custom/dotnet-tools.json");

            result.ShouldBe(Path.GetFullPath("./custom/dotnet-tools.json"));
        }

        [Fact]
        public void With_Root_Manifest_Present_Returns_Root_Manifest()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var rootManifest = Path.Combine(currentDirectory, "dotnet-tools.json");
            var legacyManifest = Path.Combine(currentDirectory, ".config", "dotnet-tools.json");

            File.WriteAllText(rootManifest, "{}");
            Directory.CreateDirectory(Path.GetDirectoryName(legacyManifest)!);
            File.WriteAllText(legacyManifest, "{}");

            try
            {
                var locator = new ToolManifestLocator(new Mock<ILogger>().Object);

                var result = locator.Resolve(null);

                result.ShouldBe(rootManifest);
            }
            finally
            {
                File.Delete(rootManifest);
                File.Delete(legacyManifest);
            }
        }

        [Fact]
        public void With_Only_Legacy_Manifest_Present_Returns_Legacy_Manifest()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var rootManifest = Path.Combine(currentDirectory, "dotnet-tools.json");
            var legacyManifest = Path.Combine(currentDirectory, ".config", "dotnet-tools.json");

            File.Delete(rootManifest);
            Directory.CreateDirectory(Path.GetDirectoryName(legacyManifest)!);
            File.WriteAllText(legacyManifest, "{}");

            try
            {
                var locator = new ToolManifestLocator(new Mock<ILogger>().Object);

                var result = locator.Resolve(null);

                result.ShouldBe(legacyManifest);
            }
            finally
            {
                File.Delete(legacyManifest);
            }
        }

        [Fact]
        public void With_No_Manifest_Present_Throws_DotBumpException()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var rootManifest = Path.Combine(currentDirectory, "dotnet-tools.json");
            var legacyManifest = Path.Combine(currentDirectory, ".config", "dotnet-tools.json");

            File.Delete(rootManifest);
            File.Delete(legacyManifest);

            var locator = new ToolManifestLocator(new Mock<ILogger>().Object);

            var exception = Should.Throw<DotBumpException>(() => locator.Resolve(null));
            exception.Message.ShouldContain("Could not find a tools manifest");
        }
    }
}
