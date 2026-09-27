// Copyright © Roby Van Damme.

using DotBump.Commands.BumpPackages;
using DotBump.Tests.TestHelpers;
using Shouldly;

namespace DotBump.Tests.Commands.BumpPackages;

public class BumpPackagesSettingsTests
{
    public class Validate
    {
        [Fact]
        public void With_Default_Settings_Returns_Success()
        {
            var settings = new BumpPackagesSettings();

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Existing_Config_File_Returns_Success()
        {
            ResetTempDirectory();
            var configPath = ConfigPath("nuget.config");
            TempDirectory.EnsureFileCreated("nuget.config");
            var settings = new BumpPackagesSettings { NuGetConfigPath = configPath };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Missing_Config_File_Returns_Error()
        {
            ResetTempDirectory();
            var missingPath = ConfigPath("missing.config");
            var settings = new BumpPackagesSettings { NuGetConfigPath = missingPath };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The file {missingPath} does not exist."));
        }

        [Fact]
        public void With_Existing_Repository_Directory_Returns_Success()
        {
            ResetTempDirectory();
            var settings = new BumpPackagesSettings { RepositoryPath = TempDirectory.AbsolutePath };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Missing_Repository_Directory_Returns_Error()
        {
            ResetTempDirectory();
            TempDirectory.EnsureDirectoryDeleted();
            var missingPath = TempDirectory.AbsolutePath;
            var settings = new BumpPackagesSettings { RepositoryPath = missingPath };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The directory {missingPath} does not exist."));
        }

        [Fact]
        public void With_Whitespace_Paths_Returns_Success()
        {
            var settings = new BumpPackagesSettings { NuGetConfigPath = "  ", RepositoryPath = "  " };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }
    }

    private static LocalDirectory TempDirectory => new("./temp/settings");

    private static string ConfigPath(string fileName) => Path.Combine(TempDirectory.AbsolutePath, fileName);

    private static void ResetTempDirectory()
    {
        TempDirectory.EnsureDirectoryDeleted();
        TempDirectory.EnsureDirectoryCreated();
    }
}
