// Copyright © 2025 Roby Van Damme.

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotBump.Commands;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpTools.DataModel.LocalTools;

namespace DotBump.Reports;

internal class BumpReport
{
    private readonly List<BumpResult> _results = new();
    private readonly List<string> _errors = new();

    public BumpReport(ToolsManifest toolsManifest, BumpType bumpType)
    {
        CommandName = "tools";
        BumpType = bumpType;
        foreach (var toolManifestEntry in toolsManifest.Tools)
        {
            _results.Add(new BumpResult(toolManifestEntry.Key, toolManifestEntry.Value.Version));
        }
    }

    public BumpReport(Sdk sdk, BumpType bumpType)
    {
        CommandName = "sdk";
        BumpType = bumpType;
        _results.Add(new BumpResult("sdk", sdk.Version));
    }

    public string CommandName { get; init; }

    public BumpType BumpType { get; init; }

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

    public void ReportChanges(Release sdkRelease)
    {
        _results.First().NewVersion = sdkRelease.LatestSdk;
        TimeStamp = DateTime.UtcNow;
    }

    public void ReportNoSdkVersionChanges()
    {
        _results.First().NewVersion = _results.First().OldVersion;
        TimeStamp = DateTime.UtcNow;
    }

    public async Task WriteToFileAsync(string? outputFile)
    {
        if (!string.IsNullOrWhiteSpace(outputFile))
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            };
            await File.WriteAllTextAsync(
                outputFile,
                JsonSerializer.Serialize(this, options),
                new UTF8Encoding());
        }
    }
}
