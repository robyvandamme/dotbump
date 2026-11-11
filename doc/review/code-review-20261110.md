# Code Review — branch `extend-docker`

- **Date:** 2026-11-10
- **Commit under review:** `ed40686` — `feat: bump aspnet, runtime and runtime-deps base images in docker command`
- **Base:** `fa2aac1`
- **Reviewer:** .NET Tech Lead (automated review)

## Scope of the change

The change extends `dotnet dotbump docker` from SDK image tags only to the four supported .NET base
image families (`sdk`, `aspnet`, `runtime`, `runtime-deps`). Key pieces:

- New `DotnetImageVersionSource` enum, `DotnetImages` registry and `ChannelReleaseVersions` resolver
  that map each image family to the release component it is tagged with.
- `ChannelRelease` / `ChannelComponent` models extended to carry the `runtime`, `sdk`,
  `aspnetcore-runtime` and `release-version` components from each channel's `releases.json`.
- `ReleaseSelector.TryFindNewest<T>` extracted from `ReleaseFinder` into a shared, generic
  minor/patch/major selection algorithm now used by both `bump sdk` and `bump docker`.
- `BumpDockerHandler` rewritten to load channel release histories per channel, group image
  occurrences by (version source, channel), and apply the bump.
- `SdkImageTag` renamed to `DotnetImageTag`.

## Overall verdict

Well-structured change with a good separation of concerns. The extraction of
`ReleaseSelector` removes a real duplication (the major/pre-release rules existed previously only
inside `ReleaseFinder`), the warning handling is consistent and tested, and the "never downgrade"
guard in `ApplyBump` is a nice touch. Test coverage is strong: happy paths, mixed families, mixed
majors, pre-release mapping, missing components, channel-index misses and history fetch failures are
all covered.

Findings below are all low severity or nits; no blocking issues were found.

---

## Findings

### 1. [Low] Major bump silently requires the group's own channel to have loaded

`src/DotBump/Commands/BumpDocker/BumpDockerHandler.cs` — `ApplyBump`:

```csharp
if (!channelReleases.TryGetValue(channel, out var channelRelease))
{
    // The channel was skipped while loading the release history; the warning is recorded.
    return;
}

var candidates = bumpType == BumpType.Major
    ? channelReleases.Values.SelectMany(item => item.Releases)
    : channelRelease.Releases;
```

For a `major` bump the candidates come from **all** loaded channels, but the method still bails out
if the *current* channel (`channel`) is not in the dictionary. Consequence: a Dockerfile pinned to
`sdk:8.0.404` whose 8.0 history failed to load will not be bumped to e.g. `10.0.405` even though
fresh candidate data exists, and the local `channelRelease` variable is unused on the major path.

If this is intentional (conservative behavior for images on orphaned channels), a comment stating
"major bumps still require the current channel to be resolvable" would help future readers;
otherwise consider skipping the gate for the major path:

```csharp
var candidates = bumpType == BumpType.Major
    ? channelReleases.Values.SelectMany(item => item.Releases)
    : (channelReleases.TryGetValue(channel, out var releases)
        ? releases.Releases
        : null);
if (candidates is null) { /* warn/skip */ }
```

The "does not list that channel" / "could not be read" checks in
`GetChannelReleasesAsync` are unit-tested, but there is currently no test pinning this
particular interaction between a failed channel load and a major bump, which makes the intent hard
to verify.

**Status: Resolved in `568d329`.** The gate is intentional: an image whose own channel history was
skipped is left untouched so the recorded warning stays accurate. `ApplyBump` now branches on the
bump type and documents that rule next to the check. The "no test" observation was incorrect —
`With_Channel_Missing_From_Index_And_Major_Bump_Adds_Warning_And_Leaves_Tags` already pinned the
gate; `With_Channel_History_Failure_And_Major_Bump_Skips_Only_That_Channel` now also pins the
failed-fetch variant.

### 2. [Low] `ApplyBump` resolves candidate versions up to three times

`src/DotBump/Commands/BumpDocker/BumpDockerHandler.cs` — `ApplyBump` calls
`ChannelReleaseVersions.GetVersion(...)`:

1. once inside the `versionSelector` lambda passed to `ReleaseSelector.TryFindNewest` (per
   candidate, per comparison pass),
2. again in the `candidates.Any(release => GetVersion(...) != null)` check when nothing newer was
   found,
3. once more on the selected release to compute `newVersion`.

Each call re-parses version strings. The result is correct, but it is easy to consolidate: project
the candidates into `(ChannelRelease, SemanticVersion?)` pairs once, pass them to
`ReleaseSelector`, and reuse the list for the "no supported version" check and for the selected
target. This removes redundant parsing and keeps the selection warning logic on the same data it
actually evaluated.

**Status: Resolved in `6287777`.** `ApplyBump` now projects the candidates and their parsed versions
once into `ReleaseCandidate` instances, passes that list to `ReleaseSelector`, and reuses the
selected candidate's version. The "no supported version" check is a `Count == 0` test on the same
list, and the lazy `SelectMany` is no longer enumerated twice.
`With_Some_Releases_Missing_AspNetCore_Version_Uses_Remaining_Rows` covers the mixed
supported/unsupported row case.

### 3. [Low, performance] Channel histories are fetched sequentially

`GetChannelReleasesAsync` awaits each channel's `releases.json` in a `foreach` loop. For a major
bump this can be several HTTP round-trips (all channels ≥ the minimum major in the manifest), with
latency scaling linearly. Since each channel is independent and failures are already handled
per-channel, this is a natural `Task.WhenAll` candidate:

```csharp
var lookups = channels.Select(async channel => (Channel: channel,
    Result: await TryGetChannelReleasesAsync(releases, channel).ConfigureAwait(false)));
var results = await Task.WhenAll(lookups).ConfigureAwait(false);
```

Not urgent for a CLI tool, but it becomes visible when many channels are involved.

**Status: Deferred.** Every network call in the tool is currently sequential — there is no
`Task.WhenAll` or `Parallel` usage in `src` — and `docker` makes at most one request per channel
(typically one, sometimes two). `packages`/`tools` make one or more requests per package per
source, so if concurrency is introduced it should be a cross-cutting change starting there rather
than a docker-only optimisation. If this is picked up later, note that `manifest.Warnings` is not
thread-safe: parallel loads must return their warnings and have them appended sequentially after
`Task.WhenAll`.

### 4. [Nit] Channel "ordering" is ordinal string order, not numeric

`GetChannelsToLoad` orders channels with `StringComparer.Ordinal`. That puts `"9.0"` after
`"10.0"`, which is surprising for a reader and only matters for warning/fetch order today. If
ordering ever gains semantic meaning, sort by the parsed major/minor instead; otherwise consider
dropping the `OrderBy` or adding a comment that order is not semantic.

**Status: Deferred.** Ordering currently affects only warning and fetch order, and it is
deterministic. If channel loading is ever parallelised (finding 3), sort by parsed major/minor at
that point so the warning output stays natural and stable.

### 5. [Nit] `DotnetImages.TryGetVersionSource` could be a dictionary

The four `string.Equals(..., StringComparison.OrdinalIgnoreCase)` branches work, but a static
`Dictionary<string, DotnetImageVersionSource>(StringComparer.OrdinalIgnoreCase)` lookup is shorter
and makes adding future image families (e.g., `aspire`, `worker`) a one-line change. Behavior would
be identical.

**Status: Resolved in `b152807`.** `DotnetImages` now uses a static
`Dictionary<string, DotnetImageVersionSource>(StringComparer.OrdinalIgnoreCase)`, so a new family is
one entry. `DotnetImagesTests` still covers the full mapping matrix.

### 6. [Nit] Structured logging: avoid `ToString()` in message arguments

`src/DotBump/Commands/BumpSdk/ReleaseFinder.cs`:

```csharp
logger.Debug("Found new release {Release}", newRelease.LatestSdkVersion.ToString());
```

Call `ToString()` only when the template forces it (this pattern is used elsewhere for
`MethodReturn`, which is fine). For a `Debug` message template, pass the object directly — Serilog
formats it via `ToString()` anyway, and passing the raw value keeps the event property typed
(`SemanticVersion`) rather than baked to a string.

**Status: Deferred.** The same `.ToString()` pattern appears at three call sites (`ReleaseFinder`,
`ReleaseWebService`, `BumpSdkHandler`). Changing only this one would leave the codebase inconsistent,
and passing the objects directly changes the structured event property type — that is a separate
logging decision rather than a docker follow-up.

### 7. [Nit] Double validation inside `DotnetImageTag.TryParse`

```csharp
version = new SemanticVersion(tag);
return version.IsValid && s_imageTagPattern.IsMatch(tag);
```

`s_imageTagPattern` is already stricter than (and subsumes) `SemanticVersion.IsValid` for image
tags — the pattern rejects four-part versions, so `version.IsValid` can never be the deciding
factor when the regex matches. Not harmful (the regex also guards against pathological input, and
`SemanticVersion` construction is cheap and never throws for non-null input), but if the intent is
"regex is the gate", the `IsValid` check is dead weight; if the intent is "both must agree", a short
comment would clarify.

**Status: Resolved in `b152807`.** The `IsValid` check was removed; `s_imageTagPattern` is the gate,
with a comment noting it is stricter than `SemanticVersion`. `DotnetImageTagTests` pins the accepted
and rejected tag matrix.

### 8. [Nit, tests] `CreateHandler(params Release[])` fabricates channel history from SDK versions

`test/DotBump.Tests/Commands/BumpDocker/BumpDockerHandlerTests.cs`:

```csharp
releaseService.WithChannelReleases(group.Key,
    group.Select(release => CreateChannelRelease(release.LatestSdk, release.LatestSdk)).ToArray());
```

In this fixture the runtime/aspnet versions default to the SDK version, so tests using the
`params Release[]` overload implicitly assert that SDK-version tags are valid runtime- and
aspnet-viability inputs. That is fine for the SDK-focused tests, but any future test that uses this
helper while asserting runtime-tag mapping could end up testing the fixture rather than the code.
A one-line comment on the helper noting "SDK-version history is fabricated; use
`WithChannelReleases` for runtime/aspnet expectations" would make the design explicit.

**Status: Resolved in `b152807`.** Added the comment to the `CreateHandler(params Release[])` helper.

---

## Positives

- **Real dedup:** `ReleaseSelector` now owns the minor/patch/major + pre-release rules shared by
  `bump sdk` and `bump docker`; the old `ReleaseFinder` switch-statements are gone and its public
  contract is unchanged.
- **Per-channel failure isolation:** an unreadable channel history warns and skips that channel
  only; tested. The `CA1031` catch has an appropriate pragma with a justification comment.
- **No-downgrade safety net:** `ApplyBump` only rewrites occurrences strictly below the resolved
  target, which protects already-current tags inside a group.
- **Tag mapping is defensively validated twice** (`TryMapFromVersion` re-runs `TryParse`), so the
  `FROM ... :<tag>` rewrite offsets in `DockerfileService` always receive a tag the parser itself
  accepts — good consistency between parse and write paths.
- **Warning messages are actionable** (image, file, line, reason) and consistently routed through
  the manifest → report pipeline with the Serilog templates required by the project conventions.
- **Test hygiene:** new tests follow `Pascal_Snake_Case`, nested per-method groups, Shouldly
  assertions, and the test data files mirror the real `releases.json` schema additions.

## Conclusion

No blocking findings. Findings 1, 2, 5, 7 and 8 are resolved (`568d329`, `6287777`, `b152807`);
findings 3 and 4 are deferred with the future concurrency work, and finding 6 is deferred as a
cross-cutting logging decision. Approved with these as optional improvements.
