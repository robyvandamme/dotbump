// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpTools.DataModel.Report;

internal class BumpResult(string id, string oldVersion)
{
    public string Id { get; } = id;

    public string OldVersion { get; } = oldVersion;

    public string? NewVersion { get; set; }

    public override string ToString()
    {
        return Id + ": " + OldVersion + " > " + NewVersion;
    }

    [JsonIgnore]
    public bool WasBumped
    {
        get
        {
            return NewVersion != null && !NewVersion.Equals(OldVersion, StringComparison.OrdinalIgnoreCase);
        }
    }
}
