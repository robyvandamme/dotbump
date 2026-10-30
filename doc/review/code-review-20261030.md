# Code Review — branch `multi-tfm-packages` (2026-10-30)

**Scope**: All C# source and test changes on `multi-tfm-packages` vs `main` (commit `04cf9ff`), ~670 additions / 100 deletions across 21 files. Markdown/documentation changes were excluded per the review instructions.

**Change summary**: Package bumping previously treated all occurrences of a package id as a single unit keyed only by package id, so conditional package references (e.g. per-TFM `PackageReference`s for `Microsoft.AspNetCore.OpenApi`) were collapsed to one reference version and bumped together to a single resolved version. This change introduces the concept of a *package variant* (same id + same effective MSBuild condition), keys version resolution by `(packageId, currentVersion)` (`PackageToBump` record instead of a string key), threads the effective condition through the file service/manifest/report, and surfaces the condition in JSON, markdown, and console reports.

---

## Overall Assessment

This is a well-structured change. The core design decisions are sound:

- Using `PackageToBump` (id + resolved version) as the resolver's dictionary key correctly deduplicates variants of the same package that share the same highest current version, while still resolving distinct versions independently. The `SemanticVersion` record equality (via `CompareTo`, case-insensitive id) makes this work without custom comparers.
- The "effective condition" is treated purely as a grouping key and is never evaluated or written back — only version spans are rewritten, so there is no risk of corrupting files with the synthesized `" && "` string.
- Skip-warning deduplication via `skippedPackageIds` correctly prevents the new per-variant iteration from emitting duplicate `--skip-package` warnings (a real regression risk this change avoided).
- Test coverage is strong and follows the repo's AAA/Pascal_Snake_Case conventions: resolver dedupe, per-variant handler resolution, conditional file read/write round-trips, report serialization with/without condition, and markdown formatting.

Findings below are mostly quality/clarity suggestions; no blocking correctness issues were identified.

---

## Findings

### 1. `PackageManifest.GetVariants()` — fragile "not found" idiom and O(n²) grouping *(Quality · Medium)* — ✅ Fixed

`src/DotBump/Commands/BumpPackages/DataModel/PackageManifest.cs:50-73`

The grouping uses `List.FirstOfDefault` on a value tuple and detects "no match" via `variant.Occurrences == null`:

```csharp
var variant = variants.FirstOrDefault(candidate =>
    string.Equals(candidate.PackageId, package.PackageId, StringComparison.OrdinalIgnoreCase)
    && string.Equals(candidate.Condition, condition, StringComparison.Ordinal));

if (variant.Occurrences == null)
{
    variant = (package.PackageId, condition, []);
    variants.Add(variant);
}
```

Problems:

- `variant.Occurrences == null` relies on the *default value* of a tuple containing a reference type. It works today, but it is an implicit sentinel: any refactor that changes `Occurrences` to a struct type, a non-null-default, or adds a check on `PackageId` instead will quietly break the grouping. It also defeats IDE/compiler intent analysis ("possible null check on non-nullable tuple member").
- The linear scan makes grouping O(n²) in the number of package entries. Fine for small projects, but every `.csproj` in a monorepo is in one manifest.

Suggested refactor — dictionary with a composite key:

```csharp
public IReadOnlyList<PackageVariant> GetVariants()
{
    var variants = new Dictionary<(string PackageId, string? Condition), List<PackageVersionEntry>>();

    foreach (var package in _packages)
    {
        var condition = string.IsNullOrWhiteSpace(package.Condition) ? null : package.Condition;
        var key = (package.PackageId, condition);

        if (!variants.TryGetValue(key, out var occurrences))
        {
            occurrences = [];
            variants[key] = occurrences;
        }

        occurrences.Add(package);
    }

    return variants
        .Select(kvp => new PackageVariant(kvp.Key.PackageId, kvp.Key.Condition, kvp.Value))
        .ToList();
}
```

Note this changes id matching from `OrdinalIgnoreCase` to the default ordinal tuple comparison; if case-insensitive id grouping must be preserved, key on `package.PackageId` normalized (e.g. `ToUpperInvariant`) or keep an explicit comparer.

**Status: fixed in `c50d747`.** `GetVariants()` now keeps the ordered variant list and uses a
`Dictionary<(string PackageId, string? Condition), int>` index with an explicit comparer that preserves
`OrdinalIgnoreCase` package-id matching and `Ordinal` condition matching. This removes the default-tuple sentinel and
makes grouping linear. Verified by `With_Multiple_Variants_Returns_First_Seen_Order`.

### 2. Duplicated "highest valid semantic version" logic *(Duplication · Medium-Low)* — ✅ Fixed

Two near-identical implementations of "pick the highest valid semantic version across occurrences" now live in:

- `BumpPackagesHandler.GetPackagesToBump` (`OrderByDescending(...).First()` over valid versions, skip entirely when none valid) — `src/DotBump/Commands/BumpPackages/BumpPackagesHandler.cs:108-126`
- `BumpReport.GetReferenceVersion` (same selection, but falls back to `entries[0]`'s version for display) — `src/DotBump/Reports/BumpReport.cs:237-252`

The fallback semantics genuinely differ (handler skips the variant, report still shows a version), which is easy to miss when one of the two is later changed. Consider moving the invariant onto `PackageVariant`, e.g.:

```csharp
// PackageVariant
public IReadOnlyList<SemanticVersion> ValidVersions =>
    Occurrences.Select(o => o.SemanticVersion).Where(v => v.IsValid).ToList();

public SemanticVersion? HighestVersion => ValidVersions.Count > 0
    ? ValidVersions.OrderByDescending(v => v).First()
    : null;
```

The handler then reads `variant.HighestVersion` and skips when null; the report keeps its small display fallback in one place. This makes the "one true selection rule" explicit and halves the surface for future divergence.

**Status: fixed.** `PackageVariant` now exposes `HighestOriginalVersion` and `HighestCurrentVersion`, both backed by one
private selection method. The handler uses `HighestCurrentVersion`; the report uses the appropriate property and keeps
its existing display fallback for invalid versions. Covered by `PackageVariantTests`.

### 3. Negated version comparison is harder to read than the positive form *(Readability · Low)* — ✅ Fixed

`src/DotBump/Commands/BumpPackages/BumpPackagesHandler.cs:54-58`

```csharp
// Never downgrade: only apply a target higher than every current occurrence in the variant.
if (!(newVersion > package.CurrentVersion))
{
    continue;
}
```

The `<=` operator exists on `SemanticVersion`; the positive form reads the same as the guard's intent:

```csharp
if (newVersion <= package.CurrentVersion)
{
    continue;
}
```

**Status: fixed.** The guard now uses the positive `<=` form. Existing no-downgrade coverage verifies the behavior is
unchanged.

### 4. Condition formatting string duplicated in three places *(Duplication · Low)* — ✅ Fixed

The `(when <condition>)` display convention is now hand-built in:

- `BumpResult.ToString()` — `src/DotBump/Reports/BumpResult.cs:24`
- `MarkdownReportFormatter.FormatResult` — `src/DotBump/Reports/MarkdownReportFormatter.cs:80-82`
- The invalid-version warning in `BumpPackagesHandler.GetPackagesToBump` uses a related ` under condition '...'` suffix (line 116)

Each currently formats slightly differently (leading space inside the parentheses vs. outside). If the convention ever changes (e.g. quoting style, truncation of long conditions), three sites must move in lock-step. Consider one shared formatter (e.g. a static helper on `BumpResult` or `PackageVariant`) that both report formatters and the warning text call. Low priority, but cheap to do while the concept is fresh.

**Status: fixed for the two report paths.** `BumpResult.DisplayId` now owns the `(when ...)` convention used by
`ToString()` and markdown. The invalid-version warning intentionally keeps its different `under condition '...'`
diagnostic wording. Covered by `BumpResultTests`.

### 5. Document the (deliberate) exact-string condition semantics *(Documentation · Low)* — ✅ Fixed

Effective conditions are compared with `string.Equals(..., StringComparison.Ordinal)` after per-element `Trim()` and joining with `" && "`. Consequences worth stating explicitly (in the `PackageVariant`/`PackageVersionEntry.Condition` XML docs or the FAQ/README):

- `$(TargetFramework) == 'net10.0'` and `'$(TargetFramework)'=='net10.0'` are *different variants* and resolve separately (duplicate NuGet queries for the same id).
- `'$(A)' == 'x'` nested in one file vs `'$(A)' == 'x' && '$(B)' == 'y'` produce different grouping even when one is logically implied.
- `||`, negations, and logical equivalences are not considered.

The doc comment on `ReadEffectiveCondition` already says "conditions are not evaluated", which is good; adding a sentence on the grouping consequences would help future maintainers distinguish design from oversight. No functional change requested.

**Status: fixed.** `README.md` now explains that condition matching is textual and that formatting, logical structure,
`||`, and negation are not normalized. `PackageVariant.Condition` documents the same rule.

### 6. Variant with mixed valid/invalid versions bumps its invalid occurrences too *(Behavioral observation · Low)* — ✅ Fixed

`BumpPackagesHandler.GetPackagesToBump` derives the reference version only from *valid* semantic versions in the variant (invalid ones are filtered out and, when all are invalid, the variant is skipped with a warning). However, `manifest.SetVersion(variant, ...)` then rewrites **every** occurrence in the variant — including occurrences whose original version could not be parsed. This matches the pre-existing per-package behavior (not a regression), but now that variants make partial-validity visible, it may merit a warning such as "N occurrence(s) had unparseable versions and were overwritten". Worth a conscious decision rather than leaving it implicit; a test asserting the current behavior would lock in whichever choice is made.

*(Tests currently cover the all-invalid case and the all-valid case; the mixed case is untested.)*

**Status: fixed.** The handler now emits a warning when a variant contains valid and unparseable versions, stating that
those occurrences will be overwritten when a bump is applied. The existing overwrite behavior is preserved and covered
by `With_Mixed_Valid_And_Unparseable_Versions_Warns_And_Updates_Both`.

### 7. `_results` first-match matching depends on conditions surviving the round-trip unchanged *(Robustness · Info)* — ✅ Fixed

`BumpReport.ReportChanges(PackageManifest)` matches report items by `(Id ordinal-ignore-case, Condition ordinal)`. Both `GetVariants()` calls (constructor and `ReportChanges`) derive from the same manifest instance in the current call flow, so matching is reliable. But the matching is only as stable as condition normalization:

- `GetVariants` normalizes whitespace-only conditions to `null` but does **not** `Trim()`.
- `ReadEffectiveCondition` does `Trim()` each part and joins with `" && "`.

Those are consistent today because the production read path is the only source of conditions. If a future source (e.g. a loader for persisted manifests) adds untrimmed conditions, `ReportChanges` would silently fail to match and report entries would show no change. If a dictionary keyed by `(Id, Condition)` is adopted per finding 1, consider a single normalization function (`Trim`, whitespace→null) applied in exactly one place and reused everywhere.

**Status: fixed.** `PackageVariant.NormalizeCondition` is now the single normalization point (`Trim`, whitespace→null), used by
both `ReadEffectiveCondition` and `GetVariants`. Covered by `With_Untrimmed_Conditions_Groups_By_Trimmed_Value`.

### 8. Interface contract: equality is now load-bearing for `IPackageVersionResolver` *(Documentation · Info)*

Changing the return type to `IReadOnlyDictionary<PackageToBump, SemanticVersion>` is a genuine improvement — it decouples "which input was resolved" from "package id". One subtlety worth capturing in the interface doc comment (the implementation's behavior relies on it):

> Inputs with an equal package id (case-insensitive) and an equal current version (`1.0.0` == `1.0.0.0`, pre-release labels compared per NuGet semantics) are treated as one resolution input and appear once in the result.

This follows from `SemanticVersion`'s `CompareTo`-based record equality and `OrdinalIgnoreCase` id hashing, but a reader of `IPackageVersionResolver` cannot know it without also reading `SemanticVersion`.

---

## Positive Notes

- **Skip-warning deduplication** (`skippedPackageIds` HashSet) correctly preserves the old one-warning-per-package behavior under the new per-variant iteration — a regression the change would otherwise have introduced. Well handled.
- **`BumpResult.Condition` with `JsonIgnoreCondition.WhenWritingNull`** keeps the JSON payload stable for unconditional packages and non-package commands; both serialization paths are tested (`With_Conditional_Package_Serializes_Condition_On_Each_Result`, `With_Unconditional_Package_Does_Not_Serialize_Condition`).
- **Markdown ordering** (`OrderBy(Id)` then `ThenBy(Condition)`) gives deterministic, grouped output for multi-variant packages.
- **Test helpers** (`TestPackageManifestFactory.CreateManifestWithConditions`, `PackageManifestTestExtensions`) keep existing call sites untouched via the tuple-typed extension, which is a clean additive approach.
- **`With_Same_Id_And_Different_Current_Versions_Resolves_Each_Input`** and **`With_Different_Conditions_And_Same_Reference_Version_Resolves_Once_And_Bumps_Both`** pin down exactly the two dedupe behaviors that justify the `PackageToBump` key — good choice of test cases.

---

## Test Coverage Observations

- A handler-level test covering a *mixed* situation (one unconditional + one conditional variant of the same package, bumped independently) would complement the existing per-condition and same-version tests and lock in the variant grouping end-to-end.
- PackageManifestTests has no 2-space/JSON file preference issue — all new tests follow the established conventions. The `PackageManifestTestExtensions.SetVersion(string, ...)` helper's `.Single()` failure mode (test fails loudly when a package has multiple variants) is the right behavior for a convenience helper, and the XML doc explains it. Good.

---

## Summary

| # | Severity | Area | Finding | Status |
|---|----------|------|---------|--------|
| 1 | Medium | Quality | `GetVariants` default-tuple sentinel check; O(n²) grouping — use a composite-key dictionary | ✅ Fixed (`c50d747`) |
| 2 | Medium-Low | Duplication | Highest-version selection logic duplicated between handler and report | ✅ Fixed |
| 3 | Low | Readability | `!(a > b)` → `a <= b` | ✅ Fixed |
| 4 | Low | Duplication | `(when ...)` formatting repeated in three places | ✅ Fixed |
| 5 | Low | Docs | Document exact-string condition grouping consequences | ✅ Fixed |
| 6 | Low | Behavior | Mixed valid/invalid variant bumps invalid occurrences (matches old behavior; make it explicit + test) | ✅ Fixed |
| 7 | Info | Robustness | Report matching depends on condition normalization consistency | ✅ Fixed |
| 8 | Info | Docs | Document equality-based dedupe contract on `IPackageVersionResolver` | Open |

No blocking issues. Findings 1–7 are fixed; the remaining finding is optional documentation polish.
