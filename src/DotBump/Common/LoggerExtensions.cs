// Copyright © 2025 Roby Van Damme.

using Serilog;
using Serilog.Events;

namespace DotBump.Common;

#pragma warning disable CA1062
internal static class LoggerExtensions
{
    public static void MethodStart(this ILogger logger, string className, string methodName)
    {
        if (logger.IsEnabled(LogEventLevel.Debug))
        {
            logger.Debug("{ClassName} {MethodName} started", className, methodName);
        }
    }

    public static void MethodReturn(this ILogger logger, string className, string methodName)
    {
        if (logger.IsEnabled(LogEventLevel.Debug))
        {
            logger.Debug("{ClassName} {MethodName} returning", className, methodName);
        }
    }

    public static void MethodReturn<TResult>(this ILogger logger, string className, string methodName, TResult result)
    {
        if (logger.IsEnabled(LogEventLevel.Debug))
        {
            logger.Debug("{ClassName} {MethodName} returning {@Result}", className, methodName, result);
        }
    }

    public static void MethodReturn<TResult>(this ILogger logger, string className, string methodName, string message)
    {
        if (logger.IsEnabled(LogEventLevel.Debug))
        {
            logger.Debug("{ClassName} {MethodName} returning, {Message}}", className, methodName, message);
        }
    }
}
