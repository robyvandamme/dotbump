// Copyright © Roby Van Damme.

using DotBump.Commands.BumpDocker;
using DotBump.Tests.TestHelpers;
using Shouldly;

namespace DotBump.Tests.Commands.BumpDocker;

public class BumpDockerSettingsTests
{
    public class Validate
    {
        [Fact]
        public void With_Default_Settings_Returns_Success()
        {
            var settings = new BumpDockerSettings();

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Existing_Repository_Directory_Returns_Success()
        {
            ResetTempDirectory();
            var settings = new BumpDockerSettings { RepositoryPath = TempDirectory.AbsolutePath };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Missing_Repository_Directory_Returns_Error()
        {
            ResetTempDirectory();
            TempDirectory.EnsureDirectoryDeleted();
            var missingPath = TempDirectory.AbsolutePath;
            var settings = new BumpDockerSettings { RepositoryPath = missingPath };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The directory {missingPath} does not exist."));
        }

        [Fact]
        public void With_Invalid_Repository_Path_Returns_Error()
        {
            var invalidPath = "bad\0path";
            var settings = new BumpDockerSettings { RepositoryPath = invalidPath };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The directory {invalidPath} is not a valid path."));
        }

        [Fact]
        public void With_Whitespace_Path_Returns_Success()
        {
            var settings = new BumpDockerSettings { RepositoryPath = "  " };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Existing_Excluded_Directory_Returns_Success()
        {
            ResetTempDirectory();
            var excludePath = Path.Combine(TempDirectory.AbsolutePath, "excluded");
            Directory.CreateDirectory(excludePath);
            var settings = new BumpDockerSettings { Exclude = [excludePath] };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Missing_Excluded_Directory_Returns_Error()
        {
            ResetTempDirectory();
            var missingPath = Path.Combine(TempDirectory.AbsolutePath, "missing");
            var settings = new BumpDockerSettings { Exclude = [missingPath] };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The directory {missingPath} does not exist."));
        }

        [Fact]
        public void With_Invalid_Excluded_Path_Returns_Error()
        {
            var invalidPath = "bad\0path";
            var settings = new BumpDockerSettings { Exclude = [invalidPath] };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The directory {invalidPath} is not a valid path."));
        }

        [Fact]
        public void With_Whitespace_Excluded_Path_Returns_Success()
        {
            var settings = new BumpDockerSettings { Exclude = ["  "] };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }
    }

    private static LocalDirectory TempDirectory => new("./temp/settings-docker");

    private static void ResetTempDirectory()
    {
        TempDirectory.EnsureDirectoryDeleted();
        TempDirectory.EnsureDirectoryCreated();
    }
}
