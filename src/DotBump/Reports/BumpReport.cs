// Copyright © Roby Van Damme.

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotBump.Commands;
using DotBump.Commands.BumpPackages.DataModel;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Common;

namespace DotBump.Reports;

internal class BumpReport
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

        // Keeps readable characters such as apostrophes (') as-is instead of the default
        // JavaScriptEncoder.Default escaping them to \u0027. The report is consumed as JSON,
        // not injected into HTML, so relaxed escaping is safe here.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private static readonly UTF8Encoding s_utf8NoBom = new();

    private readonly List<BumpResult> _results = new();
    private readonly List<string> _errors = new();
    private readonly List<string> _warnings = new();

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

    public BumpReport(PackageManifest packageManifest, BumpType bumpType)
    {
        ArgumentNullException.ThrowIfNull(packageManifest);

        CommandName = "packages";
        BumpType = bumpType;

        foreach (var packageId in packageManifest.GetPackageIds())
        {
            _results.Add(new BumpResult(packageId, GetOldVersion(packageManifest, packageId)));
        }
    }

    public string CommandName { get; init; }

    public BumpType BumpType { get; init; }

    public DateTime TimeStamp { get; set; }

    public IReadOnlyCollection<BumpResult> Results => _results;

    public IReadOnlyCollection<string> Errors => _errors;

    public IReadOnlyCollection<string> Warnings => _warnings;

    [JsonIgnore]
    public bool HasChanges
    {
        get
        {
            return Results.Any(o =>
                o.NewVersion != null && !o.NewVersion.Equals(o.OldVersion, StringComparison.OrdinalIgnoreCase));
        }
    }

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

    public void ReportChanges(Release sdkRelease)
    {
        _results[0].NewVersion = sdkRelease.LatestSdk;
        TimeStamp = DateTime.UtcNow;
    }

    public void ReportChanges(PackageManifest packageManifest)
    {
        ArgumentNullException.ThrowIfNull(packageManifest);

        foreach (var packageId in packageManifest.GetPackageIds())
        {
            var reportItem = _results.FirstOrDefault(o => o.Id.Equals(
                packageId,
                StringComparison.OrdinalIgnoreCase));
            if (reportItem != null)
            {
                reportItem.NewVersion = GetNewVersion(packageManifest, packageId);
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

    public void ReportWarnings(IReadOnlyCollection<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(warnings);

        _warnings.AddRange(warnings);
        TimeStamp = DateTime.UtcNow;
    }

    public void ReportNoSdkVersionChanges()
    {
        _results[0].NewVersion = _results[0].OldVersion;
        TimeStamp = DateTime.UtcNow;
    }

    public async Task WriteToFileAsync(string? outputFile)
    {
        if (string.IsNullOrWhiteSpace(outputFile))
        {
            return;
        }

        // System.Text.Json indents with Environment.NewLine; normalize to LF so the JSON report is
        // byte-identical on every platform.
        var content = ReportFormatResolver.Resolve(outputFile) == ReportFormat.Markdown
            ? MarkdownReportFormatter.Format(this)
            : JsonSerializer.Serialize(this, s_jsonOptions).Replace(Environment.NewLine, "\n", StringComparison.Ordinal);

        if (content is null)
        {
            // Nothing to report: remove any report left over from a previous run so a reused
            // output path never yields stale content.
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }

            return;
        }

        var outputDirectory = Path.GetDirectoryName(outputFile);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        await File.WriteAllTextAsync(outputFile, content, s_utf8NoBom);
    }

    /// <summary>
    /// Gets the lowest original version across all occurrences of a package, which represents the
    /// occurrence that is furthest behind. Using the lowest occurrence means that both a real upgrade
    /// and aligning stale occurrences to a higher existing version are reported as a change.
    /// Falls back to the first occurrence when none is a valid semantic version.
    /// </summary>
    private static string GetOldVersion(PackageManifest packageManifest, string packageId)
    {
        return GetVersion(packageManifest, packageId, useOriginalVersion: true, selectLowest: true);
    }

    /// <summary>
    /// Gets the highest current version across all occurrences of a package, which is the target all
    /// occurrences converge to. Falls back to the first occurrence when none is a valid semantic
    /// version.
    /// </summary>
    private static string GetNewVersion(PackageManifest packageManifest, string packageId)
    {
        return GetVersion(packageManifest, packageId, useOriginalVersion: false, selectLowest: false);
    }

    private static string GetVersion(
        PackageManifest packageManifest,
        string packageId,
        bool useOriginalVersion,
        bool selectLowest)
    {
        var entries = packageManifest.Packages
            .Where(package => string.Equals(package.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var versions = entries
            .Select(entry => new SemanticVersion(useOriginalVersion ? entry.OriginalVersion : entry.Version))
            .Where(version => version.IsValid)
            .ToList();

        if (versions.Count == 0)
        {
            return useOriginalVersion ? entries[0].OriginalVersion : entries[0].Version;
        }

        return selectLowest ? versions.Min()!.Version : versions.Max()!.Version;
    }
}
