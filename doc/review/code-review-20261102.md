# Code Review — `docker-containers` branch

- **Date:** 2026-11-02
- **Reviewer:** .NET Tech Lead
- **Scope:** `99ea92b` — `feat: add docker command to bump the .NET SDK base image tag`
- **Files reviewed:** `src/DotBump/Commands/BumpDocker/*`, `src/DotBump/Reports/BumpReport.cs`,
  `src/DotBump/CommandConfiguration.cs`, `src/DotBump/Commands/BumpCommandNames.cs`, and the
  corresponding tests under `test/DotBump.Tests/Commands/BumpDocker/`, `test/DotBump.Tests/Reports/`.

## Summary

Overall this is a well-structured, idiomatic addition that follows the established command pattern
(command → handler → file service → manifest models), reuses the SDK release-index plumbing, and
ships with a solid, isolated test suite. The span-based rewriting of Dockerfiles (recorded offsets,
backwards application, BOM/line-ending preservation) is careful and well covered by tests.

Findings below are ranked. Finding 1 has been resolved; Finding 2 still deserves attention before merge, and the
rest are minor.

## Status

| Finding | Status |
| --- | --- |
| 1. Cross-major image rewrite | **Fixed** in `49ac565` — targets are now resolved per SDK channel. |
| 2. Silent multi-line `FROM` skip | Open |
| 3. Silent skip on missing file text | Open |
| 4. Trailing `@` digest edge case | Open |
| 5. Continuation state and comments | Open |
| 6. `XmlFileText` naming | Open |
| 7. Brittle split assertion | Open |
| 8. Design observations | Informational |

---

## Findings

### 1. HIGH — Cross-major image rewrite under `minor`/`patch` bump types

> **Status: fixed in `49ac565`.** The handler no longer resolves a single repository-wide target. It groups
> occurrences by SDK channel (`major.minor` for `patch`, `major` for `minor` and `major`), resolves each group from
> its highest current tag, and applies the result only inside that group. `minor`/`patch` therefore never cross a
> major, and `major` still moves every image to the latest release. The original recommendation was superseded; see
> the resolution below.

`BumpDockerHandler.HandleAsync` picks the *highest* `ParsedVersion` across **all** entries in the
repository as the "current" SDK, resolves a target release for it, and then updates **every** entry
below that target:

```csharp
var currentEntry = manifest.Entries
    .OrderByDescending(entry => entry.ParsedVersion)
    .FirstOrDefault();
...
foreach (var entry in manifest.Entries.Where(entry => newVersion > entry.ParsedVersion))
{
    manifest.SetTag(entry, newTag);
}
```

In a repository with Dockerfiles pinned to different majors (very common: a legacy migration where
one service still runs `sdk:8.0.404` while others are on `sdk:10.0.404`), a `--type patch` bump
resolves the target from the 10.0 channel (`10.0.406`) and then **silently rewrites the 8.0 image to
`10.0.406`**. The user asked for "the latest patch of the current minor" and received a major
platform upgrade in several Dockerfiles. This contradicts the documented semantics ("Use the 'patch'
type option to bump the image to the latest patch version for the current minor version") and the
behaviour of the `sdk` command, where a patch bump never changes majors.

Note that `ReleaseFinder.TryFindPatch`/`TryFindMinorOrPatch` behave correctly *given* the single
`currentSdk` they are handed — the issue is which `currentSdk` the handler feeds in and which
entries it lets the target leak onto.

**Original recommendation (superseded):** filter the eligible entries by major and warn about entries
left on another major. That would have prevented the destructive rewrite, but it would still leave
lower-major images unbumped and would not enforce the minor cap for `patch`.

**Resolution applied in `49ac565`:** resolve per SDK channel instead of per repository. The handler
groups occurrences by `major.minor` for `patch` and by `major` for `minor`/`major`, takes the highest
tag in each group as the reference, resolves that group with `ReleaseFinder`, and applies the target
only to entries inside the group. Behaviour:

* `patch` keeps every image on its current `major.minor`.
* `minor` keeps every image on its current major.
* `major` removes the cap, so every group converges on the latest release (stable by default, highest
  available with `--prerelease`).

Tests added in `BumpDockerHandlerTests`:

* mixed-major `patch` and `minor` — each major is bumped to its own latest;
* mixed-major `major` — every image converges on the latest release;
* mixed-major `major --prerelease` — every image converges on the highest pre-release;
* mixed patches within one major — all converge on the latest patch;
* mixed minors with `patch` — each minor stays within itself.

### 2. MEDIUM — Multi-line `FROM` skip is silent, contradicting the documented behaviour

In `DockerfileService.ReadFromInstructions`, multi-line `FROM` instructions are skipped with only a
Debug log:

```csharp
if (continuesOnNextLine)
{
    if (match.Success)
    {
        logger.Debug("Skipping multi-line FROM instruction in {File}", relativePath);
    }
    isContinuation = true;
    continue;
}
```

The README (and, implicitly, the warning contract of this command) states that skipped SDK images
are "left unchanged and listed under `warnings` in the output report", including "Multi-line `FROM`
instructions". The test `With_Multi_Line_From_Instruction_Skips_It` even asserts
`Warnings.ShouldBeEmpty()`. So either the README is wrong or the code is. Since a skipped `FROM`
means the Dockerfile quietly keeps an outdated SDK image, a manifest warning (visible in the report)
is the better behaviour:

```csharp
if (match.Success)
{
    AddWarning(
        manifest,
        $"Skipping multi-line FROM instruction in '{relativePath}' (line {lineIndex + 1}) because multi-line instructions are not supported yet.");
}
```

…and update the test to assert the warning. Please also reconcile the README wording with the code.

### 3. LOW — `SaveDockerfileManifest` can silently skip a changed file while the report claims a bump

```csharp
foreach (var filePath in manifest.ChangedFiles)
{
    var text = manifest.GetFileText(filePath);
    if (text == null)
    {
        continue;
    }
    ...
}
```

`bumpReport.ReportChanges(manifest)` runs *before* `SaveDockerfileManifest`, so if a changed file
has no registered text the report says the tag was bumped while the file was never written. This is
currently unreachable through the normal flow (text is registered for every file that produced
entries), but as a defensive branch it should be a warning (`AddWarning`) rather than a `continue`
with only a missing log — right now the skip would be completely invisible. Also consider re-reporting
errors per-file so a filesystem failure during one write does not leave the user with a partially
bumped repo and a report claiming success.

### 4. LOW — Digest edge case: `sdk:10.0.101@` passes the digest guard

```csharp
var digestIndex = imageToken.Value.IndexOf('@', StringComparison.Ordinal);
...
if (digestIndex >= 0 && digestIndex < imageToken.Value.Length - 1)
{
    AddWarning(...); return;
}
```

For a tag like `mcr.microsoft.com/dotnet/sdk:10.0.101@` (trailing `@`, empty digest — invalid
Dockerfile syntax), `digestIndex == Value.Length - 1` sidesteps the guard, the tag is parsed as
`10.0.101`, and a later bump rewrites the file as `sdk:11.0.101@`, preserving the stray `@`. The
input file is broken in either case, but a silent half-fix is worse than a clear warning. Treat any
presence of `@` as digest-pinned (`digestIndex >= 0`) so all such cases are skipped with the same
diagnostic.

### 5. LOW — Continuation state is reset by a comment line inside a multi-line instruction

```csharp
var continuesOnNextLine = !isComment && line.TrimEnd().EndsWith('\\');
if (isContinuation)
{
    isContinuation = continuesOnNextLine;
    continue;
}
```

Docker skips comment lines *inside* a continued instruction, so for:

```dockerfile
RUN apt-get update \
# note about the install
FROM mcr.microsoft.com/dotnet/sdk:8.0.404
```

the comment line resets `isContinuation` to `false`, and the `FROM` (still logically part of the
`RUN` shell command) is parsed as a new instruction. Consequence: the subsequent `FROM`-shaped
content in *that* construct could be misinterpreted. The same divergence exists for non-comment
lines whose continuation starts with a `#` and is followed by an instruction-looking shell arg.
Practically rare, and the current behaviour errs on the safe side for `FROM`-shape lines, but the
parser is knowingly different from Docker's here. If you extend the continuation tracking later,
treat comment-only lines inside a continuation as consumed continuation content (internal to the
skip logic), not as terminators. At minimum, add a comment in `ReadFromInstructions` documenting
this accepted divergence so a future maintainer does not "fix" it blindly.

### 6. LOW — Reusing `XmlFileText` for plain-text Dockerfiles is misleading

`DockerfileService` reads/writes Dockerfiles via `XmlFileText.ReadText`, `XmlFileText.WriteText` and
`XmlFileText.GetLineStartOffsets`. Those APIs happen to be generic (byte-based encoding detection,
offset math), but the type name promises XML and only the XML-specific members
(`GetAttributeValueSpan`, `GetElementTextSpan`) actually care about XML. A reader of the Docker
code has to check `XmlFileText` before trusting it with a non-XML file.

**Recommendation:** rename the type to something like `EncodedFileText` (or extract the
encoding/offset helpers into a small `FileText` static class and keep `XmlFileText` for the XML span
helpers). This shows up again in the log line `"Updated SDK image tags in {File}"` calling
`XmlFileText.WriteText` — correct at runtime, confusing on screen.

### 7. LOW (tests) — Brittle assertion in `With_Multiple_Occurrences_Updates_All_Of_Them`

```csharp
File.ReadAllText(filePath).ShouldNotContain("10.0.100");
File.ReadAllText(filePath).Split("10.0.101").Length.ShouldBe(3);
```

Deriving the occurrence count from a `Split().Length` of 3 (i.e. "two chunks less than three")
is non-obvious, and the file is read twice. Prefer:

```csharp
var updated = File.ReadAllText(filePath);
updated.Where((c, i) => ...).ShouldBe(2); // or
updated.Split("10.0.101").Length.ShouldBe(3) // with a comment: 2 occurrences → 3 chunks
```

—the simplest fix is to read once into a local and count occurrences with
`updated.CountOccurences("10.0.101")`-style helper or `Regex.Count(updated, "10.0.101")`, or simply
assert on the two full lines as the handler test `With_Multiple_Occurrences_Does_Not_Downgrade`
already does (which reads better).

### 8. INFO — Design observations (no change requested)

* **Reuse of `ReleaseFinder` via `new Sdk(currentEntry.OriginalTag, string.Empty)`:** acceptable
given the stated goal of identical version selection; the fake `Sdk` with an empty `rollForward` is a
bit of a smelly contract since `TryFindNewRelease` ignores it, but introducing a
`SemanticVersion`-based overload is not worth the churn now. A comment on the call site explaining
"Sdk here is only used for its parsed version" would help future readers.
* **Release-index → tag mapping assumption:** `SdkImageTag.TryMapFromSdkVersion` assumes every
release-index version has a published `mcr.microsoft.com/dotnet/sdk:<version>` tag. Historically a
brand-new release can exist in the index before the Docker tag is published. The command will then
edit the Dockerfile to a tag that may not exist yet. Worth a note in the README/limitations, not a
code change.
* **`DockerfileManifest` visibility mix:** public `HasChanges`/`Entries` alongside internal
`ChangedFiles`/`Add` works, but `Add` being the mutation point while `TrackFileText` is internal-only
reads slightly inconsistently — matching the surrounding manifest classes' style is fine as-is.
* **`ReportChanges(DockerfileManifest)`** is O(entries × results) — trivial at these sizes; fine.

## Tests

Good coverage overall: command-level exit codes and console output, handler scenarios including
no-downgrade and prerelease mapping, service-level parsing, offsets, CRLF/BOM preservation, and
tag-mapping theory tests. Conventions (nested classes per method, AAA, `ShouldSatisfyAllConditions`,
`LocalDirectory` isolation) are followed.

Missing coverage to add:

* ~~Mixed-major Dockerfiles (Finding 1).~~ Covered by the per-channel `patch`/`minor`/`major` tests added in
  `49ac565`.
* Warning-on-skip for multi-line `FROM` if Finding 2 is accepted.
* One `Containerfile`/`Dockerfile.dev` save-path test (the save tests only exercise `Dockerfile`,
and the discovery test asserts entry counting, not writing).

## What's done well

* Consistent structure with the other commands (command/handler/service/interfaces folders,
`MethodStart`/`MethodReturn` tracing, `DotBumpException` for domain errors).
* Span-based tag replacement preserves surrounding bytes exactly (BOM, CRLF, indentation), tested.
* The "no downgrade" rule, prerelease mapping from the release index, and the "only fully versioned
SDK tags" guard are all clearly implemented and guarded with regex timeouts.
* `SdkImageTagTests` theory tables make the supported/unsupported tag matrix explicit.
