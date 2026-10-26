# Code Review: `fix-debug` — bare boolean flags and `=` argument forms

**Date:** 2026-10-26  
**Scope:** branch `fix-debug` vs `main` (4 commits: `c327f7f`, `fb18211`, `a114271`, `6b0833a`). Code reviewed:
`src/DotBump/ArgumentHandler.cs`, `src/DotBump/CommandConfiguration.cs`,
`test/DotBump.Tests/ArgumentHandlerTests.cs`. README edits were treated as documentation and are out of scope except
where they describe the corresponding CLI behaviour.  
**Reviewer role:** .NET Tech Lead (code-only review).

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

| # | Severity | Finding |
|---|----------|---------|
| M1 | Medium | `IsDebugMode` accepts arbitrary `--debug=<value>` and `LogFile` accepts `--logfile=` even though Spectre rejects both; tests codify these unreachable command outcomes |
| L1 | Low | Token matching and prefix literals are duplicated between `IsDebugMode` and `LogFile` |
| L2 | Low | No CLI-level tests for the newly documented syntaxes; existing integration tests still use `--prerelease true` |
| N1 | Nit | Comment claims a null check that is not performed |

---

## M1. Pre-parser accepts forms that Spectre.Console.Cli rejects

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

**Suggestion:** align the pre-parser with Spectre's grammar — treat only `true`/`false` as explicit values, and return
`false` for anything else. For a bare flag followed by a separate value token, only `true` should enable and `false`
should disable:

```csharp
private const string DebugOption = "--debug";
private const string DebugPrefix = "--debug=";

if (args[i].Equals(DebugOption, StringComparison.OrdinalIgnoreCase))
{
    if (i < args.Length - 1)
    {
        if (args[i + 1].Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (args[i + 1].Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // A following option means the flag was used bare; any other token is a
        // value Spectre will reject.
        if (!args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            return false;
        }
    }

    return true;
}

if (args[i].StartsWith(DebugPrefix, StringComparison.OrdinalIgnoreCase))
{
    return args[i][DebugPrefix.Length..].Equals("true", StringComparison.OrdinalIgnoreCase);
}
```

For `--logfile=`, decide explicitly whether the empty value should be treated as "no log file" (current helper
behaviour) or rejected. If the helper keeps returning `string.Empty`, add a CLI-level test asserting the parser error so
the two behaviours are documented together. Update the two unit tests flagged above accordingly.

---

## L1. Option matching is duplicated between the two pre-parser methods

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

## L2. New flag syntax is not covered by CLI-level tests

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

### N1. Comment says "null or empty" but null is not checked

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
- Gaps are the ones described in M1 and L2: unreachable/invalid inputs are asserted as if they were valid, and the
  actual CLI parsing path is not exercised. The existing `--prerelease true` integration tests were not updated to the
  newly documented bare syntax.
- Minor: `--debug=` (empty inline value) has no unit test, while `--logfile=` does. If `--debug=` remains supported as
  "enabled", add the symmetric case; if it is treated as invalid, add a CLI-level rejection test.

---

## Security

- Debug logging includes response data (per the option description). The current fail-open branch is limited to runs
  that Spectre subsequently aborts, so there is no demonstrated leak today. Tightening the pre-parser as suggested in
  M1 removes the risk that a future positional argument turns a typo into a successful debug-enabled run.
- No new secret handling, shell invocation or network surface was introduced by these changes.

---

## Verdict

**Approve with minor changes.** Valid documented usage (`--debug`, `--debug=true`, `--debug=false`,
`--logfile=path`) behaves correctly, and there is no blocking production defect. Before merging, fix M1 so the
pre-parser and Spectre agree on invalid values and the tests stop asserting unreachable command outcomes; add the
end-to-end coverage from L2. L1 and N1 are optional clean-ups.
