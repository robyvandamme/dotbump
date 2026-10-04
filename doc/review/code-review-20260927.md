# Code Review: Bump Packages Functionality

**Date:** 2026-09-27
**Scope:** `dotnet dotbump packages` feature — `src/DotBump/Commands/BumpPackages/`, its use of `NuGet/PackageVersionResolver.cs` and shared NuGet services, report integration in `Reports/BumpReport.cs`, and the corresponding tests in `test/DotBump.Tests/Commands/BumpPackages/`.
**Reviewer role:** .NET Tech Lead (code-only review; no code executed).

---

## Summary

The feature is well built overall. Notable strengths: the span-based, in-place rewrite of MSBuild files (instead of an XML round-trip) is the right call and is thoroughly tested for formatting preservation; there is a clear no-downgrade guard; symlink/reparse points and build-output directories are skipped; configuration is validated before any network call; and credentials in `NuGetClientCredential` are correctly protected with `[LogMasked]`.

Findings below are ordered by severity. No critical (data-loss/security-breach) issues were found, but two high-severity items deserve attention before broader adoption.

| # | Severity | Finding |
|---|----------|---------|
| H1 | High | Unescaped Spectre markup in console output can fail the command on paths containing `[`/`]` — resolved, see H1 |
| H2 | High | Version spans are applied without verifying the span content matches the original version (corruption risk) — partially resolved, see H2 |
| M1 | Medium | `CancellationToken` is accepted but never propagated to handler/resolver/HTTP calls |
| M2 | Medium | Fully sequential HTTP resolution (sources × packages) with all-or-nothing failure semantics |
| M3 | Medium | `PackageManifest.Warnings` is write-only dead state; skipped packages are never surfaced — resolved, see M3 |
| M4 | Medium | NuGet config content and raw credential values can end up in logs — resolved (handled at high priority), see M4 |
| M5 | Medium | Duplicated "highest valid version per package" logic, O(ids × occurrences) |
| M6 | Medium | `BumpReport` has temporal coupling (ctor before mutation, `ReportChanges` after) and mixed responsibilities |
| M7 | Medium | Inconsistent case-sensitivity semantics for version comparison vs. change detection |
| L1–L8 | Low | Validator/key casing, validation exceptions, hardcoded command name, dictionary comparers, repeated parsing, SRP of `PackageFileService`, contract validation |

---

## High severity

### H1. Unescaped Spectre markup in console output

> **Status (2026-09-30): resolved.**
>
> All dynamic content passed to Spectre is escaped (`Markup.Escape`) in `BumpPackagesCommand`, `BumpSdkCommand` and
> `BumpToolsCommand`: the settings line, the error lines and the result lines. Regression tests feed `[draft]`-style
> markup-unsafe paths through all three commands (test gap 4).

**Files:** `src/DotBump/Commands/BumpPackages/BumpPackagesCommand.cs:58-59`, `:94`, `:110`

```csharp
console.MarkupLine(
    $"Bumping Packages with settings: type={bumpType}, path={repositoryPath}, output: {outputFile ?? "none"}, config: {nugetConfigPath}");
...
console.MarkupLine(bumpReportError);          // error messages contain file paths
...
console.MarkupLine(bumpResult.ToString());    // BumpResult.ToString() contains FilePath
```

Spectre interprets `[` and `]` as markup tags. A repository path such as `/repos/[draft]/app` (or an error message containing one) causes a `MarkupParseException`, which is then swallowed by the catch-all at line 74 — so the run fails with exit code `1` and a stack trace for what is purely a display problem.

**Suggestion:** escape all dynamic content, or use plain writes:

```csharp
console.MarkupLine(
    $"Bumping Packages with settings: type={bumpType}, path={Markup.Escape(repositoryPath)}, ...");
...
console.WriteLine(bumpResult.ToString());
```

Note: the same pattern exists in `BumpSdkCommand` and `BumpToolsCommand`; fixing it in a shared helper (e.g. an `IConsole` extension) would cover all three.

### H2. Version spans are applied without verifying the span content

> **Status (2026-09-30): partially resolved.**
>
> - **Part 2 — silent `GetOffset` fallback: fixed.** `GetOffset` now returns `-1`, both span-capture
>   callers reject out-of-range offsets, and an unlocatable span is skipped at read time with a
>   `Warning` and a manifest warning. Covered by the `GetOffset`, `GetVersionSpan` and
>   `With_Out_Of_Bounds_Span_Skips_And_Warns` tests.
> - **Part 1 — span-content assertion in `ApplyChanges`: deliberately not fixed.** The span is raw
>   (possibly entity-encoded) text while `OriginalVersion` is decoded, so the literal check below
>   would reject valid entity-encoded versions (see `With_Entity_Encoded_Version_Normalizes_Value_On_Bump`).
>   The rationale is documented in code at `ReadVersion` (guard removed in `eb01a576`) and at the
>   `ApplyChanges` bounds check; span/content drift is impossible in practice because the save path
>   edits the same captured text the span was computed from.

**Files:** `src/DotBump/Commands/BumpPackages/PackageFileService.cs:204-213`, `:225-259`, `:432-466`

`ApplyChanges` validates only that the recorded span lies within the text:

```csharp
if (package.VersionStart < 0
    || package.VersionLength <= 0
    || package.VersionStart + package.VersionLength > text.Length)
{
    logger.Warning(...);
    continue;
}

edits.Add((package.VersionStart, package.VersionLength, package.Version));
```

It never verifies that `text[VersionStart..VersionStart+VersionLength]` actually equals `OriginalVersion`. The span is derived heuristically (`IndexOf('=')` from line info in `GetAttributeVersionSpan`), and `GetOffset` silently falls back to offset `0` when line information is out of range:

```csharp
private static int GetOffset(int[] lineStartOffsets, int lineNumber, int linePosition)
{
    var lineIndex = lineNumber - 1;
    if (lineIndex < 0 || lineIndex >= lineStartOffsets.Length)
    {
        return 0;   // ← silent fallback to start of file
    }
    return lineStartOffsets[lineIndex] + (linePosition - 1);
}
```

If that fallback (or any line-info drift) ever fires, the rewrite would splice a version string into an unrelated location — silently corrupting the file. The consequence is severe enough that a cheap assertion is worth it:

```csharp
var spanText = text.Substring(package.VersionStart, package.VersionLength);
if (!string.Equals(spanText, package.OriginalVersion, StringComparison.Ordinal))
{
    logger.Warning(
        "Skipping version '{Version}' for {PackageId} in {File}: span content '{SpanText}' does not match",
        package.OriginalVersion, package.PackageId, filePath, spanText);
    continue;
}
```

Same check should ideally live at span-capture time (`ReadPackageVersions`), so the mismatch is reported at read time rather than at save time.

---

## Medium severity

### M1. CancellationToken is never propagated

**Files:** `BumpPackagesCommand.cs:27-30`, `Interfaces/IBumpPackagesHandler.cs:9`, `NuGet/Interfaces/IPackageVersionResolver.cs:22-25`, `NuGet/NuGetClient.cs:32`, `:72`, `:115`

`ExecuteAsync` receives a `cancellationToken` (Spectre supplies one wired to Ctrl+C) but never passes it on. The entire chain — `HandleAsync` → `ResolveAsync` → `NuGetClient.GetStringAsync` — has no cancellation support. A long scan plus hundreds of feed requests cannot be interrupted.

**Suggestion:** add `CancellationToken cancellationToken = default` to `IBumpPackagesHandler.HandleAsync`, `IPackageVersionResolver.ResolveAsync`, and pass it to `httpClient.GetStringAsync(url, cancellationToken)`. This is a natural follow-up to the interface, and both interfaces are `internal`, so the change is cheap.

### M2. Sequential resolution with all-or-nothing failure

**File:** `src/DotBump/NuGet/PackageVersionResolver.cs:38-58`

```csharp
foreach (var nugetPackageSource in nuGetConfiguration.PackageSources)
{
    ...
    foreach (var package in packageList)
    {
        var candidateVersion = await ResolvePackageVersionAsync(...).ConfigureAwait(false);
        ...
    }
}
```

Every package is queried against every source strictly sequentially (plus follow-up catalog-page fetches). A repository with 200 packages and 2 feeds is 400+ serial HTTP round-trips.

Additionally, a single non-404 HTTP failure (transient 500, timeout) aborts the entire command (fail-fast, exit 1, nothing saved — behavior is asserted in `BumpPackagesCommandTests`). Fail-fast is a defensible choice, but it means one flaky package blocks bumping all others.

**Suggestions:**
- Parallelize with bounded concurrency (e.g. `Parallel.ForEachAsync` with `MaxDegreeOfParallelism = 4..8`, or `SemaphoreSlim`), honoring the cancellation token from M1. The per-source service index fetch can stay sequential.
- Consider collecting per-package failures into `bumpReport.ReportErrors(...)` and continuing, so partial results are still reported (the report already has an error channel, and the command already returns `1` when errors exist).

### M3. `PackageManifest.Warnings` is write-only dead state

> **Status (2026-09-30): resolved — surfaced via the report.**
>
> Warnings now flow service → `PackageManifest.Warnings` → `BumpReport.ReportWarnings(...)` → the `warnings` array in
> the JSON report (and the markdown output planned for automated dependency-update PRs). Deliberately **not** printed
> to the console, and the default log level stays `Error`; every warning-semantic message is logged at
> `LogEventLevel.Warning`, so it appears in logs only with `--debug`. This also covers the save-time skip (test gap 8)
> and packages whose versions cannot be parsed as a semantic version (e.g. four-part versions like `1.2.3.4`).
> Covered by `With_Unapplied_Span_Surfaces_Warning`, `With_Unsupported_Versions_Skips_And_Warns` (log level),
> `With_Read_Warnings_Reports_Warnings`, `With_Validation_Errors_Still_Reports_Read_Warnings`,
> `With_Invalid_Semantic_Version_Skips_And_Reports_Warning` and `BumpReportTests`.

**Files:** `PackageFileService.cs:392-396`, `DataModel/PackageManifest.cs:23`

Warnings for skipped packages (ranges, floating versions, `$(Property)` versions) are collected via `manifest.AddWarning(...)`, but no production code ever reads `.Warnings` — only tests do. The user-visible outcome comes solely from the parallel `logger.Debug` call, which matches the README ("only logged at debug level"), which makes the collected list redundant.

**Suggestion:** either surface warnings (console summary after the bump, and/or a `warnings` array in the JSON report — users who run without `--debug` currently get no indication a package was skipped), or drop `Warnings`/`AddWarning` entirely and keep the debug log only. Collecting state that nobody consumes will rot.

### M4. NuGet config content and raw credential values can be logged

> **Status (2026-09-30): resolved — handled at high priority.**
>
> - **Raw config content is no longer logged.** Both `{Content}` Error messages in `NuGetConfigFileService` now log
>   the file path (plus credential-section names for the "no package sources" case) instead of the raw XML, which
>   previously dumped `packageSourceCredentials` — including plaintext passwords — into the default-visible
>   (`Error`) log.
> - **Attribute-based redaction is actually wired.** `.Destructure.UsingAttributes()` had been accidentally removed
>   in an earlier commit, which made every `[LogMasked]`/`[NotLogged]` attribute inert (including the
>   `NuGetClientCredential` masking praised above). It is restored and now lives in a single
>   `LoggerConfigurator.CreateConfiguration(...)` seam shared by production and tests, so a removal fails a test.
> - The `MethodStart`/`MethodReturn` destructuring of `NuGetConfig` and `NuGetClientConfig` is deliberately kept:
>   it shows which sources and credential keys exist, with values rendered as `***`. Note that masking only covers
>   properties carrying `[LogMasked]` — new credential-shaped properties must carry the attribute too.
> - Covered by `With_Credentials_Only_Logs_Without_Config_Content`,
>   `With_Plaintext_Credential_Debug_Log_Redacts_Value`, `LoggerConfiguratorTests` and `NuGetClientFactoryTests`.

**Files:** `NuGet/NuGetConfigFileService.cs:65`, `:88-91`, `:39-45`, `:45`; `NuGet/DataModel/NuGetConfiguration/Credential.cs`

```csharp
logger.Error("Unable to read the nuget config file at {FilePath} with {Content}", filePath, doc);
...
logger.Error("No package sources were found in the NuGet config {FilePath} with content {Content}", filePath, doc);
...
logger.MethodReturn(nameof(NuGetConfigFileService), nameof(GetNuGetConfiguration), config); // destructures credentials
```

- The `{Content}` logs dump the **entire nuget.config XML**, including `packageSourceCredentials`, at Error level (always logged, not debug-gated).
- `MethodReturn(..., config)` destructures `NuGetConfig` → `SourceCredential` → `Credential.Value`. Unlike `NuGetClientCredential` (which correctly carries `[LogMasked]`), `Credential.Value` has no masking, so a plaintext password written directly in the config lands in the debug log. The validator rejects such passwords *after* this log line has already been written.

**Suggestions:** add `[LogMasked]` to `Credential.Value` (Destructurama is already wired up), and stop logging raw document content — log the file path and a summary (source count, credential keys) instead.

### M5. Duplicated "highest valid version" logic, computed quadratically

**Files:** `BumpPackagesHandler.cs:73-97`, `Reports/BumpReport.cs:150-170`, `DataModel/PackageManifest.cs:50-61`

The same rule — "group occurrences by id (case-insensitive), take the highest valid `SemanticVersion`" — is implemented three times:

1. `BumpPackagesHandler.GetReferenceVersions` — re-filters `manifest.Packages` once **per distinct id** (O(ids × occurrences)).
2. `BumpReport.GetReferenceVersion` — same scan, invoked once per id from the constructor (line 44-47) *and again* per id from `ReportChanges` (line 96-105), each time re-parsing `SemanticVersion` from strings.
3. `PackageManifest.SetVersion` — linear filter per call.

**Suggestion:** expose the grouping once on the manifest and reuse it:

```csharp
public ILookup<string, PackageVersionEntry> GetEntriesById()
    => _packages.ToLookup(p => p.PackageId, StringComparer.OrdinalIgnoreCase);

public bool TryGetReferenceVersion(string packageId, bool useOriginal, out string version) { ... }
```

Then the handler, the report constructor, and `ReportChanges` all call the same helper, and the repeated `new SemanticVersion(...)` parsing (see L6) is done once per entry instead of several times per run.

### M6. `BumpReport`: temporal coupling and mixed responsibilities

**File:** `src/DotBump/Reports/BumpReport.cs:20-48`, `:70-108`, `:123-127`

The packages flow depends on a specific call order:

1. `new BumpReport(manifest, bumpType)` snapshots `OldVersion` from `OriginalVersion`.
2. The handler mutates the manifest via `SetVersion`.
3. `ReportChanges(manifest)` recomputes `NewVersion` from the *mutated* versions.

If step 3 is skipped or reordered, the report silently reports wrong data — no guard exists. Additionally, the class is becoming a hub of command-specific behavior: three constructors, three `ReportChanges` overloads, plus `ReportNoSdkVersionChanges()` (SDK-only) living on the shared type.

**Suggestion:** build results in one shot after resolution, from the entry pairs themselves — e.g. `BumpReport.FromManifest(manifest, bumpType)` reading `OriginalVersion` vs. `Version` per id — eliminating the two-phase protocol. Longer term, consider splitting per-command reporting (or a small `IBumpReportBuilder`) so shared JSON serialization stays in one place while command-specific logic lives with the command.

### M7. Inconsistent case-sensitivity for versions

> **Status (2026-10-03): resolved — aligned on case-insensitive semantics (NuGet-style).**
>
> Chosen direction: a version that differs only by casing is **not** a different version. `SemanticVersion`
> ordering now compares pre-release identifiers with `OrdinalIgnoreCase` (both numeric identifiers still compare
> numerically), and `SemanticVersion` equality/`GetHashCode` were overridden so a record-equal comparison agrees
> with `CompareTo == 0` (e.g. `1.0.0-RC1` equals `1.0.0-rc1`). Change detection was already `OrdinalIgnoreCase`
> and is left unchanged, so ordering and change detection now agree: a casing-only difference resolves as
> `CompareTo == 0`, fails the `newVersion > reference` guard, and is correctly reported as "no change" instead of
> applying then reporting "No package versions were bumped."
>
> This is a deliberate deviation from SemVer's "ASCII sort order" wording, and it matches current NuGet
> behavior, which "uses case insensitive string comparisons for pre-release components" (so `1.0.0-alpha` and
> `1.0.0-Alpha` are equal), per the
> [NuGet package versioning reference](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning).
> A proposal to switch NuGet to ordinal comparison (NuGet/Home#11621) was declined by the NuGet team as a
> breaking change. Documented at `ComparePreReleaseIdentifiers`. Covered by
> `With_PreRelease_Differing_Only_In_Casing_Returns_Zero`, `With_Different_PreRelease_Identifiers_Returns_Expected_Order`
> and `With_Resolved_Version_Differing_Only_In_Casing_Does_Not_Bump_Or_Save`.
>
> **Follow-up (2026-10-03): `GetHashCode` aligned with `CompareTo`.** Because `Equals` delegates to
> `CompareTo == 0`, and `CompareTo` has always compared numeric pre-release identifiers by value, versions such as
> `1.0.0-beta.01` and `1.0.0-beta.1` are equal but the original hash (case-insensitive over the raw label) produced
> different hashes — a broken equals/hash contract that would break `HashSet`/`Dictionary` lookups. `GetHashCode`
> now hashes each identifier using the same rules as `CompareTo` (numeric by parsed value, otherwise
> case-insensitively). Covered by `With_Numeric_Identifier_Leading_Zero_Returns_Zero_And_Same_Hash`. No production
> code currently keys a hash collection by `SemanticVersion`, so this was a latent contract fix.

**Files:** `PackageManifest.cs:28-29`, `PackageFileService.cs:439`, `BumpResult.cs:25`, `BumpReport.cs:65-66` vs. `Common/SemanticVersion.cs:176-231`

Change detection everywhere uses `StringComparison.OrdinalIgnoreCase`:

```csharp
public bool HasChanges => _packages.Any(package =>
    !string.Equals(package.Version, package.OriginalVersion, StringComparison.OrdinalIgnoreCase));
```

…while `SemanticVersion` ordering compares pre-release identifiers **ordinally** (per SemVer: ASCII sort, case-sensitive). A feed version that differs from the local version only by casing (e.g. local `1.0.0-Preview1` vs. feed `1.0.0-preview.1` won't apply, but `1.0.0-RC1` vs `1.0.0-rc1` would) can pass the `newVersion > reference` guard, call `SetVersion`, and then be classified as "no change" — so no file is written and the console prints "No package versions were bumped."

**Suggestion:** version strings should be compared ordinally end-to-end (they are semantically case-sensitive), i.e. drop `OrdinalIgnoreCase` from `HasChanges`, `WasBumped`, `Report.HasChanges`, and the skip check in `ApplyChanges`. If case-insensitive comparison is deliberate, document why next to the SemVer ordering code.

---

## Low severity

### L1. Validator credential-key casing is inconsistent with the resolver

> **Status (2026-10-03): resolved.**
>
> `NuGetConfigValidator` now compares `ClearTextPassword` with `StringComparison.OrdinalIgnoreCase`, matching the
> resolver's `NuGetClientConfig` lookup, so a key such as `cleartextPASSWORD` is accepted by both. Covered by
> `With_Mixed_Case_Credential_Keys_Returns_Empty_List`.

`NuGetConfigValidator.cs:64-65`:

```csharp
if (!cred.Key.Equals("UserName", StringComparison.OrdinalIgnoreCase) &&
    !cred.Key.Equals("ClearTextPassword"))   // ← default (ordinal, case-sensitive)
```

`NuGetClientConfig` looks up both keys with `OrdinalIgnoreCase`. So `key="cleartextpassword"` is *accepted* by the resolver but *rejected* by the validator — contradictory errors. Use `OrdinalIgnoreCase` for both comparisons.

### L2. `BumpPackagesSettings.Validate` can throw instead of returning an error

> **Status (2026-10-03): resolved.**
>
> Added `Common/PathValidation.TryGetFullPath`, which wraps `Path.GetFullPath` and returns `false` for invalid input
> (`ArgumentException`, `NotSupportedException`, `PathTooLongException`) instead of throwing. Both
> `BumpPackagesSettings` (`--config`, `--path`) and `BumpToolsSettings` (`--config`, `--manifest`) now return a
> `ValidationResult.Error` (`"The file/directory {path} is not a valid path."`). Covered by the invalid-path cases in
> `BumpPackagesSettingsTests` and `BumpToolsSettingsTests`.

`BumpPackagesSettings.cs:35`, `:45` — `Path.GetFullPath(...)` throws `ArgumentException` for invalid path characters rather than yielding a friendly `ValidationResult.Error`. Wrap in try/catch and convert to a validation message.

### L3. Hardcoded command name

`BumpPackagesCommand.cs:37-40`:

```csharp
if (context.Name != "packages")
{
    throw new DotBumpException($"Unsupported command name {context.Name}");
}
```

`CommandConfiguration.PackagesCommandName` already defines this literal. Reuse the constant (or reconsider the check itself — the command is only reachable through that registration, so the guard mostly duplicates Spectre's wiring).

### L4. Per-instance default path fields

`BumpPackagesCommand.cs:18-19` — `_defaultNugetConfigPath` / `_defaultRepositoryPath` are instance fields initialized from `Directory.GetCurrentDirectory()` at construction. They never vary per instance; make them `static readonly`, or compute them inside `ExecuteAsync` so they reflect the working directory at execution time.

### L5. Dictionary comparers don't match case-insensitive package-id identity

`BumpPackagesHandler.cs:75` (`referenceVersions`) and `PackageVersionResolver.cs:28` (`bestVersions`) use the default ordinal comparer, while package-id identity is case-insensitive everywhere else (`GetPackageIds` → `Distinct(OrdinalIgnoreCase)`, `SetVersion` → `OrdinalIgnoreCase`). Behavior is currently correct only because all keys originate from a single source (`GetPackageIds`). Use `StringComparer.OrdinalIgnoreCase` on both dictionaries for defense in depth.

### L6. `SemanticVersion` re-parsed on every property access

`PackageVersionEntry.cs:45` — `public SemanticVersion SemanticVersion => new(Version);` re-runs the compiled regex each time it is read; handler and report together parse every entry multiple times per run. Cache the parsed value (lazy field or computed once when the entry is created).

### L7. `PackageFileService` does too much

467 lines covering directory traversal, XML parsing, span computation, encoding detection, and writing. Consider splitting into focused types (e.g. `RepositoryScanner` for candidate-file discovery, `PackageVersionReader` for XML → entries, `VersionSpanRewriter` for applying edits). The existing tests map cleanly onto those units and would survive the split.

### L8. Handler validates `repositoryPath` but not `nugetConfigPath`

`BumpPackagesHandler.cs:24` guards `repositoryPath` only. A null/whitespace `nugetConfigPath` silently falls back to the default nuget.org feed inside `NuGetConfigFileService`. For an interface contract, validate both (`ArgumentException.ThrowIfNullOrWhiteSpace(nugetConfigPath)`).

### Nits

- `BumpPackagesCommand.cs:77-78` — expected domain failures (`DotBumpException`) go through `console.WriteException(...)`, printing a stack trace for what is often a user-fixable condition (bad config). Consider a short message for `DotBumpException` and reserve `WriteException` for unexpected errors.
- `NuGetClient.cs:128` — `_disposed` field is declared after the methods that use it; move it up with `_defaultOptions`.
- `BumpPackagesCommand.cs:67` / `WriteReportToConsole` — `Errors.Any()` is evaluated twice; trivial.

---

## Test review

Overall the test suite is a strong point: `PackageFileServiceTests` covers byte-for-byte preservation (single/double quotes, entities, XML declaration, CRLF and lone-CR line endings, whitespace-padded values, repeated versions, `Condition` attributes containing the same version), excluded directories, symlinks (with a sensible Windows fallback), and the handler covers no-downgrade, reference-version selection, validation short-circuit, and invalid-semver skipping. `PackageVersionResolverTests` covers pre-release preference and feed-order independence. Naming, structure, and AAA conventions match `test/DotBump.Tests/AGENTS.md`.

Gaps:

1. **BOM preservation on actual change.** Resolved: `With_Utf8_Bom_File_Modified_Preserves_Bom_And_Encoding`,
   `With_Utf16_Le_Bom_File_Modified_Preserves_Bom_And_Encoding`,
   `With_Utf16_Be_Bom_File_Modified_Preserves_Bom_And_Encoding` and
   `With_No_Bom_File_Modified_Does_Not_Add_Bom` write an encoded file with a real version bump and assert the preamble,
   encoding and content survive (`DetectEncoding` + `WriteText` path).
2. **Case-insensitive id unification.** Resolved at the service integration level: `With_Ids_Differing_Only_By_Case_Bumps_All_Occurrences_To_One_Target`
   (in `PackageFileServiceTests`) writes two `.csproj` files whose ids differ only by casing (`Newtonsoft.Json` /
   `newtonsoft.json`), asserts `GetPackageIds()` yields a single id, then bumps via `SetVersion` with yet another casing
   and asserts both files are rewritten to the single target version.
3. **Warnings not asserted as surfaced.** Resolved: warnings are asserted through to the report (`With_Read_Warnings_Reports_Warnings`, `With_Validation_Errors_Still_Reports_Read_Warnings`, `BumpReportTests`) and `With_Unsupported_Versions_Skips_And_Warns` now also asserts `LogEventLevel.Warning` output. No console assertion — the report is the intended surface (see M3).
4. **Markup-unsafe paths.** Resolved: `[draft]`-style markup-unsafe paths are exercised for all three commands (finding H1 resolved above).
5. **Span-mismatch guard.** The `ApplyChanges` invalid-span warning path is now covered by `With_Out_Of_Bounds_Span_Skips_And_Warns`; the span-content check itself is deliberately not added (see the H2 disposition above), so no content-mismatch test is warranted.
6. **Duplicated test helper.** Resolved: `CreateManifest` is now a single factory on `TestPackageManifestFactory`
   (`test/DotBump.Tests/TestHelpers/`), imported with `using static` in `BumpPackagesCommandTests` and
   `BumpPackagesHandlerTests`; the two copy-pasted private methods were removed.
7. **Shared static service.** `PackageFileServiceTests` uses a single `static readonly s_service`; safe under xUnit's per-class sequential execution, but an instance-per-test field would remove the implicit coupling.
8. **Save-time skip is not surfaced.** Resolved: `ApplyChanges` records the skipped bump via `manifest.AddWarning`, the handler propagates warnings to the report after saving, and `With_Unapplied_Span_Surfaces_Warning` passes (state is kept, the failure is reported).

---

## What's done well

- **Span-based rewrite instead of XML re-serialization** — preserving quoting, entities, layout, declaration, encoding, BOM, and line endings is exactly the right design for a tool that touches project files, and it is the best-tested part of the feature.
- **No-downgrade guard** (`BumpPackagesHandler.cs:49-53`) with a matching test — reference version is the highest current occurrence, so occurrences are never rolled back.
- **Validation before I/O** — NuGet config errors are reported through `ReportErrors` before any network call, and the command returns exit code `1` without saving.
- **Filesystem hygiene** — excluded directories, reparse-point skipping with graceful degradation on inaccessible entries, and candidate-file filtering.
- **Credential masking** — `[LogMasked]` on `NuGetClientCredential` and `Credential.Value`, wired through the single `LoggerConfigurator.CreateConfiguration` seam and pinned by tests (see M4).
- **Consistency with existing conventions** — file headers, file-scoped namespaces, primary constructors, Serilog message templates (no interpolation), `MethodStart`/`MethodReturn` tracing, `DotBumpException`, and the Pascal_Snake_Case test naming.

---

## Suggested priority

1. **H1** (markup escaping) and **H2** (span content verification) — small, self-contained, prevent user-visible failures and potential file corruption.
2. **M4** (log redaction) — small and security-adjacent.
3. **M1 + M2** (cancellation + bounded parallelism) — natural single change set.
4. **M3, M5, M6** — refactors that pay off as the feature grows past beta.
5. **M7** and the low-severity items — quick consistency passes; add the missing tests alongside.
