// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;
using DotBump.Commands.BumpTools.DataModel.LocalTools;

namespace DotBump.Commands.BumpTools.DataModel.Report;

internal class BumpReport
{
    public BumpReport(ToolsManifest toolsManifest)
    {
        foreach (var toolManifestEntry in toolsManifest.Tools)
        {
            Results.Add(new BumpResult(toolManifestEntry.Key, toolManifestEntry.Value.Version));
        }
    }

    public DateTime TimeStamp { get; set; }

    public List<BumpResult> Results { get; set; } = new();

    public void ReportChanges(ToolsManifest toolsManifest)
    {
        foreach (var toolManifestEntry in toolsManifest.Tools)
        {
            var reportItem = Results.FirstOrDefault(o => o.Id.Equals(
                toolManifestEntry.Key,
                StringComparison.OrdinalIgnoreCase));
            if (reportItem != null)
            {
                reportItem.NewVersion = toolManifestEntry.Value.Version;
            }
        }

        TimeStamp = DateTime.UtcNow;
    }

    [JsonIgnore]
    public bool HasChanges
    {
        get
        {
            return Results.Any(o =>
                o.NewVersion != null && !o.NewVersion.Equals(o.OldVersion, StringComparison.OrdinalIgnoreCase));
        }
    }
}
