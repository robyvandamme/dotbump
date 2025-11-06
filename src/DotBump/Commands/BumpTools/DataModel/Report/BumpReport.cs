// Copyright © 2025 Roby Van Damme.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DotBump.Commands.BumpTools.DataModel.LocalTools;

namespace DotBump.Commands.BumpTools.DataModel.Report;

internal class BumpReport
{
    private readonly List<BumpResult> _results = new();
    private readonly List<string> _errors = new();

    public BumpReport(ToolsManifest toolsManifest)
    {
        foreach (var toolManifestEntry in toolsManifest.Tools)
        {
            _results.Add(new BumpResult(toolManifestEntry.Key, toolManifestEntry.Value.Version));
        }
    }

    public DateTime TimeStamp { get; set; }

    public IReadOnlyCollection<BumpResult> Results => _results;

    public IReadOnlyCollection<string> Errors => _errors;

    public void ReportChanges(ToolsManifest toolsManifest)
    {
        foreach (var toolManifestEntry in toolsManifest.Tools)
        {
            var reportItem = _results.FirstOrDefault(o => o.Id.Equals(
                toolManifestEntry.Key,
                StringComparison.OrdinalIgnoreCase));
            if (reportItem != null)
            {
                reportItem.NewVersion = toolManifestEntry.Value.Version;
            }
        }

        TimeStamp = DateTime.UtcNow;
    }

    public void ReportErrors(List<ValidationResult> validationErrors)
    {
        foreach (var error in validationErrors)
        {
            if (error.ErrorMessage != null)
            {
                _errors.Add(error.ErrorMessage);
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
