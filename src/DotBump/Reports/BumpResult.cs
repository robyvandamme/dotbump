// Copyright © Roby Van Damme.

using System.Text.Json.Serialization;

namespace DotBump.Reports;

internal class BumpResult(string id, string oldVersion)
{
    public string Id { get; } = id;

    public string OldVersion { get; } = oldVersion;

    public string? NewVersion { get; set; }

    /// <summary>
    /// Gets the effective MSBuild condition for a conditional package occurrence. <see langword="null"/>
    /// for unconditional packages and for commands other than <c>packages</c>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Condition { get; init; }

    public override string ToString()
    {
        var condition = Condition == null ? string.Empty : $" (when {Condition})";

        return Id + condition + ": " + OldVersion + " > " + NewVersion;
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
