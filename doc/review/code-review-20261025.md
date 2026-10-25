# Code Review: `language` Command + SDK Alignment Refactor (final branch state)

**Date:** 2026-10-25
**Scope:** branch `bump-language` vs `main` (PR #123, 16 commits ending at `7c51ad3`). Full final state of all
changed files was reviewed: `Commands/BumpLanguage/*`, `SdkAlignmentCommand`/`SdkAlignmentSettings`,
`BumpPathSettings`, `BumpCommandNames`, `CommandConfiguration`, `BumpReport`, `MarkdownReportFormatter`,
`LanguageVersion`/`LanguageEntry`/`LanguageManifest`, `XmlFileText` usage, the CI workflow, and all new/updated
tests under `test/DotBump.Tests`.
**Reviewer role:** .NET Tech Lead (code-only review; no code executed).
**Relation to prior review:** `doc/review/code-review-20261024.md` reviewed earlier commits. The resolutions claimed
there (M1, L1, L2, L4, L5, T1, T2) were re-verified against the final branch state and are confirmed. This document
covers the final state as an independent pass.

---

## Summary

Solid, well-tested feature. The final state is a clear improvement over the initially reviewed commits:

- `SdkAlignmentCommand<TSettings>` cleanly implements the template-method pattern; `BumpFrameworksCommand` and
  `BumpLanguageCommand` are now 27-line subclasses, and the guard/report/error handling flow is written (and
  potentially tested) once.
- `LanguageVersion` is a correct, minimal `readonly record struct` with `IComparable`, invariant parsing/formatting,
  and the mapping rule as a named constant (`SdkMajorOffset = 4`) — a single source of truth for "what is a numeric
  C# language version", used by the file service, the handler and the CLI description.
- The span-edit machinery (`LanguageFileService` + `XmlFileText`) preserves formatting, BOM and comments precisely,
  and the malformed-XML/comment-split/multi-element edge cases are all exercised by tests.
- Naming, logging (`MethodStart`/`MethodReturn`, message templates without interpolation), `DotBumpException`
  handling, `s_`/`PascalCase` conventions and file headers all follow the repository standards.

No critical or high-severity findings. Two low-severity issues were found in the final state (one duplication the
prior review did not call out, one path-resolution ordering caveat), plus two nits.

| # | Severity | Finding |
|---|----------|---------|
| L6 | Low | SDK-major guard block duplicated between `BumpLanguageHandler` and `BumpFrameworksHandler` |
| L7 | Low | Relative `--exclude` values are resolved against the current directory, not `--path` |
| N1 | Nit | `parts.Length is < 1 or > 2` — the `< 1` branch is unreachable |
| N2 | Nit | `LanguageVersionTests` does not cover overflow input (e.g. `"99999999999999999"`) |

Deferred items already accepted in the prior review are not re-reported here: **M2** (framework/language file-service
machinery duplication — revisit with a third value-bump command) and **L3** (stale-snapshot span edits at save time —
defensive hardening). Both remain valid deferrals at their stated severity.

---

## Low severity

### L6. Minimum-SDK-version guard is copy-pasted across both SDK-aligned handlers

**Files:** `src/DotBump/Commands/BumpLanguage/BumpLanguageHandler.cs:29-41`,
`src/DotBump/Commands/BumpFrameworks/BumpFrameworksHandler.cs:28-40`

Both handlers contain the exact same 12-line block, differing only in the message subject:

```csharp
var currentSdk = sdkFileService.GetCurrentSdkVersionFromFile(globalJsonPath);
if (!currentSdk.SemanticVersion.IsValid || currentSdk.SemanticVersion.Major <= 0)
{
    throw new DotBumpException(
        $"Failed to determine a valid {subject} from SDK version '{currentSdk.Version}'.");
}

if (currentSdk.SemanticVersion.Major < MinimumSdkMajor)
{
    throw new DotBumpException(
        $"The {commandName} command requires .NET {MinimumSdkMajor} or later; " +
        $"SDK version '{currentSdk.Version}' cannot be mapped to a {subject}.");
}
```

Same `private const int MinimumSdkMajor = 5;` in both classes. This refactor commit introduced
`SdkAlignmentCommand`/`SdkAlignmentSettings` precisely to hoist shared validation for SDK-aligned commands, but the
handler-side guard was left duplicated. A future change to the mapping threshold (e.g. a new C# mapping rule) must be
edited in two places with no compile-time coupling to keep them aligned.

**Suggestion:** extract a small shared helper used by both handlers, parameterized only on the message subject:

```csharp
internal static class SdkVersionGuard
{
    public const int MinimumSdkMajor = 5;

    public static int GetValidatedSdkMajor(ISdkFileService sdkFileService, string globalJsonPath, string subject)
    {
        var currentSdk = sdkFileService.GetCurrentSdkVersionFromFile(globalJsonPath);
        if (!currentSdk.SemanticVersion.IsValid || currentSdk.SemanticVersion.Major <= 0)
        {
            throw new DotBumpException(
                $"Failed to determine a valid {subject} from SDK version '{currentSdk.Version}'.");
        }

        if (currentSdk.SemanticVersion.Major < MinimumSdkMajor)
        {
            throw new DotBumpException(
                $"The command requires .NET {MinimumSdkMajor} or later; " +
                $"SDK version '{currentSdk.Version}' cannot be mapped to a {subject}.");
        }

        return currentSdk.SemanticVersion.Major;
    }
}
```

The existing "legacy SDK throws before scanning" tests in both handler test suites continue to pass unchanged. Note
the current messages differ slightly ("The frameworks command requires…" vs "The language command requires…"), so
keep that variability in the parameter; if both messages should become identical, say so explicitly when applying.

### L7. Relative `--exclude` paths resolve against the current directory, not `--path`

**Files:** `src/DotBump/Commands/SdkAlignmentCommand.cs:43-49`

```csharp
var repositoryPath = !string.IsNullOrWhiteSpace(settings.RepositoryPath)
    ? Path.GetFullPath(settings.RepositoryPath)
    : _defaultRepositoryPath;
var excludedPaths = settings.Exclude?
    .Where(path => !string.IsNullOrWhiteSpace(path))
    .Select(Path.GetFullPath)
    .ToArray();
```

`repositoryPath` is resolved from `--path`, but `Path.GetFullPath` on the exclude values resolves them against
`Directory.GetCurrentDirectory()`. When `--path` points at a different tree than the current directory, a relative
`--exclude ./foo` is silently attached to the wrong root: the excluded directory either does not exist at the
resolved location (matches nothing, silently) or — worse — matches a directory outside the scanned tree. It is
validate-then-misapply: `BumpPathSettings.ValidateExcludedDirectories` validates the user's *intent* (the directory
given), while execution checks a different absolute location.

In the CI workflow the current directory and `--path` coincide, so behavior is correct today; the risk is local CLI
use (`dotnet dotbump language --path ../other-repo --exclude ./temp`).

**Suggestion:** resolve relative exclude values against the effective repository root — for example give
`RepositoryFileScanner`/the caller a `Path.Combine(repositoryPath, exclude)` step, or validate-and-resolve both
through one normalization helper in `BumpPathSettings`. If cwd-relative resolution is intentional, consider
documenting it in the `--exclude` option description.

---

## Nits (non-blocking)

### N1. Unreachable pattern arm in `LanguageVersion.TryParse`

**File:** `src/DotBump/Commands/BumpLanguage/DataModel/LanguageVersion.cs:42-47`

```csharp
var parts = value.Split('.', StringSplitOptions.None);
if (parts.Length is < 1 or > 2 || !int.TryParse(...))
```

`string.Split(char)` never returns an empty array, so `parts.Length < 1` cannot be true (the empty/whitespace cases
already returned earlier). Only `> 2` is doing work. Simplify to `parts.Length > 2` to avoid implying a possible
state that cannot occur. Alternatively, use `StringSplitOptions.TrimEntries` so `" 12 "` — which currently produces a
parsing failure on `int.TryParse` with `NumberStyles.None` — parses cleanly; today `ReadLanguageVersions` trims the
element value before calling `TryParse` (`LanguageFileService.cs:116`), so behavior is already correct, but the
struct's own contract is slightly narrower than the "numeric version" concept it exposes.

### N2. `LanguageVersion.TryParse` overflow input not covered by tests

**File:** `test/DotBump.Tests/Commands/BumpLanguage/LanguageVersionTests.cs:24-38`

`NumberStyles.None` + `int.TryParse` correctly rejects `"99999999999999999"` (overflow), but no test locks this in.
Add one negative `InlineData` (`"9223372036854775807"` or similar) to the existing `With_Non_Numeric_Value_Returns_False`
theory so overflow rejection is an asserted contract rather than an incidental property of the runtime.

---

## Test review

Strong overall, and the improvements claimed between commits were verified:

- **Handler:** SDK-mapping coverage is complete for SDK 5→10 via `[Theory]`, plus "current version not lower
  (including un-suffixed `14`)", "no changes → no save", "invalid/legacy SDK throws before scanning *and* saving"
  (verifying zero side effects via `Times.Never`), exclude forwarding, and warning propagation
  (`BumpLanguageHandlerTests`). The `MinimumSdkMajor` boundary (SDK 4.x vs 5.0) is exercised through the legacy-SDK
  test; an explicit `"4.9.999"` inline case would make the boundary itself visible, but is optional.
- **File service:** candidate-file selection (`.csproj`, `Directory.Build.props`/`.targets`, `.vbproj` ignored),
  exclusion applies, non-numeric/property-expression values skipped, comment-split value → warning + untouched file,
  invalid XML → `DotBumpException`, BOM preserved byte-wise, multiple elements updated, missing file text and invalid
  span defensive branches both covered with warning/untouched assertions.
- **Command (CLI):** exit codes for success/no-change/failure, "unsupported command name" guard, full integration
  through a real `BumpLanguageHandler` + `SdkFileService` + real files (report in markdown, project updated,
  legacy-SDK path leaves the project untouched), per the `TestConsole`/`CommandContext` canonical pattern.
- **Settings:** every validation arm (file, path, exclude: success/missing/invalid/whitespace) is covered.
- Conventions are followed throughout: nested method-group classes, `With_{Condition}_Returns_{Outcome}` names, AAA,
  `Shouldly` assertions, `Mock.Of`/`Mock<T>` + `Verify`, isolated `LocalDirectory("./temp/...")` with reset.

No new test findings.

---

## Security

- Report output paths, identifiers and diagnostics are escaped before console markup (`Markup.Escape`) and before
  markdown (`EscapeInline` collapsing line breaks + backtick/`*`/`_`/bracket escaping) — injection-safe for both sink
  types, including warning strings that embed file content-derived values.
- The language command performs only local file reads within the (resolvable) repository root; no network, secret, or
  shell surface was added. The workflow change keeps `contents: read` / `packages: read` permissions.

## Verdict

**Approve.** No findings require changes before merge. L6 (shared SDK guard) and L7 (relative exclude resolution) are
worth a follow-up commit — both are small; N1/N2 are optional polish. The previously accepted deferrals (M2, L3)
remain trackable debt with a clear trigger condition (third value-bump command / concurrent-writer use case).
