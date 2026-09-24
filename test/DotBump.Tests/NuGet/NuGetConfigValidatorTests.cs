// Copyright © Roby Van Damme.

using DotBump.NuGet;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.NuGet;

public class NuGetConfigValidatorTests
{
    public class Validate
    {
        private readonly ILogger _loggerMock = new Mock<ILogger>().Object;

        [Fact]
        public void With_Valid_Config_Returns_Empty_List()
        {
            // Arrange
            var config = new NuGetConfig
            {
                PackageSources = new List<PackageSource>
                {
                    new()
                    {
                        Key = "nuget.org",
                        Value = "https://api.nuget.org/v3/index.json",
                        ProtocolVersion = "3",
                    },
                },
                Credentials = new Dictionary<string, SourceCredential>(),
            };

            var validator = new NuGetConfigValidator(_loggerMock);

            // Act
            var result = validator.Validate(config);

            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public void With_Invalid_Protocol_Version_Returns_Validation_Error()
        {
            // Arrange
            var config = new NuGetConfig
            {
                PackageSources = new List<PackageSource>
                {
                    new()
                    {
                        Key = "nuget.org",
                        Value = "https://api.nuget.org/v3/index.json",
                        ProtocolVersion = "2",
                    },
                },
                Credentials = new Dictionary<string, SourceCredential>(),
            };

            var validator = new NuGetConfigValidator(_loggerMock);

            // Act
            var result = validator.Validate(config);

            // Assert
            result.ShouldSatisfyAllConditions(
                () => result.Count.ShouldBe(1),
                () => result[0].ErrorMessage!.ShouldContain("invalid protocol version"),
                () => result[0].MemberNames.ShouldContain(nameof(PackageSource.ProtocolVersion)));
        }

        [Fact]
        public void With_Invalid_Url_Returns_Validation_Error()
        {
            // Arrange
            var config = new NuGetConfig
            {
                PackageSources = new List<PackageSource>
                {
                    new() { Key = "nuget.org", Value = "not-a-valid-url", ProtocolVersion = "3" },
                },
                Credentials = new Dictionary<string, SourceCredential>(),
            };

            var validator = new NuGetConfigValidator(_loggerMock);

            // Act
            var result = validator.Validate(config);

            // Assert
            result.ShouldSatisfyAllConditions(
                () => result.Count.ShouldBe(1),
                () => result[0].ErrorMessage!.ShouldContain("invalid URL"),
                () => result[0].MemberNames.ShouldContain(nameof(PackageSource.Value)));
        }

        [Fact]
        public void With_Multiple_Source_Errors_Returns_Multiple_Validation_Errors()
        {
            // Arrange
            var config = new NuGetConfig
            {
                PackageSources = new List<PackageSource>
                {
                    new() { Key = "nuget.org", Value = "not-a-valid-url", ProtocolVersion = "2" },
                },
                Credentials = new Dictionary<string, SourceCredential>(),
            };

            var validator = new NuGetConfigValidator(_loggerMock);

            // Act
            var result = validator.Validate(config);

            // Assert
            result.ShouldSatisfyAllConditions(
                () => result.Count.ShouldBe(2),
                () => result.Any(r => r.MemberNames.Contains(nameof(PackageSource.ProtocolVersion))).ShouldBeTrue(),
                () => result.Any(r => r.MemberNames.Contains(nameof(PackageSource.Value))).ShouldBeTrue());
        }

        [Fact]
        public void With_Invalid_Credential_Values_Returns_Validation_Errors()
        {
            var configCredentials = new Dictionary<string, SourceCredential>();
            var userName = new Credential() { Key = "UserName", Value = "Some Value" };
            var password = new Credential() { Key = "ClearTextPassword", Value = "Some other value" };
            var sourceCredential = new SourceCredential() { SourceName = "nuget.org", };
            sourceCredential.Credentials.Add(userName);
            sourceCredential.Credentials.Add(password);
            configCredentials.Add("nuget.org", sourceCredential);

            // Arrange
            var config = new NuGetConfig
            {
                PackageSources =
                    new List<PackageSource>
                    {
                        new()
                        {
                            Key = "nuget.org",
                            Value = "https://api.nuget.org/v3/index.json",
                            ProtocolVersion = "3",
                        },
                    },
                Credentials = configCredentials,
            };
            var validator = new NuGetConfigValidator(_loggerMock);

            // Act
            var result = validator.Validate(config);

            // Assert
            result.ShouldSatisfyAllConditions(
                () => result.Count.ShouldBe(2),
                () => result[0].ErrorMessage!.ShouldContain(
                    "Credential value for Username for source nuget.org must start and end with a % character"),
                () => result[1].ErrorMessage!.ShouldContain(
                    "Credential value for ClearTextPassword for source nuget.org must start and end with a % character"));
        }

        [Fact]
        public void With_Invalid_Credential_Keys_Returns_Validation_Errors()
        {
            var configCredentials = new Dictionary<string, SourceCredential>();
            var userName = new Credential() { Key = "User", Value = "%VALID%" };
            var password = new Credential() { Key = "ClearPassword", Value = "%ALSO_VALID%" };
            var sourceCredential = new SourceCredential() { SourceName = "nuget.org", };
            sourceCredential.Credentials.Add(userName);
            sourceCredential.Credentials.Add(password);
            configCredentials.Add("nuget.org", sourceCredential);

            // Arrange
            var config = new NuGetConfig
            {
                PackageSources =
                    new List<PackageSource>
                    {
                        new()
                        {
                            Key = "nuget.org",
                            Value = "https://api.nuget.org/v3/index.json",
                            ProtocolVersion = "3",
                        },
                    },
                Credentials = configCredentials,
            };
            var validator = new NuGetConfigValidator(_loggerMock);

            // Act
            var result = validator.Validate(config);

            // Assert
            result.ShouldSatisfyAllConditions(
                () => result.Count.ShouldBe(2),
                () => result[0].ErrorMessage!.ShouldContain(
                    "Credential key for source nuget.org should be UserName or ClearTextPassword"),
                () => result[1].ErrorMessage!.ShouldContain(
                    "Credential key for source nuget.org should be UserName or ClearTextPassword"));
        }

        [Fact]
        public void With_Valid_Credential_Values_Returns_Empty_List()
        {
            var configCredentials = new Dictionary<string, SourceCredential>();
            var userName = new Credential() { Key = "UserName", Value = "%OK%" };
            var password = new Credential() { Key = "ClearTextPassword", Value = "%OK%" };
            var sourceCredential = new SourceCredential() { SourceName = "nuget.org", };
            sourceCredential.Credentials.Add(userName);
            sourceCredential.Credentials.Add(password);
            configCredentials.Add("nuget.org", sourceCredential);

            // Arrange
            var config = new NuGetConfig
            {
                PackageSources =
                    new List<PackageSource>
                    {
                        new()
                        {
                            Key = "nuget.org",
                            Value = "https://api.nuget.org/v3/index.json",
                            ProtocolVersion = "3",
                        },
                    },
                Credentials = configCredentials,
            };

            var validator = new NuGetConfigValidator(_loggerMock);

            // Act
            var result = validator.Validate(config);

            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public void With_Non_Https_Url_Returns_Validation_Error()
        {
            // Arrange
            var config = new NuGetConfig
            {
                PackageSources = new List<PackageSource>
                {
                    new() { Key = "file", Value = "ftp://server/path", ProtocolVersion = "3" },
                },
                Credentials = new Dictionary<string, SourceCredential>(),
            };

            var validator = new NuGetConfigValidator(_loggerMock);

            // Act
            var result = validator.Validate(config);

            // Assert
            result.ShouldSatisfyAllConditions(
                () => result.Count.ShouldBe(1),
                () => result[0].ErrorMessage!.ShouldContain("invalid URL"));
        }

        [Fact]
        public void With_Http_Url_Returns_Validation_Error()
        {
            // Arrange
            var config = new NuGetConfig
            {
                PackageSources = new List<PackageSource>
                {
                    new() { Key = "file", Value = "http://server/path", ProtocolVersion = "3" },
                },
                Credentials = new Dictionary<string, SourceCredential>(),
            };

            var validator = new NuGetConfigValidator(_loggerMock);

            // Act
            var result = validator.Validate(config);

            // Assert
            result.ShouldSatisfyAllConditions(
                () => result.Count.ShouldBe(1),
                () => result[0].ErrorMessage!.ShouldContain("invalid URL"));
        }
    }
}
