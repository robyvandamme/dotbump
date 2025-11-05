// Copyright © 2025 Roby Van Damme.

using Serilog;
using Serilog.Events;

namespace DotBump.Common;

internal static class LoggerExtensions
{
    public static void MethodStart(this ILogger logger, string className, string methodName)
    {
        if (logger.IsEnabled(LogEventLevel.Debug))
        {
            logger.Debug("{ClassName} {MethodName} started", className, methodName);
        }
    }

    public static void MethodStart<TParameter>(
        this ILogger logger,
        string className,
        string methodName,
        TParameter parameter)
    {
        if (logger.IsEnabled(LogEventLevel.Debug))
        {
            logger.Debug(
                "{ClassName} {MethodName} started with parameter {@Parameter}",
                className,
                methodName,
                parameter);
        }
    }

    public static void MethodStart<TParameter1, TParameter2>(
        this ILogger logger,
        string className,
        string methodName,
        TParameter1 parameter1,
        TParameter2 parameter2)
    {
        if (logger.IsEnabled(LogEventLevel.Debug))
        {
            logger.Debug(
                "{ClassName} {MethodName} started with parameters {@Parameter1}, {@Parameter2}",
                className,
                methodName,
                parameter1,
                parameter2);
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
