// Copyright © Roby Van Damme.

using DotBump.Common;
using Moq;
using Serilog;
using Serilog.Events;
using Shouldly;

namespace DotBump.Tests.Common;

public class DelegatingLoggerTests
{
    [Fact]
    public void IsEnabled_Forwards_To_Active_Logger()
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

    [Fact]
    public void Write_Forwards_Event_To_Active_Logger()
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

    [Fact]
    public void Debug_Binds_And_Forwards_Event_To_Active_Logger()
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
