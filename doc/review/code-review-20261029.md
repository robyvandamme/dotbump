# Code Review — branch `project-sdk-pump`

**Date:** 2026-10-29
**Scope:** Commits `5838ce3..39470c7` — feature: support bumping project SDK references via the
`dotnet dotbump packages` command.

**Files reviewed:**
- `src/DotBump/Commands/BumpPackages/PackageFileService.cs` (additive SDK-reference parsing + `TryAddVersion` refactor)
- `src/DotBump/CommandConfiguration.cs` (command description)
- `test/DotBump.Tests/Commands/BumpPackages/PackageFileServiceTests.cs`
- `test/DotBump.Tests/Commands/BumpPackages/BumpPackagesHandlerTests.cs`
- `test/DotBump.Tests/Data/Packages/AppHost.csproj`, `test/DotBump.Tests/DotBump.Tests.csproj`

**Update (2026-10-29, post-review fixes):** Findings 1 and 3 were addressed and test gap 1 was
closed. Writing those tests uncovered a more serious flaw in the same raw-text path than the one
reported: semicolon splitting also matched the `;` inside numeric entities, so an entity-encoded
version in the root `Sdk` attribute was mis-parsed and a bump rewrote the attribute to a malformed
value. That bug was fixed in `8f157d9`. Finding 2, findings 4–5 and test gaps 2–3 were deliberately
skipped, and versioned SDK references on `<Import>` elements remain unsupported (documented, not
implemented). The original findings are retained below with status notes, so the document still
reflects what was first reported.

| Item | Status |
|------|--------|
| Finding 1 — entity-encoded slash | **Partially resolved** — encoded slash documented; related entity-semicolon bug fixed |
| Finding 2 — duplication with the `PackageReference` loop | **Skipped** — `TryAddVersion` already covers the meaningful part; proposed helper would need ~9 parameters |
| Finding 3 — repeated `"Sdk"` literal | **Resolved** — `SdkName` constant used at all four sites |
| Finding 4 — redundant double traversal | **Skipped** — review did not recommend a change |
| Finding 5 — non-short-circuiting `\|=` | **Skipped** — verified correct; no comment added |
| Test gap 1 — entity-encoded SDK version | **Resolved** — four tests added (attribute, element, id, save normalization) |
| Test gap 2 — `GetAttributeValueSpan` failure branch | **Skipped** — defensive, effectively unreachable with `SetLineInfo` |
| Test gap 3 — `<Sdk>` with child `<Version>` | **Skipped** — `<Sdk>` has no child elements in the MSBuild schema |
| Additional — versioned SDK references on `<Import>` | **Documented as unsupported**, not implemented (no requirement yet) |

---

## Overall assessment

Good, solid change. The approach is sound: SDK references (`Sdk="Id/version"` on the project root
and `<Sdk Name="..." Version="..." />` elements) are parsed on the **raw text span** level (same
strategy the service already uses for `PackageReference` versions), so only the version substring
is rewritten during save and the rest of the file stays byte-for-byte intact. The refactor of the
duplicate read/warn logic into `TryAddVersion` removes meaningful duplication, and the refactor
was behavior-preserving (warning order and content are unchanged).

Particular positives:

- **Lazy span computation** (`Func<(int, int)> getVersionSpan`) keeps `TryAddVersion` from doing
  expensive line-info work for versions it is about to skip anyway. Clean design.
- **Raw-span parsing in `ParseSdkAttributeReferences` / `TryParseSdkReference`** is careful:
  semicolon splitting with span tracking, `LastIndexOf('/')` for `<id>/<version>`, whitespace
  trimming via the existing `XmlFileText.TrimSpan`, and empty/unversioned segments silently
  skipped (correct — unversioned SDKs like `Microsoft.NET.Sdk` must not warn).
- **Registration semantics preserved:** `manifest.RegisterFileText` is still only called when
  something was actually found, so files without recognized entries are never rewritten.
- **Tests persist and restore the exact text** rather than asserting substrings, which is the
  strongest possible style for a span-based file rewriting feature.

No blocking issues found. The findings below are minor.

---

## Findings

### 1. Minor — entity-encoded slash (`&#47;`) silently hides an SDK reference

> **Status (2026-10-29): partially resolved.**
>
> The minimal mitigation was applied: the raw-text limitation is now documented in
> `TryParseSdkReference`. Decoding and mapping back to raw coordinates was not implemented, so
> encoded slashes are still skipped.
>
> Writing the test from gap 1 revealed that the premise above — "the version decodes fine" — was
> only true for the `<Sdk Version="..." />` element form. In the root `Sdk` attribute the raw value
> was split on **every** semicolon, including the terminator of numeric entities, so
> `Sdk="Aspire.AppHost.Sdk/13&#46;0&#46;1"` produced a bogus version `13&#46` and a bump rewrote the
> attribute to `Aspire.AppHost.Sdk/13.6.4;0&#46;1`. Fixed in `8f157d9` by making the separator scan
> entity-aware (`FindReferenceSeparator`).

`TryParseSdkReference` searches for `'/'` in the **raw** segment text
(`PackageFileService.cs:206-236`). If an author writes

```xml
<Project Sdk="Aspire.AppHost.Sdk/13&#46;0&#46;1" />
```

the version decodes fine, but an encoded slash — `Sdk="Aspire.AppHost.Sdk&#47;13.0.1"` — makes
the parser conclude there is no slash at all, and the reference is skipped **silently** (same
code path as a legitimately unversioned SDK). This is consistent with the spirit of the comment
in `ReadVersion` (`PackageFileService.cs:133-137`), which explicitly commits to supporting
entity-encoded values elsewhere. Two cheap mitigations, in order of preference:

- Decode the segment first and search the slash in the decoded text, then map back to raw
  coordinates (a `TrimSpan`-like helper can find the boundary offsets by scanning raw text for
  the same character positions).
- Or, minimally, add a comment to `TryParseSdkReference` documenting this known limitation next
  to the "never contains a slash" claim, so a future reader doesn't assume it was considered.

The practical risk is low (encoded slashes in `Sdk` values are rare), but the silent-skip
behavior is the notable part: it is indistinguishable from "not a versioned SDK at all".

### 2. Minor — duplication between `ReadSdkElementReferences` and the `PackageReference` loop

> **Status (2026-10-29): skipped.**
>
> `TryAddVersion` already removed the meaningful duplication. The proposed helper hardcodes
> `PackageSourceKind.Project`, which is wrong for the package loop (central and shared files can
> produce other source kinds), and adding the source kind, id-attribute resolver and differing
> debug message would leave a roughly nine-parameter helper for little gain.

`ReadSdkElementReferences` (`PackageFileService.cs:374-413`) repeats the exact
*read id → read version → warn on missing → `TryAddVersion`* flow from the
`ReadPackageVersions` element loop (`PackageFileService.cs:264-301`), differing only in the id
attribute (`Name` vs `Include`/`Update`) and the debug message. Consider extracting a small
helper such as:

```csharp
private bool TryReadVersionedEntry(
    string filePath,
    string elementName,
    string packageId,
    XElement element,
    string text,
    int[] lineStartOffsets,
    PackageManifest manifest)
{
    if (string.IsNullOrWhiteSpace(packageId))
    {
        return false;
    }

    var (version, versionSource) = ReadVersion(element);
    if (version == null || versionSource == null)
    {
        logger.Debug(
            "Skipping {Element} {PackageId} in {File} because it has no version",
            elementName, packageId, filePath);
        return false;
    }

    return TryAddVersion(
        manifest, filePath, PackageSourceKind.Project, elementName, packageId, version,
        () => GetVersionSpan(text, lineStartOffsets, versionSource));
}
```

which both call sites can use. Not a correctness issue — `TryAddVersion` already eliminated the
worst of it — but this would finish the consolidation that commit `39470c7` started.

### 3. Nit — repeated `"Sdk"` literal

> **Status (2026-10-29): resolved.**
>
> Added `SdkName = "Sdk"` and replaced the attribute lookup, element filter and both labels.

The string literal `"Sdk"` (element name label used in `TryAddVersion` calls and warnings)
appears four times (`PackageFileService.cs:365, 366, 402-406`). The class already collects
similar strings into named constants / static arrays (`s_packageElementNames`,
`DirectoryPackagesPropsFileName`, …); a `private const string SdkElementName = "Sdk";` would
match the local conventions.

### 4. Info — redundant double tree traversal

> **Status (2026-10-29): skipped** — the review did not recommend a change, and keeping the two
> passes separate avoids coupling the features.

`ReadPackageVersions` enumerates `root.Descendants()` once for package elements and
`ReadSdkElementReferences` enumerates it a second time for `Sdk` elements. The cost is O(n) and
negligible for the file sizes involved, so this is only worth noting for tidiness; folding it
into one pass with a combined `Where` would slightly couple the two features for no real gain.
Not recommending a change.

### 5. Info — `sdkReferenceFound |= TryAddVersion(...)` is non-short-circuiting by design

> **Status (2026-10-29): skipped** — verified that the `|=` behaviour is intentional; no comment
> was added.

`PackageFileService.cs:361, 402`: `|=` ensures *every* reference is processed (each being a
separate manifest entry), which is correct here. Just flagging it for reviewers since `&&`-style
short-circuiting would have been a bug; worth a passing comment if authors want to protect the
intent.

### Additional gap — versioned SDK references on `<Import>` elements

MSBuild also supports `<Import Project="Sdk.props" Sdk="My.Custom.Sdk" Version="1.2.3" />`, a form
often used in `Directory.Build.props`. `ReadSdkReferences` only reads the root `Sdk` attribute and
`<Sdk>` elements, so those references are silently skipped.

> **Status (2026-10-29): documented, not implemented.**
>
> There is no current requirement for this form, so it was added to the README limitations and the
> `ReadSdkReferences` XML doc comment (commit `6ef8e1b`). Implementing it later does not require
> removing anything.

---

## Test review

Coverage is good for the happy paths and the important edge cases:

- Root attribute single SDK, multi-SDK (versioned + unversioned mixed) ✔
- Whitespace around the separator / around the version ✔
- Unversioned SDK skipped without warning ✔
- `<Sdk>` element with `Name`/`Version` ✔
- Unsupported version (`$(AspireVersion)`) warns ✔
- Save path respects spans for both attribute and element forms ✔
- Handler-level report assertion asserts resolved and bumped result ✔

Gaps worth considering (all small):

1. **Entity-encoded version in the SDK attribute** — e.g.
   `Sdk="Aspire.AppHost.Sdk/13&#46;0&#46;1"`. The reader refactors explicitly support decoded-
   vs-raw differences (see the `ReadVersion` comment), and `TryParseSdkReference` decodes the
   version with `WebUtility.HtmlDecode`, but no test pins this behavior for the SDK path. A test
   here would guard against a future "simplify" that drops the decode step.

   **Status (2026-10-29): resolved.** Four tests were added: encoded version on the root attribute,
   encoded version in a `<Sdk>` element, encoded id, and save normalization. They exposed the
   entity-semicolon bug described under finding 1.
2. **`GetAttributeValueSpan` failure path for the SDK attribute** (warning
   "…because the Sdk attribute value could not be located", `PackageFileService.cs:349-356`) —
   this warning branch has no test. Hard to trigger without a malformed file, so a unit test may
   be awkward; acceptable to leave, but it is the only uncovered branch in the new code.

   **Status (2026-10-29): skipped.** The branch is defensive and effectively unreachable through
   the file-reading path because loaded attributes always carry line info (`SetLineInfo`).
3. **`<Sdk>` element with a child `<Version>` element** (or `VersionOverride`) — `ReadVersion`
   is reused so behavior follows the package path, but the SDK-element tests only pin the
   `Version` attribute form.

   **Status (2026-10-29): skipped.** The MSBuild `<Sdk>` element has no child elements, so the
   child-`<Version>` form is invalid input; reusing `ReadVersion` is permissive but harmless.

Otherwise tests follow the repo conventions (file-based fixtures in `Data/Packages/`,
`ResetTempDirectory` isolation, AAA structure, `Shouldly` assertions, `Moq` logger).

---

## Conventions & style check

- Copyright header, file-scoped namespace, `using` placement — ✔
- Serilog message templates (no interpolation in log calls; interpolated warning strings are
  passed as a single parameter, and the `{Warning}` template is now a shared constant) — ✔
- Naming (`PascalCase` types/methods, `_camelCase` only where fields exist, etc.) — ✔
- Nullable annotations used correctly; XML entity caveat documented where it matters — ✔
- No new analyzer-risk constructs spotted.

## Security

No concerns. `WebUtility.HtmlDecode` on a bounded substring is safe; no path construction from
file content; spans are bounds-checked before use in `ApplyChanges`.

---

*Docs/README changes in this branch were not reviewed per the review scope.*
