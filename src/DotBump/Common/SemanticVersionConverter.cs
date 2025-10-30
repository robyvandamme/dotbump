// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace DotBump.Common;

/// <summary>
/// Handles conversion of string values to <see cref="SemanticVersion"/> for NuGet feeds.
/// The reason for this converter is the fact that NuGet package indexes can contain versions that do not correspond to
/// the current Semantic Version regex pattern.
/// </summary>
/// <param name="logger">The logger instance.</param>
internal class SemanticVersionConverter(ILogger logger) : JsonConverter<SemanticVersion>
{
    /// <summary>
    /// Checks if the version string value matches the Semantic Version regex pattern.
    /// In case it does not a Semantic Version of "0.0.0" is returned and a Warning is logged.
    /// </summary>
    public override SemanticVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected string value for version");
        }

        var version = reader.GetString();

        if (string.IsNullOrEmpty(version))
        {
            logger.Warning("The version string is empty, defaulting to 0.0.0");
            return new SemanticVersion("0.0.0");
        }

        var match = version.MatchesSemanticVersionPattern();

        if (!match.Success)
        {
            logger.Warning(
                "The version {Version} does not have the expected format x.y.z[-prerelease]. Defaulting to 0.0.0",
                version);
            return new SemanticVersion("0.0.0");
        }

        return new SemanticVersion(version);
    }

    /// <summary>
    /// Not Implemented.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, SemanticVersion value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
