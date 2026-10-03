// Copyright © Roby Van Damme.

using DotBump.Commands.BumpTools;
using DotBump.Tests.TestHelpers;
using Shouldly;

namespace DotBump.Tests.Commands.BumpTools;

public class BumpToolsSettingsTests
{
    public class Validate
    {
        [Fact]
        public void With_Default_Settings_Returns_Success()
        {
            var settings = new BumpToolsSettings();

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Existing_Config_File_Returns_Success()
        {
            ResetTempDirectory();
            var configPath = ConfigPath("nuget.config");
            TempDirectory.EnsureFileCreated("nuget.config");
            var settings = new BumpToolsSettings { NuGetConfigPath = configPath };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Missing_Config_File_Returns_Error()
        {
            ResetTempDirectory();
            var missingPath = ConfigPath("missing.config");
            var settings = new BumpToolsSettings { NuGetConfigPath = missingPath };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The file {missingPath} does not exist."));
        }

        [Fact]
        public void With_Existing_Manifest_File_Returns_Success()
        {
            ResetTempDirectory();
            var manifestPath = ConfigPath("dotnet-tools.json");
            TempDirectory.EnsureFileCreated("dotnet-tools.json");
            var settings = new BumpToolsSettings { ToolManifestPath = manifestPath };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Missing_Manifest_File_Returns_Error()
        {
            ResetTempDirectory();
            var missingPath = ConfigPath("missing-tools.json");
            var settings = new BumpToolsSettings { ToolManifestPath = missingPath };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The file {missingPath} does not exist."));
        }

        [Fact]
        public void With_Whitespace_Paths_Returns_Success()
        {
            var settings = new BumpToolsSettings { NuGetConfigPath = "  ", ToolManifestPath = "  " };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Invalid_Config_Path_Returns_Error()
        {
            var invalidPath = "bad\0path.config";
            var settings = new BumpToolsSettings { NuGetConfigPath = invalidPath };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The file {invalidPath} is not a valid path."));
        }

        [Fact]
        public void With_Invalid_Manifest_Path_Returns_Error()
        {
            var invalidPath = "bad\0path.json";
            var settings = new BumpToolsSettings { ToolManifestPath = invalidPath };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The file {invalidPath} is not a valid path."));
        }
    }

    private static LocalDirectory TempDirectory => new("./temp/settings-tools");

    private static string ConfigPath(string fileName) => Path.Combine(TempDirectory.AbsolutePath, fileName);

    private static void ResetTempDirectory()
    {
        TempDirectory.EnsureDirectoryDeleted();
        TempDirectory.EnsureDirectoryCreated();
    }
}
