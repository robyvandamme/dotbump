// Copyright © Roby Van Damme.

using DotBump.Common;
using DotBump.NuGet.DataModel.NuGetClientConfiguration;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.Tests.TestHelpers;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shouldly;

namespace DotBump.Tests.Common;

public class LoggerConfiguratorTests
{
    public class CreateConfiguration
    {
        [Fact]
        public void With_LogMasked_Credential_Masks_Value()
        {
            var (logger, sink) = CreateLogger();

            logger.Debug(
                "{@Result}",
                new Credential { Key = "ClearTextPassword", Value = "s3cr3t-password" });

            var rendered = Render(sink);
            rendered.ShouldSatisfyAllConditions(
                () => rendered.ShouldNotContain("s3cr3t-password"),
                () => rendered.ShouldContain("***"));
        }

        [Fact]
        public void With_LogMasked_NuGetClientCredential_Masks_Values()
        {
            var (logger, sink) = CreateLogger();

            logger.Debug("{@Result}", new NuGetClientCredential("s3cr3t-user", "s3cr3t-password"));

            var rendered = Render(sink);
            rendered.ShouldSatisfyAllConditions(
                () => rendered.ShouldNotContain("s3cr3t-user"),
                () => rendered.ShouldNotContain("s3cr3t-password"),
                () => rendered.ShouldContain("***"));
        }

        private static (ILogger Logger, TestLogSink Sink) CreateLogger()
        {
            var sink = new TestLogSink();
            var logger = LoggerConfigurator.CreateConfiguration(new LoggingLevelSwitch(LogEventLevel.Verbose))
                .WriteTo.Sink(sink)
                .CreateLogger();

            return (logger, sink);
        }

        private static string Render(TestLogSink sink)
        {
            return string.Join("\n", sink.Events.Select(logEvent => logEvent.RenderMessage(null)));
        }
    }
}
