// Copyright © Roby Van Damme.

using System.Xml;
using DotBump.Common;
using DotBump.NuGet;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.NuGet;

public class NuGetConfigFileServiceTests
{
    public class GetNuGetConfiguration
    {
        private static readonly string s_defaultNugetConfig = "nuget.config";

        [Fact]
        public void With_No_Config_File_Returns_Default_Configuration()
        {
            var directory = new LocalDirectory(Environment.CurrentDirectory);
            directory.EnsureFileDeleted("nuget.config");
            var service = new NuGetConfigFileService(new Mock<ILogger>().Object);
            var result = service.GetNuGetConfiguration(s_defaultNugetConfig);

            result.ShouldSatisfyAllConditions(
                () => result.Credentials.ShouldBeEmpty(),
                () => result.PackageSources.ShouldHaveSingleItem(),
                () => result.PackageSources.First().Key.ShouldBe("nuget.org"),
                () => result.PackageSources.First().Value.ShouldBe("https://api.nuget.org/v3/index.json"),
                () => result.PackageSources.First().ProtocolVersion.ShouldBe("3"));
        }

        [Fact]
        public void With_Package_Sources_Only_Returns_Package_Sources_And_No_Credentials()
        {
            // Arrange
            var xmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <configuration>
                    <packageSources>
                        <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" protocolVersion=""3"" />
                        <add key=""myorg"" value=""https://myorg.pkgs.visualstudio.com/_packaging/myorg/nuget/v3/index.json"" protocolVersion=""3"" />
                    </packageSources>
                </configuration>";

            var tempFile = CreateTempConfigFile(xmlContent);
            var service = new NuGetConfigFileService(new Mock<ILogger>().Object);

            try
            {
                // Act
                var result = service.GetNuGetConfiguration(s_defaultNugetConfig);

                // Assert
                result.ShouldSatisfyAllConditions(
                    () => result.ShouldNotBeNull(),
                    () => result.PackageSources.Count.ShouldBe(2),
                    () => result.PackageSources.First().Key.ShouldBe("nuget.org"),
                    () => result.PackageSources.First().Value.ShouldBe("https://api.nuget.org/v3/index.json"),
                    () => result.PackageSources.First().ProtocolVersion.ShouldBe("3"),
                    () => result.PackageSources.Last().Key.ShouldBe("myorg"),
                    () => result.PackageSources.Last().Value.ShouldBe(
                        "https://myorg.pkgs.visualstudio.com/_packaging/myorg/nuget/v3/index.json"),
                    () => result.PackageSources.Last().ProtocolVersion.ShouldBe("3"),
                    () => result.Credentials.ShouldBeEmpty());
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void With_Credentials_Only_Throws_DotBumpException()
        {
            var xmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <configuration>
                    <packageSourceCredentials>
                        <myorg>
                            <add key=""Username"" value=""myuser"" />
                            <add key=""ClearTextPassword"" value=""mypassword"" />
                        </myorg>
                        <anothersource>
                            <add key=""Username"" value=""anotheruser"" />
                        </anothersource>
                    </packageSourceCredentials>
                </configuration>";

            var tempFile = CreateTempConfigFile(xmlContent);
            var service = new NuGetConfigFileService(new Mock<ILogger>().Object);

            try
            {
                Should.Throw<DotBumpException>(() => service.GetNuGetConfiguration(s_defaultNugetConfig));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void With_Both_Sources_And_Credentials_Present_Returns_Both()
        {
            // Arrange
            var xmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <configuration>
                    <packageSources>
                        <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" protocolVersion=""3"" />
                        <add key=""myorg"" value=""https://somedomain.com/myorg/nuget/v3/index.json"" protocolVersion=""3"" />
                    </packageSources>
                    <packageSourceCredentials>
                        <myorg>
                            <add key=""Username"" value=""myuser"" />
                            <add key=""ClearTextPassword"" value=""mypassword"" />
                        </myorg>
                    </packageSourceCredentials>
                </configuration>";

            var tempFile = CreateTempConfigFile(xmlContent);
            var service = new NuGetConfigFileService(new Mock<ILogger>().Object);

            try
            {
                // Act
                var result = service.GetNuGetConfiguration(s_defaultNugetConfig);

                // Assert
                result.ShouldSatisfyAllConditions(
                    () => result.ShouldNotBeNull(),
                    () => result.PackageSources.Count.ShouldBe(2),
                    () => result.Credentials.Count.ShouldBe(1),
                    () => result.PackageSources.First().Key.ShouldBe("nuget.org"),
                    () => result.PackageSources.ShouldContain(o => o.Key.Equals("myorg")),
                    () => result.Credentials.ShouldContainKey("myorg"),
                    () => result.Credentials.Values.ShouldContain(o => o.SourceName.Equals("myorg")),
                    () => result.Credentials.Values.First().Credentials.Count.ShouldBe(2),
                    () => result.Credentials.Values.First().Credentials
                        .FirstOrDefault(o => o.Key.Equals("username", StringComparison.OrdinalIgnoreCase))
                        .ShouldNotBeNull(),
                    () => result.Credentials.Values.First().Credentials
                        .FirstOrDefault(o => o.Key.Equals("username", StringComparison.OrdinalIgnoreCase))!.Value
                        .ShouldBe("myuser"),
                    () => result.Credentials.Values.First().Credentials
                        .FirstOrDefault(o => o.Key.Equals("ClearTextPassword", StringComparison.OrdinalIgnoreCase))
                        .ShouldNotBeNull(),
                    () => result.Credentials.Values.First().Credentials
                        .FirstOrDefault(o => o.Key.Equals("ClearTextPassword", StringComparison.OrdinalIgnoreCase))!
                        .Value
                        .ShouldBe("mypassword"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void With_No_Sources_Or_Credentials_Throws_DotBumpException()
        {
            // Arrange
            var xmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <configuration>
                </configuration>";

            var tempFile = CreateTempConfigFile(xmlContent);
            var service = new NuGetConfigFileService(new Mock<ILogger>().Object);

            try
            {
                Should.Throw<DotBumpException>(() => service.GetNuGetConfiguration(s_defaultNugetConfig));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void With_Invalid_Xml_Content_Throws_XmlException()
        {
            // Arrange
            var xmlContent = "Just some text";

            var tempFile = CreateTempConfigFile(xmlContent);
            var service = new NuGetConfigFileService(new Mock<ILogger>().Object);

            try
            {
                // Act
                Should.Throw<XmlException>(() => service.GetNuGetConfiguration(s_defaultNugetConfig));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        private static string CreateTempConfigFile(string content)
        {
            var localDirectory = new LocalDirectory(Environment.CurrentDirectory);
            var filename = s_defaultNugetConfig;
            localDirectory.EnsureFileDeleted(filename);
            localDirectory.EnsureFileCreated(filename, content);
            return "nuget.config";
        }
    }
}
