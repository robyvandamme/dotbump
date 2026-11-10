// Copyright © Roby Van Damme.

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpSdk.DataModel;

/// <summary>
/// A single release within a .NET channel, including the SDK versions it shipped per feature band,
/// the runtime version and the ASP.NET Core runtime version.
/// </summary>
internal record ChannelRelease(
    [property: JsonPropertyName("security")] bool Security,
    [property: JsonPropertyName("sdks")] Collection<ChannelSdk>? Sdks,
    [property: JsonPropertyName("release-version")] string? ReleaseVersion = null,
    [property: JsonPropertyName("sdk")] ChannelComponent? Sdk = null,
    [property: JsonPropertyName("runtime")] ChannelComponent? Runtime = null,
    [property: JsonPropertyName("aspnetcore-runtime")] ChannelComponent? AspNetCoreRuntime = null);
