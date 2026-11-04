// Copyright © Roby Van Damme.

using DotBump.Common;
using Moq;
using Serilog;
using Serilog.Events;
using Shouldly;

namespace DotBump.Tests.Common;

/// <summary>
/// Tests for the delegating logger. These mutate the static <see cref="Log.Logger"/>, which is only safe because
/// <c>xunit.runner.json</c> disables parallel test collections.
/// </summary>
public class DelegatingLoggerTests
{
    public class IsEnabled
    {
        [Fact]
        public void Forwards_To_Active_Logger()
        {
            var originalLogger = Log.Logger;
            var activeLogger = new Mock<ILogger>();
            activeLogger.Setup(logger => logger.IsEnabled(LogEventLevel.Debug)).Returns(true);

            try
            {
                Log.Logger = activeLogger.Object;
                var logger = new DelegatingLogger();

                logger.IsEnabled(LogEventLevel.Debug).ShouldBeTrue();
            }
            finally
            {
                Log.Logger = originalLogger;
            }
        }
    }

    public class Write
    {
        [Fact]
        public void Forwards_Event_To_Active_Logger()
        {
            var originalLogger = Log.Logger;
            var activeLogger = new Mock<ILogger>();
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                new MessageTemplate("Test", []),
                []);

            try
            {
                Log.Logger = activeLogger.Object;
                var logger = new DelegatingLogger();

                logger.Write(logEvent);

                activeLogger.Verify(active => active.Write(logEvent), Times.Once);
            }
            finally
            {
                Log.Logger = originalLogger;
            }
        }
    }

    public class Debug
    {
        [Fact]
        public void Binds_And_Forwards_Event_To_Active_Logger()
        {
            var originalLogger = Log.Logger;
            var activeLogger = new Mock<ILogger>();
            activeLogger.Setup(logger => logger.IsEnabled(LogEventLevel.Debug)).Returns(true);

            LogEvent? writtenEvent = null;
            activeLogger
                .Setup(logger => logger.Write(It.IsAny<LogEvent>()))
                .Callback<LogEvent>(logEvent => writtenEvent = logEvent);

            try
            {
                Log.Logger = activeLogger.Object;
                ILogger logger = new DelegatingLogger();

                logger.Debug("Hello {Name}", "World");

                writtenEvent.ShouldNotBeNull();
                writtenEvent!.MessageTemplate.Text.ShouldBe("Hello {Name}");
                writtenEvent.Properties["Name"].ToString().ShouldContain("World");
            }
            finally
            {
                Log.Logger = originalLogger;
            }
        }
    }

    public class ForContext
    {
        [Fact]
        public void Debug_With_ForContext_Forwards_Event_To_Active_Logger()
        {
            var originalLogger = Log.Logger;
            var activeLogger = new Mock<ILogger>();
            activeLogger.Setup(logger => logger.IsEnabled(LogEventLevel.Debug)).Returns(true);

            LogEvent? writtenEvent = null;
            activeLogger
                .Setup(logger => logger.Write(It.IsAny<LogEvent>()))
                .Callback<LogEvent>(logEvent => writtenEvent = logEvent);

            try
            {
                Log.Logger = activeLogger.Object;
                ILogger logger = new DelegatingLogger();

                logger.ForContext<DelegatingLoggerTests>().Debug("Hello {Name}", "World");

                writtenEvent.ShouldNotBeNull();
                writtenEvent!.Properties["SourceContext"].ToString()
                    .ShouldContain(nameof(DelegatingLoggerTests));
            }
            finally
            {
                Log.Logger = originalLogger;
            }
        }
    }
}
