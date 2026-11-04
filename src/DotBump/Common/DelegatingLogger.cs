// Copyright © Roby Van Damme.

using Serilog;
using Serilog.Events;

namespace DotBump.Common;

/// <summary>
/// An <see cref="ILogger"/> that forwards to the current <see cref="Log.Logger"/>.
/// Logging is configured from the bound command settings by the <see cref="DotBump.LoggingInterceptor"/>, which runs
/// after dependency injection has registered the logger, so the logger instance handed to commands must resolve the
/// active logger at call time.
/// </summary>
/// <remarks>
/// Spectre builds its dependency injection container before it runs interceptors. The interceptor replaces
/// <see cref="Log.Logger"/> (Serilog loggers are immutable) rather than mutating the logger registered here, so a
/// logger captured at registration time would stay pointed at the silent default. Commands and services resolve
/// <see cref="ILogger"/> after interception, but this instance is registered earlier, so it looks up the active
/// logger on every call.
/// <para>
/// Only <see cref="Write(LogEvent)"/> and <see cref="IsEnabled"/> are forwarded. The other <see cref="ILogger"/>
/// members, including the convenience <c>Write</c> overloads and <c>ForContext</c>, are Serilog 4 default interface
/// implementations that funnel into <see cref="Write(LogEvent)"/>; the small surface is intentional.
/// </para>
/// </remarks>
internal sealed class DelegatingLogger : ILogger
{
    /// <inheritdoc/>
    public bool IsEnabled(LogEventLevel level)
    {
        return Log.Logger.IsEnabled(level);
    }

    /// <inheritdoc/>
    public void Write(LogEvent logEvent)
    {
        Log.Logger.Write(logEvent);
    }
}
