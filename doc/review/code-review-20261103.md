# Code Review — 2026-11-03

- **Repository:** dotbump
- **Branch:** `package-source-mapping` (vs `master` @ `71f53f9`)
- **Commits reviewed:** `29d47cf` (feat: honor packageSourceMapping when bumping packages and tools), `9eb62de` (config:
  add packageSourceMapping to nuget.config)
- **Reviewer:** .NET Tech Lead

## Scope of Changes

Adds `packageSourceMapping` support to the `bumpPackages` and `bumpTools` commands:

- `NuGetConfigFileService` parses the `packageSourceMapping` section into a new `PackageSourceMappingEntry` model.
- New `PackageSourceMappingResolver` selects the package sources a package may be queried against, following NuGet's
  precedence rules (exact id > longest prefix > `*`).
- `IPackageVersionResolver.ResolveAsync` now returns a `PackageResolutionResult` (versions + warnings) instead of a bare
  dictionary, and only queries sources the package is mapped to.
- `BumpPackagesHandler` / `BumpToolsHandler` pipe resolver warnings into their reports.
- Tests added at four levels: config parsing, mapping resolution, resolver integration, and handler warning reporting.

## Overall Assessment

A well-executed feature. The design keeps user-correctable configuration issues (unknown source keys, unmapped packages)
as warnings rather than exceptions, which is the right call for a CLI tool. The precedence logic in
`PackageSourceMappingResolver` correctly mirrors NuGet's behavior, including the subtle "only the part before the first
`*` participates in the match" rule, which is documented with a comment. Backward compatibility is preserved: when no
mapping section exists, every package is queried against all sources exactly as before.

Conventions are followed throughout (file headers, file-scoped namespaces, Serilog message templates without
interpolation, `MethodStart`/`MethodReturn` tracing, test structure mirroring production layout). The interface change
is clean; all call sites and mocks were updated, and the new warning flow is covered both ways
(`BumpPackagesHandlerTests.With_Resolver_Warnings_Reports_Warnings`,
`BumpToolsHandlerTests.With_Resolver_Warnings_Reports_Warnings`).

I verified that mapping warnings pushed into the `PackageManifest` remain in-memory only (`PackageManifest._warnings` is
never serialized), so there is no risk of warnings accumulating in the manifest file across runs.

Findings below contain no blockers.

## Findings

### 1. [Moderate] `PackageSourceMappingResolver` is hard-wired instead of injected —
`src/DotBump/NuGet/PackageVersionResolver.cs:18`

```csharp
private readonly PackageSourceMappingResolver _packageSourceMappingResolver = new();
```

`PackageVersionResolver` instantiates its collaborator directly. Everything else in the NuGet pipeline is exposed via an
interface in `NuGet/Interfaces/` and injected through the primary constructor (`INuGetClientFactory`,
`INuGetReleaseFinder`, `INuGetConfigFileService`, ...). This is a small SOLID/DIP deviation:

- The mapping-selection behavior cannot be stubbed, so tests of `PackageVersionResolver` must model mapping rules
  through config data rather than testing both concerns in isolation.
- If NuGet mapping semantics evolve (e.g., suffix validation), the resolver cannot be swapped without recompilation.

**Suggestion:** make it an injectable dependency, either as an interface (`IPackageSourceMappingResolver`) or directly
as a concrete parameter of the primary constructor:

```csharp
internal class PackageVersionResolver(
    IPackageSourceMappingResolver packageSourceMappingResolver,
    INuGetClientFactory nuGetClientFactory,
    INuGetReleaseFinder nuGetReleaseFinder,
    ILogger logger) : IPackageVersionResolver
```

### 2. [Minor] `MatchedPattern` only reports the first-encountered co-winning pattern —
`src/DotBump/NuGet/PackageSourceMappingResolver.cs:139-149`

When two patterns share the same prefix but differ after the wildcard (e.g., `Contoso.*` from `feed1` and
`Contoso.*Legacy` from `feed2`), source keys are correctly merged, but `winningPattern` retains the first pattern
processed. The reported `MatchedPattern` (`"Contoso.*Legacy"`, `"Contoso.*"`, depending on iteration order) can
therefore be misleading when surfaced in debug logs.

This is cosmetic — the matching behavior itself is correct because two equal-length prefixes that both match the same
package id must be the identical prefix string. Options: report the prefix (e.g., `"Contoso."`) instead of the pattern,
or document that `MatchedPattern` is representative. Not worth complexity given the meta-character nuance is already
documented in a comment.

### 3. [Minor] Unmapped packages produce one warning per package —
`src/DotBump/NuGet/PackageSourceMappingResolver.cs:65-67`

Suggested tweak. For a repository with many packages that fail to match (e.g., after locking a config down to a specific
prefix), the report can be flooded with near-identical warnings:

```text
Package 'Serilog' does not match any packageSourceMapping pattern; skipping it.
Package 'Serilog.Exceptions' does not match any packageSourceMapping pattern; skipping it.
...
```

Consider collapsing these into a single warning per unmatched prefix or a summary form once a threshold is exceeded. Low
priority; current behavior is at least deterministic and deduplicated at the unknown-source-key level
(`reportedUnknownSourceKeys`).

### 4. [Minor] Deferred enumerable passed to the logger — `src/DotBump/NuGet/PackageVersionResolver.cs:61-66`

```csharp
logger.Debug(
    "Package {PackageId} is mapped to package source(s) {PackageSources} by pattern {Pattern}",
    package.PackageId,
    match.Sources.Select(o => o.Key),
    match.MatchedPattern);
```

Serilog will enumerate the LINQ projection during rendering. It is safe here (fresh list, no mutation before logging),
but materializing makes the intent explicit and renders identically on every sink:

```csharp
match.Sources.Select(o => o.Key).ToArray(),
```

Nit-level; apply only if you touch the method for finding 1.

### 5. [Minor] Test coverage gaps

Good baseline coverage exists, but a few paths are untested:

1. **Exact pattern tie across multiple sources.** `With_Same_Pattern_On_Multiple_Sources_Returns_All_Sources` covers a
   prefix (`Contoso.*`) tie, but an exact-id pattern declared on two sources is not covered in
   `PackageSourceMappingResolverTests`.
2. **Duplicate `<packageSource key>` merge in the file service.** The merge branch in `NuGetConfigFileService` (two
   `<packageSource>` blocks with the same key) is only exercised indirectly —
   `PackageSourceMappingResolverTests.With_Duplicate_Source_Key_Entries_Merges_Patterns` builds the data model by hand
   and bypasses the XML parsing. A `NuGetConfigFileServiceTests` case with two mapping blocks for the same key would
   close the loop between parser and model.
3. **Mixed source availability.** A package mapped to a combination of defined and undefined source keys receives only
   the defined ones (no skip); this behavior is not asserted anywhere.
4. **Null-argument tests.** `PackageSourceMappingResolver.Resolve` guards both parameters with
   `ArgumentNullException.ThrowIfNull`, consistent with the rest of the codebase, but there are no
   `With_Null_Input_Throws_ArgumentNullException` tests for it.

### 6. [Nit] Misleading helper name — `test/DotBump.Tests/NuGet/PackageVersionResolverTests.cs`

`CreateConfig` now just forwards to `CreateConfigWithMapping`, even when no mappings are passed. Consider renaming the
combined helper (e.g., `CreateNuGetConfig(... mappings = [])`) and deleting the shim, so the factory name does not imply
a mapping was configured.

### 7. [Nit] Test name underpromises — `test/DotBump.Tests/NuGet/PackageSourceMappingResolverTests.cs:152`

`With_Unknown_Source_Key_Returns_Warning` asserts two distinct outcomes (the unknown-source-key warning *and* the
unmapped-package warning for `ghost.package`). Per the repo's "one logical outcome per test" guideline, consider
splitting it, or renaming to reflect that both warnings are exercised.

## Verified Non-Issues (checked, no action needed)

- **Early-return ordering** (`mappingResult` computed before the `packageList.Count == 0` check in
  `PackageVersionResolver`) is intentional — it lets unknown-source-key warnings surface even with an empty package
  list, and there is a test pinning that behavior
  (`With_Empty_Package_List_And_Unknown_Mapping_Source_Returns_Warning`).
- **Unmapped packages are skipped with a warning rather than failing** (NuGet restore would hard-fail). A sensible
  deviation for an advisory tool, and the warning is propagated to both handlers.
- **Case handling** is consistent throughout: package ids, patterns, and source keys are all compared OrdinalIgnoreCase,
  and `packagesBySource` keys are normalized the same way.
- **Warning persistence** is in-memory only; no risk of reports/manifests accumulating warnings across runs.
- **`<clear />` inside `packageSourceMapping`** is ignored, which is correct for a tool reading a single config file
  rather than NuGet's config-hierarchy merge.
- **No security concerns**: the parser does not expand credentials, does not follow external references, and treats
  config content as trusted input from the repository being operated on.

## Conclusion

Small, cohesive feature with strong test coverage and careful attention to NuGet-compatible edge semantics. The only
finding I would ask to be addressed before merge is the injectability of `PackageSourceMappingResolver` (finding 1); the
remaining items are polish. No findings discovered that block the branch.
