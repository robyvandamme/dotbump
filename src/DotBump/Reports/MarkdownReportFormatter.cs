// Copyright © Roby Van Damme.

using System.Text;

namespace DotBump.Reports;

/// <summary>
/// Renders a <see cref="BumpReport"/> as concise markdown: a command heading, the entries that
/// changed, and any warnings or errors. Used as the content for automated dependency-update pull
/// requests.
/// </summary>
internal static class MarkdownReportFormatter
{
    /// <summary>
    /// Formats the report as markdown.
    /// </summary>
    /// <param name="report">The report to format.</param>
    /// <returns>
    /// The markdown content, or <see langword="null"/> when the report has nothing to show (no
    /// changed entries, no warnings and no errors).
    /// </returns>
    public static string? Format(BumpReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(report.CommandName);

        var changedResults = report.Results
            .Where(result => result.WasBumped)
            .OrderBy(result => result.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (changedResults.Count == 0 && report.Warnings.Count == 0 && report.Errors.Count == 0)
        {
            return null;
        }

        var (displayName, includeId) = GetCommandPresentation(report.CommandName);

        var builder = new StringBuilder();
        builder.Append("## ").AppendLine(displayName);
        builder.AppendLine();

        foreach (var result in changedResults)
        {
            builder.Append("- ").AppendLine(FormatResult(result, includeId));
        }

        if (changedResults.Count > 0)
        {
            builder.AppendLine();
        }

        AppendSection(builder, "Warnings", report.Warnings);
        AppendSection(builder, "Errors", report.Errors);

        // AppendLine uses Environment.NewLine (CRLF on Windows); normalize to LF so the report is
        // byte-identical on every platform.
        return builder.ToString().Replace(Environment.NewLine, "\n", StringComparison.Ordinal);
    }

    private static (string DisplayName, bool IncludeId) GetCommandPresentation(string commandName)
    {
        if (string.Equals(commandName, "sdk", StringComparison.OrdinalIgnoreCase))
        {
            return ("SDK", false);
        }

        return (char.ToUpperInvariant(commandName[0]) + commandName[1..], true);
    }

    private static string FormatResult(BumpResult result, bool includeId)
    {
        return includeId
            ? $"{EscapeInline(result.Id)}: {EscapeInline(result.OldVersion)} → {EscapeInline(result.NewVersion)}"
            : $"{EscapeInline(result.OldVersion)} → {EscapeInline(result.NewVersion)}";
    }

    private static void AppendSection(StringBuilder builder, string heading, IReadOnlyCollection<string> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }

        builder.Append("### ").AppendLine(heading);
        builder.AppendLine();

        foreach (var line in lines)
        {
            builder.Append("- ").AppendLine(EscapeInline(line));
        }

        builder.AppendLine();
    }

    private static string EscapeInline(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Collapse embedded line breaks (and other whitespace runs) so a diagnostic cannot inject a
        // block (for example a heading), then escape the inline constructs that would otherwise
        // change how identifiers, versions and diagnostics render.
        var singleLine = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return singleLine
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("`", @"\`", StringComparison.Ordinal)
            .Replace("*", @"\*", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal)
            .Replace("[", @"\[", StringComparison.Ordinal)
            .Replace("]", @"\]", StringComparison.Ordinal);
    }
}
