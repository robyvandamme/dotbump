# Code Review: `language` Command + SDK Alignment Refactor

**Date:** 2026-10-24
**Scope:** branch `bump-language` vs `main` — 3 commits (`f6bffd3 feat: add C# language version bump support`, `47be2de refactor: introduce shared SDK alignment base logic`, `fdf949b refactor: centralize option constants`). New `Commands/BumpLanguage/*`, new `SdkAlignmentCommand`/`SdkAlignmentSettings`/`BumpPathSettings` bases, `BumpReport`/markdown/CI wiring, and the new tests under `test/DotBump.Tests/Commands/BumpLanguage/` plus `Reports` test additions.
**Reviewer role:** .NET Tech Lead (code-only review; no code executed).

---

## Summary

Solid, well-scoped feature. The refactoring commits are the strongest part of the branch: `SdkAlignmentCommand<TSettings>` turns the previously copy-pasted `ExecuteAsync` flow in `BumpFrameworksCommand` into a clean template-method base, and `BumpPathSettings`/`SdkAlignmentSettings` hoist duplicated options and validation out of the settings classes. The new language command follows the established patterns (file service + handler + manifest model, exceptions as `DotBumpException`, warnings surfaced into the report, span-based edits that preserve formatting/BOM), and it is thoroughly tested — including the tricky cases (comment-split values, multiple `<LangVersion>` elements, BOM preservation, legacy SDK guard).

No critical or high-severity issues. Two medium-severity maintainability findings (duplicated version-parsing logic and a large near-copy of the MSBuild edit machinery), plus several low-severity hardening and consistency items.

| # | Severity | Finding | Status |
|---|----------|---------|--------|
| M1 | Medium | Version-number parsing exists in two mechanisms (handler text parser vs service regex) — single source of truth needed | Resolved |
| M2 | Medium | `LanguageFileService` / `LanguageManifest` / `LanguageEntry` largely duplicating the framework file service machinery | Accepted (deferred) |
| L1 | Low | `language` command knowledge duplicated in 4 places, mapping rule ("major + 4") drifts from hardcoded report name | Resolved |
| L2 | Low | `TryParseVersion` uses `System.Version` as an int-pair comparator; two allocations and heavier than needed | Resolved with M1 |
| L3 | Low | `SaveLanguageManifest` re-reads the file for encoding only; offsets applied to a snapshot can silently corrupt a concurrently changed file | Open |
| L4 | Low | Case-insensitive "did it change" comparisons copied from frameworks where they are meaningful; numeric versions have no case | Open |
| L5 | Low | CI language step: speculative `if [ -d ./test/DotBump.Tests/Data ]` guard around `--exclude` | Open |
| T1 | Low | Test gap: non-numeric entry reaching the handler is never asserted to be left unchanged | Resolved structurally |
| T2 | Low | Test gap: `SaveLanguageManifest` skip paths (unregistered file text) uncovered | Open |

### Resolution update (2026-10-25)

- **M1 is resolved.** Parsing is now owned by the `LanguageVersion` value type. `LanguageFileService` parses each
  `<LangVersion>` once and stores the result on `LanguageEntry.ParsedVersion`; `BumpLanguageHandler` only compares
  parsed values. `TryParseVersion` and the separate file-service regex were removed.
- **L2 is resolved with M1.** `System.Version` is no longer used; `LanguageVersion` provides the comparison and
  invariant formatting directly.
- **T1 is resolved structurally.** Non-numeric values are rejected by `LanguageVersion.TryParse` before an entry is
  created, so the handler cannot receive one.
- **L1 is resolved across all commands.** Command names now come from `BumpCommandNames` and are reused by command
  configuration, command classes and report constructors. For the language command, the mapping rule now lives in
  `LanguageVersion.SdkMajorOffset`/`FromSdkMajor`, and the command description uses the same offset.
- **M2 is accepted for now.** The duplication is tolerable for two related implementations, and extracting a
  generic `TManifest`/`TEntry` base now would couple them prematurely. Revisit if a third related value-bump command
  is introduced; prefer domain-agnostic helpers (XML occurrence scanner, span edit applier, file-text registry) over
  a generic base.
- **L3, L4, L5 and T2 remain open.**
- Resolution verification: **522 tests passed**; Release build completed with **0 warnings and 0 errors**.

---

## Medium severity

### M1. Version parsing lives in two components with independent rules

**Status: Resolved.** Parsing now lives in `LanguageVersion.TryParse`; `LanguageFileService` stores
`ParsedVersion` on `LanguageEntry`, and `BumpLanguageHandler` compares parsed values only. `TryParseVersion` and the
separate file-service regex were removed.

**Files:** `src/DotBump/Commands/BumpLanguage/DataModel/LanguageVersion.cs:18-44`,
`src/DotBump/Commands/BumpLanguage/DataModel/LanguageEntry.cs:45`,
`src/DotBump/Commands/BumpLanguage/LanguageFileService.cs:125-144`,
`src/DotBump/Commands/BumpLanguage/BumpLanguageHandler.cs:44-57`

The file service decides *what counts as a version* with a regex:

```csharp
private static readonly Regex s_languageVersionPattern = new(
    @"^(?<major>\d+)(?:\.(?<minor>\d+))?$",
    RegexOptions.Compiled,
    TimeSpan.FromMilliseconds(100));
```

...while the handler re-parses the *same* values with a completely different mechanism (`TryParseVersion`, splitting on `'.'` with `int.TryParse`):

```csharp
var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries);
if (parts.Length is < 1 or > 2
    || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major)) ...
```

Two side effects:

1. **Divergence risk.** The definition of "numeric language version" now lives in two places with subtly different rules. Today's public rules coincide (the handler never sees a value the service rejected via `RemoveEmptyEntries`), but any future change (e.g. allowing `preview`-style suffixes, or 3-part values like `12.0.0`) must be made twice, and a mistake here silently skips entries instead of erroring. The invariant "everything in `LanguageManifest.Entries` is parseable by `TryParseVersion`" is implicit and untested.
2. **Duplicated concept.** Even where they coincide, the handler re-parses strings the service already parsed/validated once.

**Suggestion:** parse once, in the file service, and carry the parsed value on `LanguageEntry` (e.g. `public Version? ParsedVersion` or two `int` properties `Major`/`Minor`). The handler then compares without parsing:

```csharp
foreach (var entry in manifest.Entries.Where(e => e.ParsedVersion != null))
{
    if (entry.ParsedVersion!.Value.CompareTo(targetVersion) < 0)
    {
        manifest.SetVersion(entry, targetLanguageVersion);
    }
}
```

That removes `TryParseVersion` entirely and makes the "entries are always numeric" invariant explicit in the model.

### M2. `LanguageFileService` is a ~90% copy of `FrameworkFileService`

**Status: Accepted (deferred).** The duplication is intentionally accepted while there are only two related
implementations. Revisit when a third value-bump command is introduced; at that point, extract domain-agnostic
mechanics by composition rather than introducing a generic manifest/entry base.

**Files:** `src/DotBump/Commands/BumpLanguage/LanguageFileService.cs` vs `src/DotBump/Commands/BumpFrameworks/FrameworkFileService.cs`

Compare these members:

| Member | Framework | Language | Delta |
|--------|-----------|----------|-------|
| `GetLanguageManifest` / `GetFrameworkManifest` | root validation, scanner, per-file call | identical | element name only |
| `SaveFrameworkManifest` / `SaveLanguageManifest` | changed-file loop, apply, write | identical | log message |
| `IsCandidateFile` | value-filter predicate | identical | extensions list |
| `GetRelativePath` | identical | identical | none |
| `ReadTargetFrameworks` / `ReadLanguageVersions` | span calc, empty-value skip, non-numeric skip, `DotBumpException` wrap, warning + `RegisterFileText` | identical | filter logic |
| `ApplyChanges` | reverse-order `Remove`+`Insert` span edits | identical | property names |
| `FrameworkManifest` / `LanguageManifest` | warning list, changed files, file-text registry, `Add`/`SetVersion` | identical | entry type |
| `FrameworkEntry` / `LanguageEntry` | id/file/original/version/spans | identical | `Suffix` |

That is roughly 140 duplicated lines (x2 including manifests), and the domain-model classes (`LanguageManifest`, `LanguageEntry`, `BumpReport(LanguageManifest, ...)`) repeat the framework trio too. A third command version-bumping an MSBuild value is plausibly not the last, and every future fix (e.g. L3's stale-snapshot issue) must be applied in both copies.

**Suggestion:** extract the shared machinery once — e.g. a generic `MsBuildValueFileService<TManifest, TEntry>`-style base (or composition: a `SpanEditApplier` + `ManifestTextRegistry` helper in `Common/`) parameterized on element name, candidate-file predicate, value-filter, and entry factory. The alternative cheap version: at minimum, extract the shared file-level operations (`ReadXmlDocumentFromFile`, `GetRelativePath`, text-registry behavior, reverse-order span edits) into `Common/XmlFileText`-adjacent helpers, which is a much smaller change and removes the riskiest duplicated logic (the span-editing invariant currently has well-tested edge cases in the language copy only).

---

## Low severity

### L1. `language` command identity is hardcoded in four places

**Status: Resolved across all commands.** `BumpCommandNames` is now the single source for command names used by
command configuration, command classes and report constructors. The language mapping rule is centralized in
`LanguageVersion.SdkMajorOffset`/`FromSdkMajor` and reused by the command description.

**Files:** `src/DotBump/CommandConfiguration.cs:27,136-152`, `src/DotBump/Commands/BumpLanguage/BumpLanguageCommand.cs:17`, `src/DotBump/Commands/BumpLanguage/BumpLanguageHandler.cs:64`, `src/DotBump/Reports/BumpReport.cs:78`

- `CommandConfiguration.LanguageCommandName = "language"`
- `BumpLanguageCommand` passes `commandName: "language"` (and it is validated in the base `ExecuteAsync` against `context.Name`)
- `BumpReport(LanguageManifest, ...)` hardcodes `CommandName = "language"`
- The derived rule "SDK major + 4" is in `BumpLanguageHandler` (`LanguageVersionMajorOffset = 4`) but the same rule is also spelled out in the command *description string* in `CommandConfiguration`.

A rename or a change to the mapping rule requires touching all of them, and nothing keeps them aligned (the base command validates `context.Name`, but the report name is not derived from it). Practical fix: have `SdkAlignmentCommand` pass its `commandName` into the report (e.g. the `BumpReport` ctor accepts a command name or a `CommandId` enum), and derive the description from a single source (or at least add a comment cross-referencing `LanguageVersionMajorOffset` to the command description).

### L2. `TryParseVersion` leans on `System.Version` unnecessarily

**Status: Resolved with M1.** `System.Version` and `TryParseVersion` were removed; comparisons now use the
`LanguageVersion` value type.

**Files:** `src/DotBump/Commands/BumpLanguage/BumpLanguageHandler.cs:44-47`,
`src/DotBump/Commands/BumpLanguage/DataModel/LanguageVersion.cs:46-49`

```csharp
var targetVersion = new Version(
    currentSdk.SemanticVersion.Major + LanguageVersionMajorOffset,
    0);
```

`System.Version` is fine here and the comparison semantics are correct for `major(.minor)` values, but it also allocates twice (`version = new Version(0,0)` then rebuilt) and drags `Build`/`Revision` semantics into play without meaning. If the parsing is centralized per M1, a cheap `(int major, int minor)` tuple with `Ordinal`-free comparison is simpler. Not blocking — just a simplification that falls out of M1.

### L3. `SaveLanguageManifest` applies offsets to a stale snapshot and pays a second file read

**Files:** `src/DotBump/Commands/BumpLanguage/LanguageFileService.cs:53-73` (+ same shape in `FrameworkFileService.cs:54-74`)

The flow is: `GetLanguageManifest` reads bytes → strips BOM → caches text + span offsets; `SaveLanguageManifest` → re-reads the file **only to re-detect the encoding** → applies edits to the *cached* text → `WriteText` re-reads bytes a third time for encoding. Between read and write, if the file changes on disk (another tool, a concurrent bump run), the cached offsets no longer describe the fresh content. The guard in `ApplyChanges` (`entry.VersionStart + entry.VersionLength > text.Length`) validates against the cached text, so it cannot catch this; the write can then splice a version into the wrong place or throw mid-file (leaving a partial state) — corrupting a project file that `dotbump` never bumped.

For a short-lived CLI run this risk is small, but the mitigation is almost free: before inserting, assert the span still contains what was recorded:

```csharp
var originalSpan = text.Substring(edit.Start, edit.Length).Trim();
if (!string.Equals(originalSpan, entry.OriginalVersion, StringComparison.Ordinal))
{
    manifest.AddWarning($"Skipping language version in '{entry.Id}' because the file changed since it was read.");
    continue;
}
```

(Also consider merging the encoding detection: `WriteText` already re-detects, so the loop-level `ReadText` at save time is otherwise unused — one read per saved file, not two.)

### L4. Case-insensitive comparisons for numeric versions

**Files:** `src/DotBump/Commands/BumpLanguage/LanguageManifest.cs:28-29,45`, `src/DotBump/Commands/BumpLanguage/LanguageFileService.cs:167`

The framework code needs `OrdinalIgnoreCase` because `Net8.0` and `net8.0` are legitimately different spellings. Language versions (`12.0` vs `12.0`) are numeric and have no case, so the copied comparisons are dead flexibility — they cannot change behavior, but they imply a semantics that doesn't exist and make grep/reading harder. Use `StringComparison.Ordinal` in the language code, or extract a shared manifest so the comparison policy is set by the domain rather than copy-pasted per flavor.

### L5. CI language step: speculative directory guard

**Files:** `.github/workflows/dependency-update-major.yml:60-67`

```yaml
- name: Bump language version
  run: |
    exclude_args=()
    if [ -d ./test/DotBump.Tests/Data ]; then
      exclude_args+=(--exclude ./test/DotBump.Tests/Data)
    fi
    dotnet dotbump language \ ...
```

`Data/Packages/Sample.csproj` contains no `<LangVersion>`, so for the language step this exclusion is currently protective in theory only, and the `[ -d ... ]` guard cannot be false on a normal repo checkout (the data directory is committed). The packages step needs it (its `Sample.csproj` has `Version` attributes that must not be touched); the language step only scans `.csproj`/`Directory.Build.*`, of which only `Sample.csproj` exists in `Data/`. Keep the *pattern* consistent with the packages step if you want, but an unconditional `--exclude ./test/DotBump.Tests/Data` is honest, simpler, and stays safe even if a future test fixture gains a numeric `<LangVersion>`.

---

## Test review

Overall quality is high: names follow the `With_{Condition}_Returns_{Outcome}` convention, AAA is respected, disk work is isolated in `LocalDirectory("./temp/...")` with proper reset, mocks are `Mock.Of`/loose with `Verify` for interactions (`Times.Once` / `Times.Never` on `SaveLanguageManifest` is exactly right), and the command tests use `TestConsole` + `CommandContext` per the canonical example. The comment-split-value test (`With_Comment_Split_LangVersion_Skips_Entry_And_Adds_Warning`, `LanguageFileServiceTests.cs:121-139`) asserting *both* empties and a warning plus untouched file content is particularly good coverage of the `XmlFileText` span machinery.

Gaps worth closing:

### T1. Non-numeric entries reaching the handler are never exercised

**Status: Resolved structurally.** Non-numeric values are rejected by `LanguageVersion.TryParse` before an entry is
created, so a non-numeric entry cannot reach the handler.

**Files:** `test/DotBump.Tests/Commands/BumpLanguage/LanguageVersionTests.cs`

All handler tests use parseable versions. Because the service filters them upstream today, the only ways a non-numeric value reaches `TryParseVersion` are a mocked manifest (test) or future code change (M1). One cheap theory/handlers-level test locks the contract in:

```csharp
[Fact]
public async Task With_Non_Numeric_Entry_Leaves_It_Unchanged()
{
    var manifest = CreateManifest("latest");
    var (handler, fileService, _) = CreateHandler(manifest, "10.0.100");

    var report = await handler.HandleAsync("./repo", "./global.json");

    report.HasChanges.ShouldBeFalse();
    manifest.Entries.Single().ShouldSatisfyAllConditions(
        () => entry.OriginalVersion.ShouldBe("latest"),
        () => entry.Version.ShouldBe("latest"));
    fileService.Verify(s => s.SaveLanguageManifest(It.IsAny<LanguageManifest>()), Times.Never);
}
```

### T2. `SaveLanguageManifest` defensive branches uncovered

`LanguageFileService.cs:60-65` (`GetFileText` returns `null` → skip) and `ApplyChanges`'s invalid-span guard (`LanguageFileService.cs:172-181`) are only guarded by construction today. If M2's shared machinery materializes, these become the *only* failure paths of a bookkeeping rewrite, so one test each — a manifest whose `ChangedFiles` contains an unregistered file and a manifest whose entry was constructed with a span outside the cached text — prevents a refactoring regression from becoming a silent skip.

Minor test nits (non-blocking):

- `BumpLanguageSettingsTests.With_Existing_GlobalJson_File_Returns_Success` builds `globalJsonPath` before creating the file and then calls `TempDirectory.EnsureFileCreated("global.json")`, which recreates it separately; wiring the two steps together would make the arrangement less confusing.
- `LanguageFileServiceTests` writes files via `File.WriteAllText` without a cleanup equivalent to `EnsureFileDeleted`; the directory-level reset in `ResetTempDirectory` covers it, so this is only worth a comment if the team purges `./temp` outside the test run.

---

## What was done well

- **`SdkAlignmentCommand<TSettings>` template-method extraction** removes ~90 duplicated lines from `BumpFrameworksCommand` and gives both commands one tested flow (guard clauses, entity-name-parameterized console output, report writing, error exit codes). Marking it `sealed override` at both call sites is correct.
- **The span-edit approach** (offsets from `IXmlLineInfo` + raw text, .NET 10 format-safe `Remove`+`Insert` in reverse order, value-only rewrite) preserves comments, whitespace, and BOM exactly; the corresponding tests (comment-split value, multiple elements, BOM bytes) validate the risky invariants.
- **Warnings as first-class data** (`manifest.AddWarning` → `bumpReport.ReportWarnings` → markdown `### Warnings`) means partially-skipped files are visible to the human reviewing the PR instead of only in verbose logs.
- **Fail-fast SDK guards** (invalid semantic version, major < 5) run before any file scanning (`With_Legacy_Sdk_Version_Throws...` verifies zero side effects).
- **Regex hardening** (`Compiled` + 100 ms timeout, `s_` naming) and `NumberStyles.None` + `InvariantCulture` parsing follow the project's standards.
- **Appreciating the empty-commit refactor** (`fdf949b` centralizing option name constants) pays for itself in `WithExample` consistency — small, but exactly the kind of change that keeps future copy-paste drift down.

## Verdict

Approve: M1 (single parser), L1 (command identity across all commands), L2 and T1 are resolved. M2 is accepted and
deferred; revisit it if a third related value-bump command is introduced. The remaining low-severity items are
independent polish.
