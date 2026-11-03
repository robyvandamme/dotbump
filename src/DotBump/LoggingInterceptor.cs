// Copyright © Roby Van Damme.

using System.Runtime.InteropServices;
using DotBump.Commands;
using DotBump.Common;
using Serilog;
using Spectre.Console.Cli;

namespace DotBump;

/// <summary>
/// Configures logging from the settings Spectre has bound, before the command is created and executed.
/// </summary>
/// <remarks>
/// Spectre invokes <see cref="Intercept"/> after it has parsed the arguments and bound the command settings, and
/// before it creates and executes the command. That is the earliest public hook where <c>--debug</c> and
/// <c>--logfile</c> are available, and it is early enough for everything the command logs.
/// <para>
/// Logging is deliberately not configured in <c>Program</c>: Spectre has no public parse-only API, and pre-parsing
/// the arguments would duplicate its option grammar. Parse failures and <c>--help</c>/<c>--version</c> never reach an
/// interceptor, so they are not written to <c>--logfile</c> (see README).
/// </para>
/// <para>
/// See <see href="https://spectreconsole.net/cli/explanation/command-lifecycle-and-execution-flow"/> and
/// <see href="https://spectreconsole.net/cli/how-to/intercepting-command-execution"/>.
/// </para>
/// </remarks>
internal sealed class LoggingInterceptor(VersionInfo versionInfo) : ICommandInterceptor
{
    /// <inheritdoc/>
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        if (settings is not BumpSettings bumpSettings)
        {
            return;
        }

        LoggerConfigurator.Configure(bumpSettings.Debug, bumpSettings.LogFile);

        var platform = PlatformInfo.GetPlatform();

        Log.Debug(
            "DotBump {Version} running on {Runtime}, {Platform} {Architecture} (OS details: {OSDescription})",
            versionInfo.Version,
            RuntimeInformation.FrameworkDescription,
            platform,
            RuntimeInformation.ProcessArchitecture,
            RuntimeInformation.OSDescription);
        Log.Debug("Starting {Command}", context.Name);
    }
}
