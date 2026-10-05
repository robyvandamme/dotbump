// Copyright © Roby Van Damme.

namespace DotBump.Reports;

/// <summary>
/// Resolves the <see cref="ReportFormat"/> for a report from its output file extension.
/// </summary>
internal static class ReportFormatResolver
{
    /// <summary>
    /// Resolves the report format from the output file name. Files ending in <c>.md</c> or
    /// <c>.markdown</c> (case-insensitive) are written as markdown; any other extension (including
    /// <c>.json</c> or no extension) defaults to JSON.
    /// </summary>
    /// <param name="outputPath">The report output file name.</param>
    /// <returns>The resolved report format.</returns>
    public static ReportFormat Resolve(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var extension = Path.GetExtension(outputPath);

        if (extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase))
        {
            return ReportFormat.Markdown;
        }

        return ReportFormat.Json;
    }
}
