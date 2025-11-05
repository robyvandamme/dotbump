// Copyright © 2025 Roby Van Damme.

using System.Xml;
using DotBump.Commands.BumpTools;
using DotBump.Common;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpTools;

public class ToolFileServiceTests
{
    public class GetNuGetConfiguration
    {
        public class NoConfigFile
        {
            [Fact]
            public void Returns_Default_Configuration()
            {
                var directory = new LocalDirectory(Environment.CurrentDirectory);
                directory.EnsureFileDeleted("nuget.config");
                var service = new ToolFileService(new Mock<ILogger>().Object);
                var result = service.GetNuGetConfiguration();
                result.Credentials.ShouldBeEmpty();
                result.PackageSources.ShouldHaveSingleItem();
                result.PackageSources.First().Key.ShouldBe("nuget.org");
                result.PackageSources.First().Value.ShouldBe("https://api.nuget.org/v3/index.json");
                result.PackageSources.First().ProtocolVersion.ShouldBe("3");
            }
        }

        public class DefaultConfigFile
        {
            public class WhenConfigFileContainsPackageSourcesOnly
            {
                [Fact]
                public void Returns_Correct_Package_Sources_And_No_Credentials()
                {
                    // Arrange
                    var xmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <configuration>
                    <packageSources>
                        <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" protocolVersion=""3"" />
                        <add key=""myorg"" value=""https://myorg.pkgs.visualstudio.com/_packaging/myorg/nuget/v3/index.json"" protocolVersion=""3"" />
                    </packageSources>
                </configuration>";

                    var tempFile = CreateTempFile(xmlContent);
                    var service = new ToolFileService(new Mock<ILogger>().Object);

                    try
                    {
                        // Act
                        var result = service.GetNuGetConfiguration();

                        // Assert
                        result.ShouldNotBeNull();
                        result.PackageSources.Count.ShouldBe(2);

                        var nugetSource = result.PackageSources.First();
                        nugetSource.Key.ShouldBe("nuget.org");
                        nugetSource.Value.ShouldBe("https://api.nuget.org/v3/index.json");
                        nugetSource.ProtocolVersion.ShouldBe("3");

                        var myorgSource = result.PackageSources.Last();
                        myorgSource.Key.ShouldBe("myorg");
                        myorgSource.Value.ShouldBe(
                            "https://myorg.pkgs.visualstudio.com/_packaging/myorg/nuget/v3/index.json");
                        myorgSource.ProtocolVersion.ShouldBe("3");

                        result.Credentials.ShouldBeEmpty();
                    }
                    finally
                    {
                        File.Delete(tempFile);
                    }
                }
            }

            public class WhenConfigFileContainsCredentialsOnly
            {
                [Fact]
                public void Throws_DotBump_Exception()
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

                    var tempFile = CreateTempFile(xmlContent);
                    var service = new ToolFileService(new Mock<ILogger>().Object);

                    try
                    {
                        Should.Throw<DotBumpException>(() => service.GetNuGetConfiguration());
                    }
                    finally
                    {
                        File.Delete(tempFile);
                    }
                }
            }

            public class WhenConfigFileContainsBothPackageSourcesAndCredentials
            {
                [Fact]
                public void Parses_Both_Package_Sources_And_Credentials_Correctly()
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

                    var tempFile = CreateTempFile(xmlContent);
                    var service = new ToolFileService(new Mock<ILogger>().Object);

                    try
                    {
                        // Act
                        var result = service.GetNuGetConfiguration();

                        // Assert
                        result.ShouldNotBeNull();
                        result.PackageSources.Count.ShouldBe(2);
                        result.Credentials.Count.ShouldBe(1);

                        result.PackageSources.First().Key.ShouldBe("nuget.org");
                        result.PackageSources.ShouldContain(o => o.Key.Equals("myorg"));

                        result.Credentials.ShouldContainKey("myorg");
                        result.Credentials.Values.ShouldContain(o => o.SourceName.Equals("myorg"));
                        result.Credentials.Values.First().Credentials.Count.ShouldBe(2);
                        result.Credentials.Values.First().Credentials
                            .FirstOrDefault(o => o.Key.Equals("username", StringComparison.OrdinalIgnoreCase))
                            .ShouldNotBeNull();
                        result.Credentials.Values.First().Credentials
                            .FirstOrDefault(o => o.Key.Equals("username", StringComparison.OrdinalIgnoreCase))!.Value
                            .ShouldBe("myuser");
                        result.Credentials.Values.First().Credentials
                            .FirstOrDefault(o => o.Key.Equals("ClearTextPassword", StringComparison.OrdinalIgnoreCase))
                            .ShouldNotBeNull();
                        result.Credentials.Values.First().Credentials
                            .FirstOrDefault(o => o.Key.Equals("ClearTextPassword", StringComparison.OrdinalIgnoreCase))!
                            .Value
                            .ShouldBe("mypassword");
                    }
                    finally
                    {
                        File.Delete(tempFile);
                    }
                }
            }

            public class WhenConfigFileHasNoPackageSourcesOrCredentials
            {
                [Fact]
                public void Throws_DotBump_Exception()
                {
                    // Arrange
                    var xmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <configuration>
                </configuration>";

                    var tempFile = CreateTempFile(xmlContent);
                    var service = new ToolFileService(new Mock<ILogger>().Object);

                    try
                    {
                        Should.Throw<DotBumpException>(() => service.GetNuGetConfiguration());
                    }
                    finally
                    {
                        File.Delete(tempFile);
                    }
                }
            }

            public class WhenNotAValidConfigFile
            {
                [Fact]
                public void Throws_XmlException()
                {
                    // Arrange
                    var xmlContent = "Just some text";

                    var tempFile = CreateTempFile(xmlContent);
                    var service = new ToolFileService(new Mock<ILogger>().Object);

                    try
                    {
                        // Act
                        Should.Throw<XmlException>(() => service.GetNuGetConfiguration());
                    }
                    finally
                    {
                        File.Delete(tempFile);
                    }
                }
            }

            private static string CreateTempFile(string content)
            {
                var localDirectory = new LocalDirectory(Environment.CurrentDirectory);
                var filename = "nuget.config";
                localDirectory.EnsureFileDeleted(filename);
                localDirectory.EnsureFileCreated(filename, content);
                return "nuget.config";
            }
        }
    }
}
