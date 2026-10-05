# Code Review: Markdown Report Generation

**Date:** 2026-10-05
**Scope:** commit `7a5f378` — `feat: add markdown report generation` (branch `markdown-report`) — new `Reports/MarkdownReportFormatter.cs`, `Reports/ReportFormat.cs`, `Reports/ReportFormatResolver.cs`, the `WriteToFileAsync` rework in `Reports/BumpReport.cs`, `--output` description updates in the three command settings classes, and the new/changed tests in `test/DotBump.Tests/`.
**Reviewer role:** .NET Tech Lead (code-only review; no code executed).

---

## Summary

The feature is a clean, well-scoped addition. Format resolution is isolated in a tiny, fully unit-tested resolver; the formatter is a pure static function with no I/O; and the refactor of `WriteToFileAsync` to guard clauses improves the existing method. Documentation (XML comments), file headers, naming, and test structure all follow the project conventions, and the previous review's H1 lesson (escape dynamic content per output medium) was correctly applied to the console paths — but not yet to the new markdown medium.

No critical or high-severity issues. One medium-severity behavioural inconsistency deserves a decision before merge; the rest are low-severity maintainability and hardening items.

| # | Severity | Finding |
|---|----------|---------|
| M1 | Medium | Empty markdown report silently skips the write, leaving a stale file from a previous run |
| L1 | Low | Command-specific formatting knowledge duplicated across `GetDisplayName`/`FormatResult`, with inconsistent comparison semantics and an unguarded `commandName[0]` |
| L2 | Low | Report content is injected into markdown unescaped/unnormalized (newlines break list items; `_`/`*`/`[` can mangle rendering) |
| L3 | Low | Identical `[Description]` string duplicated in three settings classes |
| L4 | Low | `WriteToFileAsync` accumulating format dispatch + serialization + I/O; JSON options allocated per call |
| L5 | Low | Unrecognized output extensions silently fall back to JSON with no signal to the user |
| T1–T4 | Low | Test gaps: stale-file behaviour, `null` resolver input, combined sections, console-output assertion |

---

## Medium severity

### M1. Empty markdown report silently skips the write, leaving a stale file

**Files:** `src/DotBump/Reports/BumpReport.cs:149-158`

```csharp
if (ReportFormatResolver.Resolve(outputFile) == ReportFormat.Markdown)
{
    var markdown = MarkdownReportFormatter.Format(this);
    if (markdown != null)
    {
        await File.WriteAllTextAsync(outputFile, markdown, new UTF8Encoding());
    }

    return;
}
```

The two formats now have different write contracts: JSON **always** overwrites the target file, markdown **sometimes** writes and otherwise leaves whatever is already there. Because the method returns silently (no log, no return value, no exception), a caller cannot distinguish "nothing to report" from "report written".

The realistic failure mode is the exact scenario this feature targets — automated PR generation. With a fixed output path reused across runs (e.g. a nightly `dotnet dotbump packages -o report.md`), a run that bumps nothing leaves the *previous* run's report on disk. Any downstream step that reads `report.md` (PR body, job summary, artifact upload) will consume stale content while the run itself reports success.

**Suggestion (pick one, and document it in the README):**

1. Delete (or truncate) an existing file when there is nothing to report, so absence of content means absence of file *and* a leftover file never lies:
   ```csharp
   var markdown = MarkdownReportFormatter.Format(this);
   if (markdown != null)
   {
       await File.WriteAllTextAsync(outputFile, markdown, new UTF8Encoding());
   }
   else if (File.Exists(outputFile))
   {
       File.Delete(outputFile);
   }
   ```
2. Or keep "no write" but make it observable — have `WriteToFileAsync` return `bool`/an enum and log a debug line in the calling commands (which already log `Output file : {OutputFile}`).

Option 1 is the safer default for CI/automation; it also makes the markdown branch symmetric with the JSON branch.

---

## Low severity

### L1. Duplicated command-specific knowledge in the formatter

**Files:** `src/DotBump/Reports/MarkdownReportFormatter.cs:56-70`

```csharp
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
```

Three issues in one pair of methods:

1. **Two switches encode the same fact** ("what is special about the `sdk` command"). Adding or renaming a command means touching both, and they can drift.
2. **Inconsistent comparison semantics:** `GetDisplayName` matches `"sdk"` ordinally, `FormatResult` case-insensitively. They happen to produce coherent output for `"SDK"` today, but only by accident.
3. **`commandName[0]` throws** `IndexOutOfRangeException`/`NullReferenceException` for an empty/null name. `BumpReport.CommandName` is `{ get; init; }` with no guard, so nothing at the type level prevents it.

**Suggestion:** resolve the command's presentation once, and validate the invariant at the source:

```csharp
private static (string DisplayName, bool IncludeId) GetCommandPresentation(string commandName) =>
    commandName.ToLowerInvariant() switch
    {
        "sdk" => ("SDK", false),
        "" => throw new DotBumpException("Report command name must not be empty."),
        _ => (char.ToUpperInvariant(commandName[0]) + commandName[1..], true),
    };
```

and make `CommandName` `required` on `BumpReport` (C# 11+) so every constructor/object initializer must supply it.

### L2. Report content is not escaped for the markdown medium

**Files:** `src/DotBump/Reports/MarkdownReportFormatter.cs:42`, `:84`

```csharp
builder.Append("- ").AppendLine(FormatResult(report.CommandName, result));
...
builder.Append("- ").AppendLine(line);
```

The console paths were hardened against their output medium in the previous review (H1: `Markup.Escape` on every dynamic value). The markdown path inserts the same strings raw into a document intended to be pasted into a PR body:

- A warning/error containing a newline (error messages embed file paths and, in `BumpPackagesHandler.GetReferenceVersions`, arbitrary version text from manifests) terminates the bullet item; the remainder renders as free-floating text — or, if it starts with `#`/`-`, as injected structure.
- Unescaped `_`, `*`, `[`, `` ` `` in package IDs or versions mangle rendering: a version like `1.0.0-beta_2` renders `_beta_` as emphasis.

Today all inputs are internally generated, so this is hardening rather than a vulnerability — but the cost of fixing it is one small helper.

**Suggestion:**

```csharp
private static string EscapeInline(string value)
{
    var singleLine = value.Replace('\r', ' ').Replace('\n', ' ');
    return singleLine.Replace("[", "\\[", StringComparison.Ordinal);
}

// versions/ids are identifiers — code spans avoid all emphasis parsing:
builder.Append("- `").Append(EscapeInline(result.Id)).Append("`: `")
       .Append(EscapeInline(result.OldVersion)).Append("` → `")
       .Append(EscapeInline(result.NewVersion)).AppendLine("`");
```

(If you prefer not to backtick-wrap — the README examples don't — at minimum apply the newline normalization to warnings/errors.)

### L3. Duplicated `[Description]` string across three settings classes

**Files:** `src/DotBump/Commands/BumpPackages/BumpPackagesSettings.cs:19`, `src/DotBump/Commands/BumpSdk/BumpSdkSettings.cs:21`, `src/DotBump/Commands/BumpTools/BumpToolsSettings.cs:19`

All three now carry the byte-identical string:

```csharp
[Description("Output file name. The name of the file to write the result to. The output format is inferred from the file extension: `.json` or `.md`.")]
```

The `-o|--output` option itself is already declared three times (pre-existing), but this branch triples a long, easy-to-desync sentence. Next wording change will inevitably miss one copy.

**Suggestion:** a `const` on the shared base class, which attributes can reference:

```csharp
// BumpSettings.cs
public const string OutputOptionDescription =
    "Output file name. The name of the file to write the result to. " +
    "The output format is inferred from the file extension: `.json` or `.md`.";
```

```csharp
[Description(BumpSettings.OutputOptionDescription)]
```

### L4. `WriteToFileAsync` now owns dispatch, serialization and I/O

**Files:** `src/DotBump/Reports/BumpReport.cs:142-175`

The previous review deferred finding M6 (`BumpReport` mixed responsibilities, "expected to resurface with the planned markdown report output"). It has now resurfaced: the method grew a format branch, and the JSON `JsonSerializerOptions` (with its non-trivial justification comment) is still constructed on every call.

This is not urgent — one `if` is readable — but two cheap improvements pay off:

```csharp
private static readonly JsonSerializerOptions s_jsonOptions = new()
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    // ... existing comment and settings ...
};

private static readonly UTF8Encoding s_utf8NoBom = new();

public async Task WriteToFileAsync(string? outputFile)
{
    if (string.IsNullOrWhiteSpace(outputFile))
    {
        return;
    }

    var content = ReportFormatResolver.Resolve(outputFile) == ReportFormat.Markdown
        ? MarkdownReportFormatter.Format(this)
        : JsonSerializer.Serialize(this, s_jsonOptions);

    if (content is null) { /* M1 handling */ return; }

    await File.WriteAllTextAsync(outputFile, content, s_utf8NoBom);
}
```

That collapses both branches into one write, removes the per-call allocations, and keeps `BumpReport` at a single (if still multi-purpose) entry point. A full `IReportWriter` strategy is not warranted at two formats.

### L5. Unrecognized extensions silently produce JSON

**Files:** `src/DotBump/Reports/ReportFormatResolver.cs:23-29`

`.mkd`, `.mdown`, `.MD.txt` — all silently yield JSON, and nothing in the output tells the user which format was chosen. The behaviour is documented in the README, but a typo in a script means "I asked for markdown and got a JSON blob" with no diagnostic.

**Suggestion:** the commands already log `Output file : {OutputFile}`; add the resolved format there (or log a warning when the extension is neither `.json` nor a markdown variant). One line, no API change:

```csharp
logger.Debug("Report format: {Format}", ReportFormatResolver.Resolve(outputFile));
```

---

## Test review

Coverage is good: the formatter, the resolver and the `WriteToFileAsync` branches each have dedicated tests, naming follows `Pascal_Snake_Case`, structure mirrors production, Shouldly is used throughout, and disk usage stays inside `./temp` with cleanup. The `ReportFormatResolverTests` case matrix (`.MD`, `.markdown`, `.Markdown`, `.JSON`, no extension, whitespace) is exactly right.

Gaps, in priority order:

- **T1 — stale-file behaviour is untested (covers M1).** `With_Markdown_Output_And_No_Content_Does_Not_Write_File` only proves nothing is *created*. Add: pre-create `empty-report.md` with known content, run an empty report, and assert the chosen contract (file deleted / truncated / still stale — whichever M1 resolves to). Right now the riskiest behaviour of the feature is the one no test pins down.
- **T2 — `Resolve(null)` not covered.** `With_Empty_Path_Throws_ArgumentException` covers `""` and `"   "`, but `ArgumentException.ThrowIfNullOrWhiteSpace` throws `ArgumentNullException` for null — a different branch. Add `[InlineData(null)]` expecting `ArgumentNullException` (or keep `ArgumentException`, since it is the base type, but exercise the input).
- **T3 — no test for changes *and* warnings *and* errors together.** The formatter's blank-line placement logic (`Format` emits one blank line after the item list, `AppendSection` emits a trailing blank line after each section) is only ever exercised pairwise. A single combined test would lock the inter-section spacing.
- **T4 (nit) — end-to-end markdown is only covered for `bump sdk`.** `bump tools` / `bump packages` rely on the shared `WriteToFileAsync` + unit tests, which is acceptable; the formatter tests cover their output shape. The new `With_Markdown_Output_Parameter_Writes_Markdown_Returns_0` also drops the `testConsole.Output.ShouldContain("SDK version bumped")` assertion its JSON twin (`With_Output_Parameter_Returns_0`) has — worth adding for parity.

Positive note: the fact that a report containing **only errors** still resolves to non-null content (and therefore gets written) is an important behaviour for CI, and `With_Errors_Returns_Errors_Section` covers the formatter side of it.

---

## What's good

- `WriteToFileAsync` was converted to guard clauses — the early return on empty `outputFile` reads better than the previous nested `if`.
- `ReportFormatResolver` is a textbook small, single-purpose, fully tested unit; case-insensitive extension matching with a JSON default is the right default.
- `MarkdownReportFormatter` is pure (no I/O, no ambient state), returns `null` for "nothing to report" with the contract documented in XML docs, and sorts entries with `StringComparer.OrdinalIgnoreCase` for deterministic output.
- New files carry the required header, file-scoped namespaces, sorted usings, and XML documentation; tests mirror production layout and use the documented `LocalDirectory`/`try-finally` isolation pattern.
- Errors are written to the report *before* the commands return exit code `1`, so a failed run still produces a markdown artifact including the `### Errors` section.

---

## Recommendation

Resolve **M1** (decide and test the stale-file contract) before merge; **L1** and **L3** are small, worthwhile cleanups in code this branch already touches. **L2**, **L4** and **L5** can be deferred or folded into follow-ups — consistent with how the previous review batched hardening and structural refactors.
