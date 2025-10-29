// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Serilog;

namespace DotBump.Common;

internal class SemanticVersionConverter(ILogger logger) : JsonConverter<SemanticVersion>
{
    private static readonly Regex s_versionPattern = new(
        @"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<prerelease>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$",
        RegexOptions.Compiled);

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

        var match = s_versionPattern.Match(version);

        if (!match.Success)
        {
            logger.Warning("The version {Version} does not have the expected format x.y.z[-prerelease]", version);
            return new SemanticVersion("0.0.0");
        }

        return new SemanticVersion(version);
    }

    public override void Write(Utf8JsonWriter writer, SemanticVersion value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
