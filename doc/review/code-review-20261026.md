# Code Review: `fix-debug` — bare boolean flags and `=` argument forms

**Date:** 2026-10-26  
**Scope:** branch `fix-debug` vs `main` (4 commits: `c327f7f`, `fb18211`, `a114271`, `6b0833a`). Code reviewed:
`src/DotBump/ArgumentHandler.cs`, `src/DotBump/CommandConfiguration.cs`,
`test/DotBump.Tests/ArgumentHandlerTests.cs`. README edits were treated as documentation and are out of scope except
where they describe the corresponding CLI behaviour.  
**Reviewer role:** .NET Tech Lead (code-only review).

**Update (2026-10-26, post-review fixes):** M1 was addressed in follow-up commits; N1 and part of L1 were cleaned up
in the same pass. The original findings are retained below with resolution notes, so the document still reflects what
was first reported. L2 remains open.

---

## Summary

Focused, small change. The user-facing behaviour is mostly correct:

- Bare `--debug`, `--debug=true` and `--debug=false` are now all accepted and configure logging consistently with
  Spectre.Console.Cli's flag parsing.
- `--logfile=value` is parsed by both the pre-parser and Spectre.
- The command examples now use bare boolean flags, which matches Spectre and the updated docs.
- The added unit tests are clean, follow the repository's Pascal_Snake_Case/AAA conventions, and cover the normal
  matrix well.

No critical or high-severity defects were found for valid CLI input. The main issues are a mismatch between the
pre-parser and the actual parser for some invalid forms, and the fact that the tests exercise only the pre-parser, so
they do not detect that mismatch.

| # | Severity | Finding | Status |
|---|----------|---------|--------|
| M1 | Medium | `IsDebugMode` accepts arbitrary `--debug=<value>` and `LogFile` accepts `--logfile=` even though Spectre rejects both; tests codify these unreachable command outcomes | **Resolved** — pre-parser aligned with Spectre, fail closed; tests updated |
| L1 | Low | Token matching and prefix literals are duplicated between `IsDebugMode` and `LogFile` | **Partially resolved** — shared constants added; a single token matcher was not extracted |
| L2 | Low | No CLI-level tests for the newly documented syntaxes; existing integration tests still use `--prerelease true` | Open |
| N1 | Nit | Comment claims a null check that is not performed | **Resolved** — comments corrected |

---

## M1. Pre-parser accepts forms that Spectre.Console.Cli rejects — **RESOLVED**

> **Resolution.** `src/DotBump/ArgumentHandler.cs` was updated so the pre-parser agrees with Spectre's grammar and
> fails closed:
>
> - `--debug=<value>`: only `true` (case-insensitive) enables debug logging. `false`, empty and any unrecognized value
>   return `false`.
> - Bare `--debug`: an explicit `true`/`false` value token sets the result; a following option token (or nothing) means
>   the flag was used bare and enables debug logging; any *other* following non-option token returns `false`, since
>   Spectre rejects such a value for a flag.
> - This removes the fail-open behaviour: an unknown value such as `--debug=yes` no longer enables debug logging for a
>   run that Spectre is about to abort, and a future positional argument can no longer be shadowed into the previous
>   always-true branch.
> - While editing, the option literals were extracted into shared constants (`DebugOption`, `DebugOptionPrefix`,
>   `LogFileOption`, `LogFileOptionPrefix`), which also removes the literal-vs-slice drift risk flagged in L1, and the
>   erroneous "null" comments from N1 were corrected.
> - For `--logfile=` the empty inline value is still treated as "no log file". This is the safe outcome: Spectre
>   rejects the form, so the helper value only matters for logging configured during a run that immediately fails. The
>   rationale is now documented on the `LogFile` method.
>
> Tests updated in `test/DotBump.Tests/ArgumentHandlerTests.cs`:
>
> - `With_Debug_Flag_And_Unrecognized_Value_Returns_True` → `With_Debug_Flag_And_Unrecognized_Value_Returns_False`
>   (`--debug yes` → `false`).
> - `With_Debug_Equals_Unrecognized_Value_Returns_True` → `With_Debug_Equals_Unrecognized_Value_Returns_False`
>   (`--debug=yes` → `false`).
> - Added `With_Debug_Equals_Empty_Value_Returns_False` (`--debug=` → `false`), closing the symmetric-case gap noted
>   in the original test review.
>
> Verification: full solution build succeeds with no problems; the `ArgumentHandlerTests` suite passes (30/30).
>
> The original finding is retained below.

**Files:** `src/DotBump/ArgumentHandler.cs:15-33`, `src/DotBump/ArgumentHandler.cs:60-65`,
`test/DotBump.Tests/ArgumentHandlerTests.cs:123-129`, `test/DotBump.Tests/ArgumentHandlerTests.cs:238-244`

`--debug` is declared as a boolean flag:

```csharp
[CommandOption("--debug")]
[DefaultValue(false)]
public bool Debug { get; set; }
```

Spectre.Console.Cli 0.57.2 accepts only the boolean values `true`/`false` for such a flag. It rejects an unknown
inline value and an empty string option value:

```text
dotnet dotbump sdk --debug=yes
=> Spectre.Console.Cli.CommandParseException: Flags cannot be assigned a value.

dotnet dotbump sdk --logfile=
=> Spectre.Console.Cli.CommandParseException: Expected an option value.
```

The pre-parser disagrees with both outcomes:

```csharp
// ArgumentHandler.cs
if (args[i].StartsWith("--debug=", StringComparison.OrdinalIgnoreCase))
{
    return !args[i]["--debug=".Length..].Equals("false", StringComparison.OrdinalIgnoreCase);
}
```

and:

```csharp
// ArgumentHandler.cs
if (args[i].StartsWith("--logfile=", StringComparison.OrdinalIgnoreCase))
{
    return args[i]["--logfile=".Length..];
}
```

Consequences:

- `With_Debug_Equals_Unrecognized_Value_Returns_True` locks in a result that can never reach command execution; the
  command aborts in Spectre with a parse error.
- `With_LogFile_Equals_Empty_Value_Returns_Empty_String` asserts behaviour for an input Spectre rejects at
  tokenization time.
- Because `LoggerConfigurator.Configure(args)` runs before `CommandApp.RunAsync(args)`, an unknown value such as
  `--debug=yes` enables debug logging for a run that is about to fail. This is fail-open. If a future command adds a
  positional argument, a typo like `--debug fals` could be consumed as that positional and the debug session would
  succeed, contrary to the previous fail-closed behaviour.

**Original suggestion:** align the pre-parser with Spectre's grammar — treat only `true`/`false` as explicit values,
and return `false` for anything else. For a bare flag followed by a separate value token, only `true` should enable
and `false` should disable. For `--logfile=`, decide explicitly whether the empty value should be treated as "no log
file" (current helper behaviour) or rejected. If the helper keeps returning `string.Empty`, add a CLI-level test
asserting the parser error so the two behaviours are documented together.

---

## L1. Option matching is duplicated between the two pre-parser methods — **PARTIALLY RESOLVED**

> **Resolution.** Shared constants (`DebugOption`, `DebugOptionPrefix`, `LogFileOption`, `LogFileOptionPrefix`) were
> introduced during the M1 fix, so the literals are no longer written twice and the slices are derived from a
> constant. The "extract one token matcher" idea remains open and was deliberately deferred; at current size a
> matcher is not warranted.

**File:** `src/DotBump/ArgumentHandler.cs:15-68`

Both methods repeat the same scan and the same string literals:

- `"--debug"` / `"--debug="` and `"--logfile"` / `"--logfile="` are each written twice.
- The prefix length used for slicing is recomputed from the literal (`["--debug=".Length..]`), so changing the literal
  without changing the slice is a silent maintenance trap.
- The "exact token then `=` token" handling is duplicated with different value semantics.

At current size this is low risk, but the file is now the single place where CLI syntax is re-implemented. A small
private matcher or at least shared constants removes the drift risk:

```csharp
private const string DebugOption = "--debug";
private const string LogFileOption = "--logfile";
private const string OptionValueSeparator = "=";
```

Ideally, extract one token matcher that both methods use, so the "exact option", "option=value" and "option value"
forms are implemented once.

---

## L2. New flag syntax is not covered by CLI-level tests — **OPEN**

**Files:** `test/DotBump.Tests/Commands/BumpSdk/BumpSdkCommandTests.cs:427`,
`test/DotBump.Tests/Commands/BumpTools/BumpToolsCommandTests.cs:331`,
`test/DotBump.Tests/Commands/BumpPackages/BumpPackagesCommandTests.cs:118`

The PR changes the documented examples from `--prerelease true`/`--debug true` to bare flags, but the command-level
integration tests continue to use the old explicit-value form:

```csharp
await app.RunAsync(["sdk", "--type", "major", "--prerelease", "true"]);
```

`ArgumentHandlerTests` cannot catch a mismatch between the pre-parser and Spectre, because it calls the helper
directly. `config.ValidateExamples()` only runs inside the application under `#if DEBUG`; it is not exercised by the
test host. As a result, the new bare-flag examples are not locked in by any automated test.

**Suggestion:** add at least one end-to-end `CommandApp.RunAsync` test per representative command for:

- `--debug` (bare),
- `--debug=true`,
- `--debug=false`,
- `--prerelease` (bare), replacing one of the existing `--prerelease true` invocations or adding a theory.

A small `[Theory]` over these argument lists would also ensure future changes to `ArgumentHandler` and the Spectre
option declarations stay in sync.

---

## Nits

### N1. Comment says "null or empty" but null is not checked — **RESOLVED**

> **Resolution.** Both comments in `ArgumentHandler.cs` (`IsDebugMode` and `LogFile`) now say "empty" only; no null
> guard was added, matching the callers (`args` is never null from `Main`).

**File:** `src/DotBump/ArgumentHandler.cs:9-13`

```csharp
// Check if args is null or empty
if (args.Length == 0)
```

`args` is never null from `Main`, and the pre-existing code did not guard it, so this is not a regression. Either
change the comment to "empty" or add `ArgumentNullException.ThrowIfNull(args)` if the helper is expected to be callable
independently. The same applies to `LogFile` (`ArgumentHandler.cs:42-45`).

---

## Test review

- The expanded `ArgumentHandlerTests` is well organised, follows the nested-group convention, and uses AAA and
  Shouldly consistently.
- Coverage is good for the normal forms: bare flag, explicit `true`/`false`, mixed casing, bare flag followed by
  another option, and both `=` forms.
- **Update:** the M1 gaps are addressed — unrecognized/empty inline values now assert fail-closed results, and the
  symmetric `--debug=` (empty inline value) case has been added.
- Remaining gap (L2): the actual CLI parsing path is not exercised, and the existing `--prerelease true` integration
  tests were not updated to the newly documented bare syntax.

---

## Security

- The fail-open behaviour described in the original M1 is now removed: `--debug yes`, `--debug=yes`, `--debug=` and
  similar invalid forms all fail closed before `CommandApp.RunAsync(args)`, so debug logging is never enabled for a
  run that Spectre proceeds to reject. The residual risk that a future positional argument shadows a typo'd value is
  also gone, since any unknown token after a bare `--debug` now prevents debug logging.
- No new secret handling, shell invocation or network surface was introduced by these changes.

---

## Verdict

**Approve.** ~~Approve with minor changes.~~ Valid documented usage (`--debug`, `--debug=true`, `--debug=false`,
`--logfile=path`) behaves correctly, and there is no blocking production defect. The blocking-for-merge item is
resolved: the pre-parser now agrees with Spectre on invalid values, the tests no longer assert unreachable command
outcomes, and the fail-open branch documented in M1 is gone. The end-to-end coverage from L2 remains a low-severity
follow-up for a later change. L1's remaining matcher-extraction suggestion and N1's guard alternative are optional
clean-ups.
