// Copyright © Roby Van Damme.

using DotBump.NuGet.DataModel.NuGetClientConfiguration;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.Tests.TestHelpers;
using Serilog;
using Serilog.Events;
using Shouldly;

namespace DotBump.Tests.NuGet;

public class NuGetClientConfigTests
{
    private const string EnvUserName = "DOTBUMP_TEST_NUGET_USER";
    private const string EnvPassword = "DOTBUMP_TEST_NUGET_PASSWORD";
    private const string EnvNoBoundaryUserName = "DOTBUMP_TEST_NOBOUND_USER";
    private const string EnvNoBoundaryPassword = "DOTBUMP_TEST_NOBOUND_PASSWORD";
    private const string EnvMissingUserName = "DOTBUMP_TEST_MISSING_USER";
    private const string EnvMissingPassword = "DOTBUMP_TEST_MISSING_PASSWORD";

    public class Constructor
    {
        [Fact]
        public void With_Valid_Source_Resolves_Url()
        {
            var (logger, _) = CreateLogger();
            var config = CreateConfig("feed", "https://feeds.example.com/index.json");

            var clientConfig = new NuGetClientConfig("feed", config, logger);

            clientConfig.Url.ShouldBe("https://feeds.example.com/index.json");
        }

        [Fact]
        public void With_Unknown_Source_Key_Throws_ArgumentOutOfRangeException()
        {
            var (logger, _) = CreateLogger();
            var config = CreateConfig("feed", "https://feeds.example.com/index.json");

            Should.Throw<ArgumentOutOfRangeException>(() => new NuGetClientConfig("missing", config, logger));
        }

        [Fact]
        public void With_Source_Key_Differing_Only_By_Case_Throws_ArgumentOutOfRangeException()
        {
            var (logger, _) = CreateLogger();
            var config = CreateConfig("Feed", "https://feeds.example.com/index.json");

            Should.Throw<ArgumentOutOfRangeException>(() => new NuGetClientConfig("feed", config, logger));
        }

        [Fact]
        public void With_Blank_Source_Value_Throws_ArgumentException()
        {
            var (logger, _) = CreateLogger();
            var config = CreateConfig("feed", " ");

            var exception = Should.Throw<ArgumentException>(() => new NuGetClientConfig("feed", config, logger));

            exception.ShouldSatisfyAllConditions(
                () => exception.Message.ShouldBe("NuGetConfig"),
                () => exception.ParamName.ShouldBeNull());
        }

        [Fact]
        public void With_Non_V3_Protocol_Throws_ArgumentException()
        {
            var (logger, _) = CreateLogger();
            var config = CreateConfig("feed", "https://feeds.example.com/index.json", protocolVersion: "2");

            var exception = Should.Throw<ArgumentException>(() => new NuGetClientConfig("feed", config, logger));

            exception.Message.ShouldBe("Only protocol version 3 is supported. feed has protocol version 2");
        }

        [Fact]
        public void With_No_Credential_Is_Not_Private()
        {
            var (logger, _) = CreateLogger();
            var config = CreateConfig("feed", "https://feeds.example.com/index.json");

            var clientConfig = new NuGetClientConfig("feed", config, logger);

            clientConfig.ShouldSatisfyAllConditions(
                () => clientConfig.Credential.ShouldBeNull(),
                () => clientConfig.IsPrivate.ShouldBeFalse());
        }

        [Fact]
        public void With_Environment_Variable_Credentials_Resolves_From_Environment()
        {
            var (logger, _) = CreateLogger();
            var config = CreateConfig("feed", "https://feeds.example.com/index.json");
            config.Credentials["feed"] = CreateSourceCredential(
                "feed",
                ("Username", $"%{EnvUserName}%"),
                ("ClearTextPassword", $"%{EnvPassword}%"));

            Environment.SetEnvironmentVariable(EnvUserName, "alice");
            Environment.SetEnvironmentVariable(EnvPassword, "secret");
            try
            {
                var clientConfig = new NuGetClientConfig("feed", config, logger);

                clientConfig.Credential.ShouldNotBeNull();
                clientConfig.Credential!.ShouldSatisfyAllConditions(
                    () => clientConfig.Credential.UserName.ShouldBe("alice"),
                    () => clientConfig.Credential.Password.ShouldBe("secret"));
                clientConfig.IsPrivate.ShouldBeTrue();
            }
            finally
            {
                Environment.SetEnvironmentVariable(EnvUserName, null);
                Environment.SetEnvironmentVariable(EnvPassword, null);
            }
        }

        [Fact]
        public void With_Missing_Environment_Variables_Falls_Back_To_Literal_And_Logs()
        {
            var (logger, sink) = CreateLogger();
            var config = CreateConfig("feed", "https://feeds.example.com/index.json");
            config.Credentials["feed"] = CreateSourceCredential(
                "feed",
                ("Username", $"%{EnvMissingUserName}%"),
                ("ClearTextPassword", $"%{EnvMissingPassword}%"));

            Environment.SetEnvironmentVariable(EnvMissingUserName, null);
            Environment.SetEnvironmentVariable(EnvMissingPassword, null);

            var clientConfig = new NuGetClientConfig("feed", config, logger);

            clientConfig.Credential.ShouldNotBeNull();
            clientConfig.Credential!.ShouldSatisfyAllConditions(
                () => clientConfig.Credential.UserName.ShouldBe($"%{EnvMissingUserName}%"),
                () => clientConfig.Credential.Password.ShouldBe($"%{EnvMissingPassword}%"));
            sink.Events.ShouldSatisfyAllConditions(
                () => sink.Events.ShouldContain(logEvent =>
                    logEvent.Level == LogEventLevel.Error
                    && logEvent.MessageTemplate.Text == "Environment variable for UserName not found for {PackageSource}"),
                () => sink.Events.ShouldContain(logEvent =>
                    logEvent.Level == LogEventLevel.Error
                    && logEvent.MessageTemplate.Text
                    == "Environment variable for ClearTextPassword not found for {PackageSource}"));
        }

        [Fact]
        public void With_Credentials_Missing_Percent_Boundaries_Logs_And_Uses_Values()
        {
            var (logger, sink) = CreateLogger();
            var config = CreateConfig("feed", "https://feeds.example.com/index.json");
            config.Credentials["feed"] = CreateSourceCredential(
                "feed",
                ("Username", EnvNoBoundaryUserName),
                ("ClearTextPassword", EnvNoBoundaryPassword));

            Environment.SetEnvironmentVariable(EnvNoBoundaryUserName, "alice");
            Environment.SetEnvironmentVariable(EnvNoBoundaryPassword, "secret");
            try
            {
                var clientConfig = new NuGetClientConfig("feed", config, logger);

                clientConfig.Credential.ShouldNotBeNull();
                clientConfig.Credential!.ShouldSatisfyAllConditions(
                    () => clientConfig.Credential.UserName.ShouldBe("alice"),
                    () => clientConfig.Credential.Password.ShouldBe("secret"));
                sink.Events.ShouldSatisfyAllConditions(
                    () => sink.Events.ShouldContain(logEvent =>
                        logEvent.Level == LogEventLevel.Error
                        && logEvent.MessageTemplate.Text == "UserName value for {Source} should have percent boundaries"),
                    () => sink.Events.ShouldContain(logEvent =>
                        logEvent.Level == LogEventLevel.Error
                        && logEvent.MessageTemplate.Text
                        == "ClearTextPassword value for {Source} should have percent boundaries"));
            }
            finally
            {
                Environment.SetEnvironmentVariable(EnvNoBoundaryUserName, null);
                Environment.SetEnvironmentVariable(EnvNoBoundaryPassword, null);
            }
        }

        [Fact]
        public void With_Source_Credential_Missing_Password_Returns_No_Credential()
        {
            var (logger, _) = CreateLogger();
            var config = CreateConfig("feed", "https://feeds.example.com/index.json");
            config.Credentials["feed"] = CreateSourceCredential(
                "feed",
                ("Username", "%SOME_USER%"));

            var clientConfig = new NuGetClientConfig("feed", config, logger);

            clientConfig.ShouldSatisfyAllConditions(
                () => clientConfig.Credential.ShouldBeNull(),
                () => clientConfig.IsPrivate.ShouldBeFalse());
        }

        [Fact]
        public void With_Source_Credential_Missing_Username_Returns_No_Credential()
        {
            var (logger, _) = CreateLogger();
            var config = CreateConfig("feed", "https://feeds.example.com/index.json");
            config.Credentials["feed"] = CreateSourceCredential(
                "feed",
                ("ClearTextPassword", "%SOME_PASSWORD%"));

            var clientConfig = new NuGetClientConfig("feed", config, logger);

            clientConfig.ShouldSatisfyAllConditions(
                () => clientConfig.Credential.ShouldBeNull(),
                () => clientConfig.IsPrivate.ShouldBeFalse());
        }

        private static NuGetConfig CreateConfig(string key, string value, string protocolVersion = "3")
        {
            return new NuGetConfig
            {
                PackageSources =
                [
                    new PackageSource { Key = key, Value = value, ProtocolVersion = protocolVersion },
                ],
            };
        }

        private static SourceCredential CreateSourceCredential(
            string sourceName,
            params (string Key, string Value)[] credentials)
        {
            var sourceCredential = new SourceCredential { SourceName = sourceName };
            foreach (var (key, value) in credentials)
            {
                sourceCredential.Credentials.Add(new Credential { Key = key, Value = value });
            }

            return sourceCredential;
        }
    }

    private static (ILogger Logger, TestLogSink Sink) CreateLogger()
    {
        var sink = new TestLogSink();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();

        return (logger, sink);
    }
}
