// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;
using Serilog;

namespace DotBump.Commands.BumpTools.DataModel.NuGetClientConfiguration;

internal record NuGetClientConfig
{
    private readonly ILogger _logger;

    public NuGetClientConfig(string packageSourceKey, NuGetConfig nuGetConfig, ILogger logger)
    {
        _logger = logger;
        Url = GetUrlFromConfig(packageSourceKey, nuGetConfig);
        Credential = GetCredentialFromConfig(packageSourceKey, nuGetConfig);
    }

    public string Url { get; init; }

    public NuGetClientCredential? Credential { get; init; }

    public bool IsPrivate
    {
        get { return Credential != null; }
    }

    /// <summary>
    /// Gets the URL string from the config for the specified packageSourceName.
    /// </summary>
    private string GetUrlFromConfig(string packageSourceName, NuGetConfig nuGetConfig)
    {
        var packageSource = nuGetConfig.PackageSources.FirstOrDefault(o => o.Key.Equals(packageSourceName));
        if (packageSource == null)
        {
            throw new ArgumentOutOfRangeException(nameof(packageSourceName));
        }

        if (string.IsNullOrWhiteSpace(packageSource.Value))
        {
            throw new ArgumentException(nameof(NuGetConfig));
        }

        if (packageSource.ProtocolVersion != "3")
        {
            throw new ArgumentException(
                $"Only protocol version 3 is supported. {packageSourceName} has protocol version {packageSource.ProtocolVersion}");
        }

        return packageSource.Value;
    }

    /// <summary>
    /// Gets the UserName and ClearTextPassword values from the config and replaces them with the matching environment variables.
    /// Expects the values to have a % prefix and suffix which will be stripped before trying to get the environment variable.
    /// Example: `%GITHUB_PACKAGES_USER%`.
    /// </summary>
    private NuGetClientCredential? GetCredentialFromConfig(string packageSourceKey, NuGetConfig nuGetConfig)
    {
        nuGetConfig.Credentials.TryGetValue(packageSourceKey, out var sourceCredential);

        if (sourceCredential != null)
        {
            var userNamePlaceHolder = sourceCredential.Credentials.FirstOrDefault(o => o.Key.Equals(
                "UserName",
                StringComparison.OrdinalIgnoreCase));
            var passwordPlaceHolder = sourceCredential.Credentials.FirstOrDefault(o => o.Key.Equals(
                "ClearTextPassword",
                StringComparison.OrdinalIgnoreCase));
            if (userNamePlaceHolder != null && passwordPlaceHolder != null)
            {
                if (!HasPercentBoundaries(userNamePlaceHolder.Value))
                {
                    // NOTE: Decided not to throw here. Configuration is validated in the NuGetConfigValidator so the chance of
                    // this happening should be low. Revisit if this would pop up.
                    _logger.Warning(
                        "UserName value for {Source} should have percent boundaries",
                        sourceCredential.SourceName);
                    return null;
                }

                if (!HasPercentBoundaries(passwordPlaceHolder.Value))
                {
                    _logger.Warning(
                        "ClearTextPassword value for {Source} should have percent boundaries",
                        sourceCredential.SourceName);
                    return null;
                }

                var userNameVariable = userNamePlaceHolder.Value.Trim('%');
                var passwordVariable = passwordPlaceHolder.Value.Trim('%');

                var userName = Environment.GetEnvironmentVariable(userNameVariable);
                var password = Environment.GetEnvironmentVariable(passwordVariable);
                if (string.IsNullOrWhiteSpace(userName))
                {
                    _logger.Warning(
                        "Environment variable {UserName} not found for {PackageSource}",
                        userNameVariable,
                        packageSourceKey);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    _logger.Warning(
                        "Environment variable {Password} not found for {PackageSource}",
                        passwordVariable,
                        packageSourceKey);
                    return null;
                }

                return new NuGetClientCredential(userName, password);
            }
        }

        return null;
    }

    private bool HasPercentBoundaries(string input)
    {
        return !string.IsNullOrEmpty(input) && input.StartsWith('%') && input.EndsWith('%');
    }
}
