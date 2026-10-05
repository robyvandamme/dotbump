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

        var changedResults = report.Results
            .Where(result => result.WasBumped)
            .OrderBy(result => result.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (changedResults.Count == 0 && report.Warnings.Count == 0 && report.Errors.Count == 0)
        {
            return null;
        }

        var builder = new StringBuilder();
        builder.Append("## ").AppendLine(GetDisplayName(report.CommandName));
        builder.AppendLine();

        foreach (var result in changedResults)
        {
            builder.Append("- ").AppendLine(FormatResult(report.CommandName, result));
        }

        if (changedResults.Count > 0)
        {
            builder.AppendLine();
        }

        AppendSection(builder, "Warnings", report.Warnings);
        AppendSection(builder, "Errors", report.Errors);

        return builder.ToString();
    }

    private static string GetDisplayName(string commandName)
    {
        return commandName switch
        {
            "sdk" => "SDK",
            _ => char.ToUpperInvariant(commandName[0]) + commandName[1..],
        };
    }

    private static string FormatResult(string commandName, BumpResult result)
    {
        return string.Equals(commandName, "sdk", StringComparison.OrdinalIgnoreCase)
            ? $"{result.OldVersion} → {result.NewVersion}"
            : $"{result.Id}: {result.OldVersion} → {result.NewVersion}";
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
            builder.Append("- ").AppendLine(line);
        }

        builder.AppendLine();
    }
}
