// Copyright © Roby Van Damme.

using DotBump.Commands.BumpTools;
using DotBump.Common;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpTools;

public class ToolManifestLocatorTests
{
    public class Resolve
    {
        private static readonly LocalDirectory s_currentDirectory = new("./");

        private static readonly LocalDirectory s_configDirectory = new("./.config");

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
            var rootManifest = Path.Combine(s_currentDirectory.AbsolutePath, "dotnet-tools.json");
            var legacyManifest = Path.Combine(s_configDirectory.AbsolutePath, "dotnet-tools.json");

            s_currentDirectory.EnsureFileCreated("dotnet-tools.json", "{}");
            s_configDirectory.EnsureFileCreated("dotnet-tools.json", "{}");

            try
            {
                var locator = new ToolManifestLocator(new Mock<ILogger>().Object);

                var result = locator.Resolve(null);

                result.ShouldBe(rootManifest);
            }
            finally
            {
                s_currentDirectory.EnsureFileDeleted("dotnet-tools.json");
                s_configDirectory.EnsureFileDeleted("dotnet-tools.json");
            }
        }

        [Fact]
        public void With_Only_Legacy_Manifest_Present_Returns_Legacy_Manifest()
        {
            var legacyManifest = Path.Combine(s_configDirectory.AbsolutePath, "dotnet-tools.json");

            s_currentDirectory.EnsureFileDeleted("dotnet-tools.json");
            s_configDirectory.EnsureFileCreated("dotnet-tools.json", "{}");

            try
            {
                var locator = new ToolManifestLocator(new Mock<ILogger>().Object);

                var result = locator.Resolve(null);

                result.ShouldBe(legacyManifest);
            }
            finally
            {
                s_configDirectory.EnsureFileDeleted("dotnet-tools.json");
            }
        }

        [Fact]
        public void With_No_Manifest_Present_Throws_DotBumpException()
        {
            s_currentDirectory.EnsureFileDeleted("dotnet-tools.json");
            s_configDirectory.EnsureFileDeleted("dotnet-tools.json");

            var locator = new ToolManifestLocator(new Mock<ILogger>().Object);

            var exception = Should.Throw<DotBumpException>(() => locator.Resolve(null));
            exception.Message.ShouldContain("Could not find a tools manifest");
        }
    }
}
