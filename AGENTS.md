# DotBump - Agent Guidelines

This document provides instructions, architectural context, and coding conventions for AI agents and automated contributors working in this repository.

---

## 1. Project Overview

**DotBump** is a .NET tool designed to automate dependency upgrade tasks within .NET solutions.

Key capabilities include:
- `dotnet dotbump sdk`: Bumps the .NET SDK version in `global.json` using the official .NET release index.
- `dotnet dotbump tools`: Bumps local .NET tools declared in `.config/dotnet-tools.json` using NuGet feeds (supporting pre-releases, unlisted package detection, and authenticated private feeds via environment variables).

### Technology Stack
- **Target Framework**: .NET 8 (`net8.0`), C# 12
- **CLI Framework**: [Spectre.Console.Cli](https://spectreconsole.net/)
- **Logging**: [Serilog](https://serilog.net/) (Console and File sinks, Destructurama attributed logging)
- **Build System**: [NUKE Build](https://nuke.build/)
- **Testing**: [xUnit](https://xunit.net/), [Shouldly](https://docs.shouldly.org/), [Moq](https://github.com/devlooped/moq), [Spectre.Console.Testing](https://spectreconsole.net/)
- **Versioning**: [GitVersion](https://gitversion.net/)

---

## 2. Repository Layout

- `src/DotBump/`: Primary application source code.
  - `Commands/`: Command definitions (`AsyncCommand<TSettings>`) and settings classes inheriting from `CommandSettings`.
  - `Commands/BumpSdk/`: SDK bump command, handler, services, and models (`global.json`, release index).
  - `Commands/BumpTools/`: Tools bump command, handler, and local tool manifest file services (`dotnet-tools.json`).
  - `NuGet/`: Reusable NuGet client services, configuration file services (`nuget.config`), validators, release finder, and data models.
  - `Common/`: Shared utilities (`SemanticVersion`, `VersionInfo`, `DotBumpException`, `LoggerExtensions`).
  - `Reports/`: Reporting models (`BumpReport`, `BumpResult`).
- `test/DotBump.Tests/`: Unit and integration test suite.
  - `Commands/`: Tests for SDK and Tools commands, handlers, and services.
  - `NuGet/`: Tests for NuGet clients, release finder, configuration validator, and configuration file services.
  - `Common/`: Tests for semantic versioning and argument handling.
  - `Data/`: Static test data (mock NuGet registrations, catalog pages, and `global.json` files).
  - `AGENTS.md`: Detailed test structure, hierarchy, naming conventions, and canonical examples.
  - `TESTS.md`: Guide to existing test cases and conventions for NuGet release testing.
- `build/`: NUKE build project (`Build.cs`, `Configuration.cs`).
- `artifacts/`: Output directory for packages, coverage reports, and test results (ignored in VCS).

---

## 3. Build, Test, and Verification Workflows

Always verify your changes before completing a task.

### Essential Commands
- **Restore Local Tools**:
  ```bash
  dotnet tool restore
  ```
- **Build Solution**:
  ```bash
  dotnet build
  ```
- **Run Tests**:
  ```bash
  dotnet test
  ```
- **Run Full NUKE Build Pipeline** (Restores tools, builds, runs tests with code coverage):
  ```bash
  ./build.sh Compile       # macOS / Linux
  ./build.cmd Compile      # Windows
  ```
- **Check Release Configuration (Strict Analyzers)**:
  ```bash
  dotnet build -c Release
  ```
  *Note*: In `Release` mode, `TreatWarningsAsErrors` and `CodeAnalysisTreatWarningsAsErrors` are enabled. Ensure all code passes without analyzer warnings.

---

## 4. Coding Standards & Conventions

### Language & Project Settings
- C# 12 with nullable reference types enabled (`<Nullable>enable</Nullable>`).
- Implicit usings enabled (`<ImplicitUsings>enable</ImplicitUsings>`).
- Enforce code style on build enabled (`<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`).

### File Header
Every `.cs` file must begin with the copyright header without a year:
```csharp
// Copyright © Roby Van Damme.
```

### Namespace & Usings
- Use file-scoped namespaces (e.g., `namespace DotBump.Commands.BumpSdk;`).
- Place `using` directives outside the namespace declaration.
- Sort `System.*` namespaces first.

### Code Style & Formatting
- **Indentation**: 4 spaces for C# files; 2 spaces for JSON, XML, `.props`, `.targets`, and shell scripts.
- **Type Inference**: Prefer `var` when type is apparent or per `.editorconfig` rules.
- **Constructors**: Prefer C# 12 primary constructors when declaring dependencies for classes.
- **Naming Conventions**:
  - Types, methods, properties, events: `PascalCase`.
  - Interfaces: `IPascalCase` (prefixed with `I`).
  - Private instance fields: `_camelCase` (prefixed with `_`).
  - Private static fields: `s_camelCase` (prefixed with `s_`).
  - Constants: `PascalCase`.
  - Parameters and local variables: `camelCase`.

### Logging & Error Handling
- Use Serilog structured logging.
- Do not use string interpolation within log messages; use Serilog message templates (e.g., `logger.Debug("Path: {Path}", path)`).
- Use `logger.MethodStart(nameof(ClassName), nameof(MethodName))` and `logger.MethodReturn(...)` where tracing method boundaries.
- For expected domain error conditions, throw or handle `DotBumpException`.

---

## 5. Testing Guidelines

- **Frameworks**: Use xUnit v2 for test execution, Shouldly (`.ShouldBe(...)`) for assertions, Moq (loose) for interface mocking, and `Spectre.Console.Testing` (`TestConsole`) for CLI command tests.
- **Structure & Layout**: Mirror production structure (`src/DotBump/Path/To/Class.cs` -> `test/DotBump.Tests/Path/To/ClassTests.cs`). Use one top-level `{ClassName}Tests` class with nested `public class` groups per method (`Constructor`, `Equals_`, etc.).
- **Method Naming**: Pascal_Snake_Case convention: `With_{Condition}_Returns_{Outcome}`, `{Condition}_Returns_{Outcome}`, or `With_{Condition}_Throws_{Exception}`.
- **Patterns**:
  - Follow Arrange-Act-Assert (AAA) with one logical outcome per test.
  - Do not use `SUT` or `ACT` variable names; use descriptive names.
  - Use Shouldly for all assertions (`ShouldSatisfyAllConditions`, `Should.Throw` / `Should.ThrowAsync`). Use Moq `Verify` for interactions.
  - Keep disk operations isolated using `LocalDirectory("./temp")` or cleanup helpers; never mutate repo files in place.
- **Test Data**: Add mock JSON payloads to `test/DotBump.Tests/Data/` (configured with `CopyToOutputDirectory`) rather than making network calls.
- See `test/DotBump.Tests/AGENTS.md` for complete patterns and canonical examples, and `test/DotBump.Tests/TESTS.md` for documented test cases and pinned tool versions.

---

## 6. Commit Guidelines

Follow the Conventional Commits specification recognized by GitVersion:
- `feat:` New features (triggers minor version bump).
- `fix:` Bug fixes (triggers patch version bump).
- `dep:` Dependency updates (triggers patch version bump).
- `chore:`, `docs:`, `refactor:`, `perf:`, `test:`, `build:`, `ci:`.
