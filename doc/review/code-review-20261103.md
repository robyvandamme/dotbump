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

### 1. [Moderate] `PackageSourceMappingResolver` is hard-wired instead of injected — ✅ Fixed
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

**Status: fixed in `f74a948`.** Added `IPackageSourceMappingResolver`, implemented it on
`PackageSourceMappingResolver`, injected it into `PackageVersionResolver`, and registered the implementation in
`CommandConfiguration`.

### 2. [Minor] `MatchedPattern` only reports the first-encountered co-winning pattern — ✅ Fixed
`src/DotBump/NuGet/PackageSourceMappingResolver.cs:139-149`

When two patterns share the same prefix but differ after the wildcard (e.g., `Contoso.*` from `feed1` and
`Contoso.*Legacy` from `feed2`), source keys are correctly merged, but `winningPattern` retains the first pattern
processed. The reported `MatchedPattern` (`"Contoso.*Legacy"`, `"Contoso.*"`, depending on iteration order) can
therefore be misleading when surfaced in debug logs.

This is cosmetic — the matching behavior itself is correct because two equal-length prefixes that both match the same
package id must be the identical prefix string. Options: report the prefix (e.g., `"Contoso."`) instead of the pattern,
or document that `MatchedPattern` is representative. Not worth complexity given the meta-character nuance is already
documented in a comment.

**Status: fixed in `2ffd573`.** `MatchedPattern` now reports the exact package id or the prefix followed by `*`, so
co-winning patterns render deterministically. Covered by
`With_Different_Patterns_Sharing_Longest_Prefix_Returns_All_Sources`.

### 3. [Minor] Unmapped packages produce one warning per package — ✅ Accepted
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

**Status: accepted.** One warning per unmapped package remains intentional: the messages are deterministic,
deduplicated per package id, and tell the user exactly which packages are skipped. A cap or summary can be added later
if real reports become noisy.

### 4. [Minor] Deferred enumerable passed to the logger — ✅ Fixed
`src/DotBump/NuGet/PackageVersionResolver.cs:61-66`

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

**Status: fixed in `24c66b8`.** The mapped source keys are materialized with `ToArray()` before being passed to
Serilog.

### 5. [Minor] Test coverage gaps — ✅ Fixed

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

**Status: fixed in `c41874f`.** Added coverage for exact-pattern ties across sources, duplicate `<packageSource>` keys
in the parser, mixed defined/undefined source keys, and null arguments to the resolver.

### 6. [Nit] Misleading helper name — ✅ Fixed
`test/DotBump.Tests/NuGet/PackageVersionResolverTests.cs`

`CreateConfig` now just forwards to `CreateConfigWithMapping`, even when no mappings are passed. Consider renaming the
combined helper (e.g., `CreateNuGetConfig(... mappings = [])`) and deleting the shim, so the factory name does not imply
a mapping was configured.

**Status: fixed in `3b9dc74`.** `CreateConfigWithMapping` was removed and `CreateConfig` now accepts optional mappings
directly.

### 7. [Nit] Test name underpromises — ✅ Fixed
`test/DotBump.Tests/NuGet/PackageSourceMappingResolverTests.cs:152`

`With_Unknown_Source_Key_Returns_Warning` asserts two distinct outcomes (the unknown-source-key warning *and* the
unmapped-package warning for `ghost.package`). Per the repo's "one logical outcome per test" guideline, consider
splitting it, or renaming to reflect that both warnings are exercised.

**Status: fixed in `c72cb1e`.** The test was split into
`With_Unknown_Source_Key_Returns_Unknown_Source_Warning` and
`With_Package_Mapped_Only_To_Unknown_Source_Returns_Undefined_Source_Warning`.

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

Small, cohesive feature with strong test coverage and careful attention to NuGet-compatible edge semantics. All
findings are now addressed or explicitly accepted: finding 1 via dependency injection (`f74a948`), the pattern
reporting and logging nits (`2ffd573`, `24c66b8`), the coverage gaps (`c41874f`), and the test cleanups (`3b9dc74`,
`c72cb1e`). Finding 3 is accepted as intentional behaviour. The branch is ready to merge.
