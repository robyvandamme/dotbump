// Copyright © 2025 Roby Van Damme.

using System.ComponentModel.DataAnnotations;
using DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;
using DotBump.Commands.BumpTools.Interfaces;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class NuGetConfigValidator(ILogger logger) : INuGetConfigValidator
{
    public List<ValidationResult> Validate(NuGetConfig config)
    {
        var validationResults = new List<ValidationResult>();

        // Validate PackageSources
        foreach (var source in config.PackageSources)
        {
            // Validate ProtocolVersion
            if (source.ProtocolVersion != "3")
            {
                logger.Error(
                    "Package source {PackageSource} has an invalid protocol version {Version}",
                    source.Key,
                    source.ProtocolVersion);
                validationResults.Add(
                    new ValidationResult(
                        $"Package source {source.Key} has an invalid protocol version. Expected 3 but got {source.ProtocolVersion}.",
                        [nameof(PackageSource.ProtocolVersion)]));
            }

            // Validate Value as URL
            if (!IsValidUrl(source.Value))
            {
                logger.Error(
                    "Package source {PackageSource} has an invalid URL version {URL}",
                    source.Key,
                    source.Value);
                validationResults.Add(
                    new ValidationResult(
                        $"Package source {source.Key} has invalid URL: {source.Value}.",
                        [nameof(PackageSource.Value)]));
            }
        }

        // Validate Credentials
        foreach (var (key, credential) in config.Credentials)
        {
            foreach (var cred in credential.Credentials)
            {
                // Validate that Value starts and ends with '%'
                if (!cred.Value.StartsWith("%") || !cred.Value.EndsWith("%"))
                {
                    if (credential != null)
                    {
                        logger.Error(
                            "Credential value {Key} for source {Source} should start and end with %",
                            cred.Key,
                            credential.SourceName);
                        validationResults.Add(
                            new ValidationResult(
                                $"Credential {cred.Key} for source {credential.SourceName} must start and end with a % character.",
                                [nameof(Credential.Value)]));
                    }
                }
            }
        }

        return validationResults;
    }

    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
