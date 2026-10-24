// Copyright © Roby Van Damme.

using DotBump.Commands.BumpLanguage;
using DotBump.Tests.TestHelpers;
using Shouldly;

namespace DotBump.Tests.Commands.BumpLanguage;

public class BumpLanguageSettingsTests
{
    public class Validate
    {
        [Fact]
        public void With_Default_Settings_Returns_Success()
        {
            var settings = new BumpLanguageSettings();

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Existing_GlobalJson_File_Returns_Success()
        {
            ResetTempDirectory();
            var globalJsonPath = ConfigPath("global.json");
            TempDirectory.EnsureFileCreated("global.json");
            var settings = new BumpLanguageSettings { GlobalJsonPath = globalJsonPath };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Missing_GlobalJson_File_Returns_Error()
        {
            ResetTempDirectory();
            var missingPath = ConfigPath("missing.json");
            var settings = new BumpLanguageSettings { GlobalJsonPath = missingPath };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The file {missingPath} does not exist."));
        }

        [Fact]
        public void With_Invalid_GlobalJson_Path_Returns_Error()
        {
            var invalidPath = "bad\0path.json";
            var settings = new BumpLanguageSettings { GlobalJsonPath = invalidPath };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The file {invalidPath} is not a valid path."));
        }

        [Fact]
        public void With_Whitespace_GlobalJson_Path_Returns_Success()
        {
            var settings = new BumpLanguageSettings { GlobalJsonPath = "  " };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Existing_Repository_Directory_Returns_Success()
        {
            ResetTempDirectory();
            var settings = new BumpLanguageSettings { RepositoryPath = TempDirectory.AbsolutePath };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Missing_Repository_Directory_Returns_Error()
        {
            ResetTempDirectory();
            TempDirectory.EnsureDirectoryDeleted();
            var missingPath = TempDirectory.AbsolutePath;
            var settings = new BumpLanguageSettings { RepositoryPath = missingPath };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The directory {missingPath} does not exist."));
        }

        [Fact]
        public void With_Whitespace_Path_Returns_Success()
        {
            var settings = new BumpLanguageSettings { RepositoryPath = "  " };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Invalid_Repository_Path_Returns_Error()
        {
            var invalidPath = "bad\0path";
            var settings = new BumpLanguageSettings { RepositoryPath = invalidPath };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The directory {invalidPath} is not a valid path."));
        }

        [Fact]
        public void With_Existing_Excluded_Directory_Returns_Success()
        {
            ResetTempDirectory();
            var excludePath = ConfigPath("excluded");
            Directory.CreateDirectory(excludePath);
            var settings = new BumpLanguageSettings { Exclude = [excludePath] };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }

        [Fact]
        public void With_Missing_Excluded_Directory_Returns_Error()
        {
            ResetTempDirectory();
            var missingPath = ConfigPath("missing");
            var settings = new BumpLanguageSettings { Exclude = [missingPath] };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The directory {missingPath} does not exist."));
        }

        [Fact]
        public void With_Invalid_Excluded_Path_Returns_Error()
        {
            var invalidPath = "bad\0path";
            var settings = new BumpLanguageSettings { Exclude = [invalidPath] };

            var result = settings.Validate();

            result.ShouldSatisfyAllConditions(
                () => result.Successful.ShouldBeFalse(),
                () => result.Message.ShouldBe($"The directory {invalidPath} is not a valid path."));
        }

        [Fact]
        public void With_Whitespace_Excluded_Path_Returns_Success()
        {
            var settings = new BumpLanguageSettings { Exclude = ["  "] };

            var result = settings.Validate();

            result.Successful.ShouldBeTrue();
        }
    }

    private static LocalDirectory TempDirectory => new("./temp/settings-language");

    private static string ConfigPath(string fileName) => Path.Combine(TempDirectory.AbsolutePath, fileName);

    private static void ResetTempDirectory()
    {
        TempDirectory.EnsureDirectoryDeleted();
        TempDirectory.EnsureDirectoryCreated();
    }
}
