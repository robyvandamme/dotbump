// Copyright © Roby Van Damme.

namespace DotBump.Reports;

/// <summary>
/// The format in which a bump report is written.
/// </summary>
internal enum ReportFormat
{
    /// <summary>
    /// The full, machine-readable JSON report.
    /// </summary>
    Json,

    /// <summary>
    /// A concise markdown report, listing only what changed (and any warnings or errors).
    /// </summary>
    Markdown,
}
