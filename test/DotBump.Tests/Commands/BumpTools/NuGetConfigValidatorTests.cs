// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools;
using DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpTools;

public class NuGetConfigValidatorTests
{
    public class Validate
    {
        private readonly ILogger _loggerMock = new Mock<ILogger>().Object;

        [Fact]
        public void Should_Return_Empty_List_When_Config_Is_Valid()
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
        public void Should_Validate_Invalid_Protocol_Version()
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
            result.Count.ShouldBe(1);
            result[0].ErrorMessage!.ShouldContain("invalid protocol version");
            result[0].MemberNames.ShouldContain(nameof(PackageSource.ProtocolVersion));
        }

        [Fact]
        public void Should_Validate_Invalid_URL()
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
            result.Count.ShouldBe(1);
            result[0].ErrorMessage!.ShouldContain("invalid URL");
            result[0].MemberNames.ShouldContain(nameof(PackageSource.Value));
        }

        [Fact]
        public void Should_Report_Multiple_Validation_Errors_For_Package_Source()
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
            result.Count.ShouldBe(2);
            result.Any(r => r.MemberNames.Contains(nameof(PackageSource.ProtocolVersion))).ShouldBeTrue();
            result.Any(r => r.MemberNames.Contains(nameof(PackageSource.Value))).ShouldBeTrue();
        }

        [Fact]
        public void Should_Validate_Credential_Values_Start_And_End_With_Percent()
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
            result.Count.ShouldBe(2);
            result[0].ErrorMessage!.ShouldContain(
                "Credential value for Username for source nuget.org must start and end with a % character");
            result[1].ErrorMessage!.ShouldContain(
                "Credential value for ClearTextPassword for source nuget.org must start and end with a % character");
        }

        [Fact]
        public void Should_Validate_Credential_Keys()
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
            result.Count.ShouldBe(2);
            result[0].ErrorMessage!.ShouldContain(
                "Credential key for source nuget.org should be UserName or ClearTextPassword");
            result[1].ErrorMessage!.ShouldContain(
                "Credential key for source nuget.org should be UserName or ClearTextPassword");
        }

        [Fact]
        public void Should_Accept_Valid_Credential_Values()
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
        public void Should_Not_Accept_Non_HTTPS_URLs()
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
            result.Count.ShouldBe(1);
            result[0].ErrorMessage!.ShouldContain("invalid URL");
        }

        [Fact]
        public void Should_Not_Accept_HTTP_URLs()
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
            result.Count.ShouldBe(1);
            result[0].ErrorMessage!.ShouldContain("invalid URL");
        }
    }
}
