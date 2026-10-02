// Copyright © Roby Van Damme.

using DotBump.Common;
using DotBump.NuGet;
using DotBump.NuGet.DataModel.NuGetClientConfiguration;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.Tests.TestHelpers;
using Serilog.Core;
using Serilog.Events;
using Shouldly;

namespace DotBump.Tests.NuGet;

public class NuGetClientFactoryTests
{
    public class CreateNuGetClient
    {
        [Fact]
        public void With_Credential_Logs_Redacts_Credentials()
        {
            var sink = new TestLogSink();
            var logger = LoggerConfigurator.CreateConfiguration(new LoggingLevelSwitch(LogEventLevel.Verbose))
                .WriteTo.Sink(sink)
                .CreateLogger();
            var nuGetConfig = new NuGetConfig();
            nuGetConfig.PackageSources.Add(
                new PackageSource
                {
                    Key = "myorg",
                    Value = "https://myorg.pkgs.visualstudio.com/_packaging/myorg/nuget/v3/index.json",
                    ProtocolVersion = "3",
                });
            var sourceCredential = new SourceCredential { SourceName = "myorg" };
            sourceCredential.Credentials.Add(new Credential { Key = "UserName", Value = "s3cr3t-user" });
            sourceCredential.Credentials.Add(new Credential { Key = "ClearTextPassword", Value = "s3cr3t-password" });
            nuGetConfig.Credentials["myorg"] = sourceCredential;
            var config = new NuGetClientConfig("myorg", nuGetConfig, logger);
            var factory = new NuGetClientFactory(logger);

            factory.CreateNuGetClient(config);

            var rendered = string.Join("\n", sink.Events.Select(logEvent => logEvent.RenderMessage(null)));
            rendered.ShouldSatisfyAllConditions(
                () => rendered.ShouldNotContain("s3cr3t-user"),
                () => rendered.ShouldNotContain("s3cr3t-password"),
                () => rendered.ShouldContain("***"));
        }
    }
}
