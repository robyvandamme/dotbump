// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpSdk.DataModel;

public record GlobalJson([property: JsonPropertyName("sdk")] Sdk Sdk);
