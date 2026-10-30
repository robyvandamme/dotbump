# DotBump

.NET tool to automate dependency upgrades and project file maintenance in .NET solutions.

[![Release](https://github.com/robyvandamme/dotbump/actions/workflows/release.yml/badge.svg)](https://github.com/robyvandamme/dotbump/actions/workflows/release.yml)
[![NuGet Version](https://img.shields.io/nuget/v/DotBump?color=004D81)](https://www.nuget.org/packages/DotBump/)
[![Pre Release](https://github.com/robyvandamme/dotbump/actions/workflows/pre-release.yml/badge.svg)](https://github.com/robyvandamme/dotbump/actions/workflows/pre-release.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=robyvandamme_dotbump&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=robyvandamme_dotbump)

## Requirements

DotBump targets .NET 10 and requires .NET 10 or later.

## Installation

```shell
dotnet tool install DotBump
```

## Usage

```text
USAGE:
    dotnet dotbump [OPTIONS] <COMMAND>

COMMANDS:
    sdk         Bump the global.json SDK version
    tools       Bump the local .NET tools versions
    packages    Bump the NuGet package and project SDK versions
    frameworks  Bump the target framework versions
    language    Bump the C# language version

OPTIONS:
    -h, --help       Prints help information
    -v, --version    Prints version information
```

## Automated dependency updates

See the
[dependency-update](https://github.com/robyvandamme/dotbump/blob/main/.github/workflows/dependency-update.yml) and
[major dependency-update](https://github.com/robyvandamme/dotbump/blob/main/.github/workflows/dependency-update-major.yml)
workflows for end-to-end examples that bump the SDK, local tools and NuGet packages and open a pull request from the
markdown reports. The major workflow additionally aligns target frameworks and the C# language version to the SDK.

## Commands

### sdk

```text
DESCRIPTION:
Bump the global.json SDK version. Use the 'minor' type option to bump the SDK to
the latest minor or patch version for the current major version. Use the 'patch'
type option to bump the SDK to the latest patch version for the current minor
version. Use the 'major' type option to bump the SDK to the latest version,
including versions with a higher major version. Use '--prerelease' with the
'major' type to include pre-release versions.

USAGE:
    dotnet dotbump sdk [OPTIONS]

EXAMPLES:
    dotnet dotbump sdk
    dotnet dotbump sdk --type patch
    dotnet dotbump sdk --type major
    dotnet dotbump sdk --type major --prerelease
    dotnet dotbump sdk --file ./other/global.json --output bump-sdk-report.json
    dotnet dotbump sdk --security-only --debug --logfile bump-sdk-log.txt

OPTIONS:
    -h, --help             Prints help information
    -t, --type             The bump type. Defaults to `minor`. Available options
                           are `minor`, `patch` and `major`
    -f, --file             The global.json file to update. Defaults to
                           `./global.json`
    -o, --output           Output file name. The name of the file to write the
                           result to. The output format is inferred from the
                           file extension: `.json` or `.md`
    -s, --security-only    Only bump the version if the new release is a
                           security release. Defaults to false
        --prerelease       Include pre-release SDK versions when bumping. Only
                           applies to `major` bumps. Defaults to false
        --debug            Enable debug logging for troubleshooting. Includes
                           response data
        --logfile          The file to send the log output to
```

### tools

```text
DESCRIPTION:
Bump the local .NET tools versions. Use the 'minor' type option to bump the
tools to the latest minor or patch versions for the current major version. Use
the 'patch' type option to bump the tools to the latest patch version for the
current minor version. Use the 'major' type option to bump the tools to the
latest version, including versions with a higher major version. Use
'--prerelease' with the 'major' type to include pre-release versions.

USAGE:
    dotnet dotbump tools [OPTIONS]

EXAMPLES:
    dotnet dotbump tools
    dotnet dotbump tools --type patch
    dotnet dotbump tools --type major
    dotnet dotbump tools --type major --prerelease
    dotnet dotbump tools --type major --skip-tool "dotnet-*"
    dotnet dotbump tools --manifest ./other/dotnet-tools.json
    dotnet dotbump tools --config ./custom-nuget.config --output bump-tools-report.json
    dotnet dotbump tools --debug --logfile bump-tools-log.txt

OPTIONS:
    -h, --help       Prints help information
    -t, --type       The bump type. Defaults to `minor`. Available options are
                     `minor`, `patch` and `major`
    -o, --output     Output file name. The name of the file to write the result
                     to. The output format is inferred from the file extension:
                     `.json` or `.md`
    -c, --config     The nuget config file to use. Defaults to `./nuget.config`
    -m, --manifest   The tools manifest file to update. Defaults to
                     `./dotnet-tools.json`, falling back to
                     `./.config/dotnet-tools.json`
        --skip-tool  A tool id or pattern (`*` and `?` supported) to skip when
                     bumping. Can be specified multiple times or as a
                     comma-separated list
        --prerelease Include pre-release versions when bumping. Only applies to
                     `major` bumps. Defaults to false
        --debug      Enable debug logging for troubleshooting. Includes response
                     data
        --logfile    The file to send the log output to
```

### packages

```text
DESCRIPTION:
Bump the NuGet package versions and versioned MSBuild project SDK references.
Use the 'minor' type option to bump the packages to the latest minor or patch
versions for the current major version. Use the 'patch' type option to bump the
packages to the latest patch version for the current minor version. Use the
'major' type option to bump the packages to the latest version, including
versions with a higher major version. Use '--prerelease' with the 'major' type
to include pre-release versions.

USAGE:
    dotnet dotbump packages [OPTIONS]

EXAMPLES:
    dotnet dotbump packages
    dotnet dotbump packages --type patch
    dotnet dotbump packages --type major
    dotnet dotbump packages --type major --prerelease
    dotnet dotbump packages --type major --prerelease --skip-package "Microsoft.*"
    dotnet dotbump packages --path ./src --config ./custom-nuget.config --output bump-packages-report.json
    dotnet dotbump packages --exclude ./test/DotBump.Tests/Data
    dotnet dotbump packages --debug --logfile bump-packages-log.txt

OPTIONS:
    -h, --help           Prints help information
    -t, --type           The bump type. Defaults to `minor`. Available options
                         are `minor`, `patch` and `major`
    -c, --config         The nuget config file to use. Defaults to `./nuget.config`
        --skip-package   A package id or pattern (`*` and `?` supported) to skip
                         when bumping. Can be specified multiple times or as a
                         comma-separated list
        --prerelease     Include pre-release versions when bumping. Only applies
                         to `major` bumps. Defaults to false
    -p, --path           The root directory to scan. Defaults to the current
                         directory
    -e, --exclude        A directory to exclude from the scan. Can be specified
                         multiple times. Relative paths are resolved against the
                         current directory, not the scan root given by --path
    -o, --output         Output file name. The name of the file to write the
                         result to. The output format is inferred from the file
                         extension: `.json` or `.md`
        --debug          Enable debug logging for troubleshooting. Includes
                         response data
        --logfile        The file to send the log output to
```

#### Scanned files

The command scans the directory passed via `--path` (defaults to the current directory) recursively and takes the
following files into account:

| File | Contents |
| ---- | -------- |
| `*.csproj`, `*.fsproj`, `*.vbproj` | `<PackageReference>` entries, where the version can be a `Version` attribute, a `<Version>` child element or a `VersionOverride` attribute. Versioned project SDK references in the root `Sdk` attribute and in `<Sdk>` elements. |
| `Directory.Packages.props` | Central Package Management `<PackageVersion>` and `<GlobalPackageReference>` entries. |
| `Directory.Build.props`, `Directory.Build.targets` | Shared `<PackageReference>`, `<PackageVersion>` and `<GlobalPackageReference>` entries. |

The package id is read from the `Include` attribute, or, for override entries, the `Update` attribute. The version is
read from a `Version` attribute, a `<Version>` child element or a `VersionOverride` attribute. The effective
`Condition` is the element's own `Condition` combined with the conditions of its enclosing elements; occurrences with
different conditions are treated as independent package variants.

Condition matching is textual. Values are trimmed, enclosing conditions are joined with `&&`, and the result is
compared ordinally. Semantically equivalent expressions with different formatting or structure — for example
`'$(TargetFramework)'=='net10.0'` versus `'$(TargetFramework)' == 'net10.0'` — are therefore separate variants.
Conditions using `||`, negation, or implied conditions are not simplified.

Project SDK references are supported in both forms:

```xml
<Project Sdk="Aspire.AppHost.Sdk/13.0.1">
```

```xml
<Project Sdk="Microsoft.NET.Sdk;Aspire.AppHost.Sdk/13.0.1">
  <Sdk Name="Aspire.AppHost.Sdk" Version="13.0.1" />
</Project>
```

The SDK id is resolved as a NuGet package, so `--skip-package` applies to SDK references too. Only SDK references with
an explicit version are bumped; unversioned SDKs such as `Microsoft.NET.Sdk` are ignored. The version is the only part
of the SDK reference that is rewritten. SDK references declared on `<Import ... />` elements are not detected yet; use
the `Sdk` attribute or an `<Sdk>` element.

#### Version resolution

* Occurrences of the same package id are grouped by their effective MSBuild `Condition` and each group is bumped
  independently. Occurrences with the same condition are bumped to a single target version; occurrences with different
  conditions (for example per-target-framework `PackageReference` entries) are resolved against their own current
  version and never collapsed into one version.
* The **highest** currently referenced version within a group is used as the starting point for resolving a newer
  version, so an occurrence is never downgraded. If an occurrence in the same group happens to be below the resolved
  target it is upgraded to the target as well.
* If no newer version is found, the package group is left unchanged.

#### Limitations

The following are intentionally **not supported yet**. Packages that use them are skipped, leaving the existing value
untouched:

* Version **ranges** (e.g. `Version="[1.0.0, 2.0.0)"`).
* **Floating** versions (e.g. `Version="1.2.*"`).
* MSBuild **version properties** (e.g. `Version="$(MyPackageVersion)"`).

Skipped packages are listed under `warnings` in the output report, so automated runs (for example a dependency-update
PR that embeds the report) can surface them without extra debugging. They are also logged at warning level; run with
`--debug` to see them in the log.

Other current limitations:

* MSBuild `Condition` expressions are not evaluated, so conditionally referenced packages are treated as active.
  Conditions are still used as part of the package identity, which keeps per-target-framework versions independent.
* Explicit `<Import ... />` elements are not followed; only the conventional `Directory.Build.props`,
  `Directory.Build.targets`, `Directory.Packages.props` and project files are scanned. Versioned SDK references
  declared on `<Import ... />` elements (for example
  `<Import Project="Sdk.props" Sdk="My.Custom.Sdk" Version="1.2.3" />`) are not bumped yet either.
* `packages.lock.json` files are not updated.
* A `<PackageReference>` without a version (the usual Central Package Management setup) is ignored, because its
  version is defined by the corresponding `<PackageVersion>` in `Directory.Packages.props`, which is scanned instead.
* `Update=` override entries are currently treated the same as `Include` entries: all occurrences of the same package
  id and effective condition are unified to a single target version. The intended semantics — in particular overriding
  a package that is also referenced with `Include` — are not decided yet, so treat `Update=` as unsupported for now.

### frameworks

```text
DESCRIPTION:
Bump the target framework versions. Aligns `<TargetFramework>` values to the SDK
major version in global.json

USAGE:
    dotnet dotbump frameworks [OPTIONS]

EXAMPLES:
    dotnet dotbump frameworks
    dotnet dotbump frameworks --file ./other/global.json --path ./src
    dotnet dotbump frameworks --exclude ./test --output bump-frameworks-report.md
    dotnet dotbump frameworks --debug --logfile bump-frameworks-log.txt

OPTIONS:
    -h, --help       Prints help information
    -f, --file       The global.json file used to determine the target versions. Defaults to `./global.json`
    -p, --path       The root directory to scan. Defaults to the current directory
    -e, --exclude    A directory to exclude from the scan. Can be specified multiple
                     times. Relative paths are resolved against the current
                     directory, not the scan root given by --path
    -o, --output     Output file name. The name of the file to write the result to. The output format is inferred
                     from the file extension: `.json` or `.md`
        --debug      Enable debug logging for troubleshooting. Includes response data
        --logfile    The file to send the log output to
```

The command reads `sdk.version` from `--file` (defaults to `./global.json`), derives `net{major}.0`, and updates
`<TargetFramework>` in `*.csproj`, `*.fsproj`, `*.vbproj`, `Directory.Build.props` and `Directory.Build.targets`.
Platform suffixes such as `-windows` and `-ios` are preserved, so only the framework version changes.

The framework is aligned to the SDK major in both directions: a project targeting a lower or higher major than the SDK
is moved to the SDK major, keeping the project and SDK in sync.

#### Limitations

* Only `<TargetFramework>` is supported for now. Files that contain `<TargetFrameworks>` are skipped with a warning, so
  multi-target projects are left untouched.
* `netstandard*`, `net4x`, `netcoreapp*`, MSBuild property expressions such as `$(TargetFramework)` and other
  unsupported values are left unchanged.
* SDK versions below .NET 5 are rejected because those SDKs use `netcoreapp*` target frameworks rather than the modern
  `netX.0` format.

### language

```text
DESCRIPTION:
Bump the C# language version. The language version is derived from the SDK major
version in global.json as SDK major + 4. Only explicit numeric LangVersion values
in C# projects and shared MSBuild files are bumped

USAGE:
    dotnet dotbump language [OPTIONS]

EXAMPLES:
    dotnet dotbump language
    dotnet dotbump language --file ./other/global.json --path ./src
    dotnet dotbump language --output bump-language-report.json
    dotnet dotbump language --debug --logfile bump-language-log.txt

OPTIONS:
    -h, --help       Prints help information
    -f, --file       The global.json file used to determine the target versions. Defaults to `./global.json`
    -p, --path       The root directory to scan. Defaults to the current directory
    -e, --exclude    A directory to exclude from the scan. Can be specified multiple
                     times. Relative paths are resolved against the current
                     directory, not the scan root given by --path
    -o, --output     Output file name. The name of the file to write the result to. The output format is inferred
                     from the file extension: `.json` or `.md`
        --debug      Enable debug logging for troubleshooting. Includes response data
        --logfile    The file to send the log output to
```

The command reads `sdk.version` from `--file` (defaults to `./global.json`) and derives the target language version as
the SDK major plus four, formatted as `major.0`: .NET 5 maps to C# 9.0, .NET 8 to C# 12.0 and .NET 10 to C# 14.0. Only
`*.csproj`, `Directory.Build.props` and `Directory.Build.targets` are scanned; F# and Visual Basic projects are
ignored.

Only existing numeric `<LangVersion>` values are considered, and they are bumped upward only.

#### Limitations

* Values such as `latest`, `latestMajor`, `preview`, `default`, MSBuild property expressions such as
  `$(LangVersion)` and missing values are left unchanged.
* SDK versions below .NET 5 are rejected because the SDK major cannot be mapped reliably to a C# version.

## Common behaviour

### Version selection and pre-releases

The `sdk`, `tools` and `packages` commands share the same version selection rules.

* `minor` and `patch` stay within the current major or minor version.
* `major` removes the major-version cap, consistent with how `minor` includes patch releases. It therefore also selects
  a newer minor or patch version when no higher major version is available, and only leaves the entry unchanged when
  nothing newer exists.
* If the current version is stable, only stable versions are considered. If the current version is a pre-release,
  pre-release versions are considered but stable versions are preferred.
* Use `--prerelease` with `major` to include pre-release versions and select the highest version available, whether
  stable or pre-release.

### Skipping updates

Tools and packages can be excluded from a bump with `--skip-tool` and `--skip-package` respectively. Skipped entries
are left at their current version and are not looked up on the NuGet feeds at all.

The options accept exact ids and wildcard patterns. `*` matches zero or more characters and `?` matches exactly one
character; matching is case-insensitive. Patterns can be specified multiple times or as a comma-separated list:

```shell
dotnet dotbump tools --type major --skip-tool "dotnet-*" --skip-tool "GitVersion.*"
dotnet dotbump tools --type major --skip-tool "dotnet-*,GitVersion.*"
dotnet dotbump packages --type major --prerelease --skip-package "Microsoft.*,System.*"
```

Quote patterns in bash/zsh so the shell does not expand them as file globs. Every skipped entry is listed under
`warnings` in the output report, for example:

```markdown
### Warnings

- Skipping tool 'dotnet-ef' because it matches --skip-tool pattern 'dotnet-*'.
- Skipping 'Microsoft.Extensions.Logging' because it matches --skip-package pattern 'Microsoft.*'.
```

### Private NuGet feeds

Private feeds are supported for `tools` and `packages` using environment variables.

If the default `./nuget.config` file does not exist, the `tools` and `packages` commands use a default configuration
with only the official nuget.org source (`https://api.nuget.org/v3/index.json`). User and machine-wide NuGet
configuration are not consulted; a nuget.config passed explicitly via `--config` must exist.

See [credentials in NuGet config files](https://learn.microsoft.com/en-us/nuget/consume-packages/consuming-packages-authenticated-feeds#credentials-in-nugetconfig-files)
and [using environment variables](https://learn.microsoft.com/en-us/nuget/reference/nuget-config-file#using-environment-variables)
for more information.

The username and plain text password in a nuget.config file can use an environment variable by adding % to the
beginning and end of the environment variable name you would like to use. Literal credentials are not supported.

Before any feed is queried, the nuget.config file is validated and the command exits with code `1` when any of the
following does not hold:

* the configuration declares at least one package source;
* every package source is an HTTPS URL and declares `protocolVersion="3"`;
* every credential value is a `%ENV_VAR%` placeholder, as shown in the example below;
* every credential key is `UserName` or `ClearTextPassword`.

Validation errors are also written to the `errors` array of the report when `--output` is used; a configuration
without any package sources aborts before a report is written.

Example:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="myfeed" value="https://nuget.pkg.github.com/robyvandamme/index.json" protocolVersion="3" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceCredentials>
    <myfeed>
      <add key="Username" value="%PRIVATE_GITHUB_FEED_USER%" />
      <add key="ClearTextPassword" value="%PRIVATE_GITHUB_FEED_PASSWORD%" />
    </myfeed>
  </packageSourceCredentials>
</configuration>
```

### Logging

All commands accept `--debug` and `--logfile`.

`--debug` enables debug logging for troubleshooting, which includes response data. It can be passed as a bare
flag; `--debug false` explicitly disables it.

### Scanning and file handling

The repository-scanning commands (`packages`, `frameworks` and `language`) scan the directory passed via `--path`
recursively. Directories named `bin`, `obj`, `.git`, `.vs` and `node_modules` are skipped. Additional directories can
be skipped with `--exclude` (repeatable). Like `--path`, each `--exclude` path is resolved relative to the current
working directory, not to `--path`. Path matching is case-sensitive: on a case-insensitive filesystem the directory
must be passed with its exact on-disk casing, otherwise it is not excluded. Symlinked files and directories are
skipped; only regular files are scanned.

Only files that actually change are written back, and only the affected value is changed; everything else (attribute
quoting, entities, attribute layout, XML declaration, encoding, byte order mark and line endings) is preserved.

### Reports

All commands accept `--output` to write a report. The format is inferred from the file extension:

* `.json` — the full, machine-readable report, including every entry, its old and new version, and the `warnings`
  and `errors` arrays. For `packages` there is one entry per package id and effective condition, using the highest
  currently referenced version in that group as the old version; all occurrences in the group are updated together.
  Conditional entries include a `condition` property. This is the default for any other extension.
* `.md` (or `.markdown`) — a concise markdown report intended as the content for an automated dependency-update pull
  request. It lists only what changed, plus any warnings and errors. When there is nothing to report, an existing
  markdown report at that path is removed, so a reused output path never yields stale content.

All commands return exit code `1` when an error occurs (for example an invalid NuGet configuration).
Invalid arguments — for example a nonexistent `--manifest`, `--config`, `--path` or `--exclude` path, an empty
`--skip-tool`/`--skip-package` value, or an unknown option — are rejected before the command runs and return exit
code `-1`.

```markdown
## Packages

- Newtonsoft.Json: 12.0.1 → 13.0.3
- Serilog: 3.0.0 → 3.0.1

### Warnings

- Skipping 'Floating.Package' because version '1.2.*' is not supported.
```
