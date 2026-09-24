# DotBump

.NET tool to automate dependency upgrade tasks in .NET solutions.

[![Release](https://github.com/robyvandamme/dotbump/actions/workflows/release.yml/badge.svg)](https://github.com/robyvandamme/dotbump/actions/workflows/release.yml)
[![NuGet Version](https://img.shields.io/nuget/v/DotBump?color=004D81)](https://www.nuget.org/packages/DotBump/)
[![Pre Release](https://github.com/robyvandamme/dotbump/actions/workflows/pre-release.yml/badge.svg)](https://github.com/robyvandamme/dotbump/actions/workflows/pre-release.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=robyvandamme_dotbump&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=robyvandamme_dotbump)

## Requirements

DotBump targets .NET 8 and requires .NET 8 or later.

## Installation

```shell
dotnet tool install DotBump
```

## Usage

```text
USAGE:
    dotnet dotbump [OPTIONS] <COMMAND>

COMMANDS:
    sdk       Bump the global.json SDK version
    tools     Bump the local .NET tools versions
    packages  Bump the NuGet package versions

OPTIONS:
    -h, --help       Prints help information
    -v, --version    Prints version information
```

## Features

### Bump the .NET SDK version

Bump the global.json SDK version to the latest specified SDK version based on the current release index.

```text
DESCRIPTION:
Bump the global.json SDK version. Use the 'minor' type option to bump the SDK to
the latest minor or patch version for the current major version. Use the 'patch'
type option to bump the SDK to the latest patch version for the current minor
version.

USAGE:
    dotnet dotbump sdk [OPTIONS]

EXAMPLES:
    dotnet dotbump sdk
    dotnet dotbump sdk --type patch
    dotnet dotbump sdk --file ./other/global.json --output bump-sdk-report.json
    dotnet dotbump sdk --security-only true --debug true --logfile bump-sdk-log.txt


OPTIONS:
    -h, --help             Prints help information                              
        --debug            Enable debug logging for troubleshooting. Includes   
                           response data                                        
        --logfile          The file to send the log output to                   
    -t, --type             The bump type. Defaults to `minor`. Available options
                           are `minor` and `patch`                              
    -f, --file             The global.json file to update. Defaults to          
                           `./global.json`                                      
    -o, --output           Output file name. The name of the file to write the  
                           result to. The output format is json                 
    -s, --security-only    Only bump the version if the new release is a        
                           security release. Defaults to false      

```

### Bump the Local .NET Tools versions

```text

DESCRIPTION:
Bump the local .NET tools versions. Use the 'minor' type option to bump the tools to
the latest minor or patch versions for the current major version. Use the 'patch' type 
option to bump the tools to the latest patch version for the current minor version. 

USAGE:
    dotnet dotbump tools [OPTIONS]

EXAMPLES:
    dotnet dotbump tools
    dotnet dotbump tools --type patch
    dotnet dotbump tools --config ./custom-nuget.config --output bump-tools-report.json
    dotnet dotbump tools --debug true --logfile bump-tools-log.txt


OPTIONS:
    -h, --help       Prints help information                                                                 
        --debug      Enable debug logging for troubleshooting. Includes response data                        
        --logfile    The file to send the log output to                                                      
    -t, --type       The bump type. Defaults to `minor`. Available options are `minor` and `patch`           
    -o, --output     Output file name. The name of the file to write the result to. The output format is json
    -c, --config     The nuget config file to use. Defaults to `./nuget.config`     

```

#### Pre-releases

If the current .NET tool version is a pre-release version, pre-release versions will be taken into account for new 
versions, but stable versions will be preferred. If the current .NET tool version is a stable version, only stable 
versions will be taken into account for new versions.

Examples:

```text
Current version: 1.0.0-beta.1
Available versions: 1.0.0-beta.2, 1.0.0
Bump to: 1.0.0
```

```text
Current version: 1.0.0-beta.1
Available versions: 1.0.1-beta.1, 1.0.0
Bump to: 1.0.0
```

```text
Current version: 1.0.1-beta.1
Available versions: 1.0.1-beta.2, 1.0.0
Bump to: 1.0.1-beta.2
```

#### Private Feeds

Private feeds are supported using environment variables.

See [credentials in NuGet config files](https://learn.microsoft.com/en-us/nuget/consume-packages/consuming-packages-authenticated-feeds#credentials-in-nugetconfig-files) 
and [using environment variables](https://learn.microsoft.com/en-us/nuget/reference/nuget-config-file#using-environment-variables) 
for more information.

The username and plain text password in a nuget.config file can use an environment variable by adding % to the 
beginning and end of the environment variable name you would like to use.

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

### Bump the NuGet Package versions

Bump the versions of NuGet packages referenced in a repository.

Use the `minor` type option to bump the packages to the latest minor or patch version for the current major
version. Use the `patch` type option to bump the packages to the latest patch version for the current minor version.

The command scans the directory passed via `--path` (defaults to the current directory) recursively and takes the
following files into account:

| File | Contents |
| ---- | -------- |
| `*.csproj`, `*.fsproj`, `*.vbproj` | `<PackageReference>` entries, where the version can be a `Version` attribute, a `<Version>` child element or a `VersionOverride` attribute. |
| `Directory.Packages.props` | Central Package Management `<PackageVersion>` and `<GlobalPackageReference>` entries. |
| `Directory.Build.props`, `Directory.Build.targets` | Shared `<PackageReference>`, `<PackageVersion>` and `<GlobalPackageReference>` entries. |

Directories named `bin`, `obj`, `.git`, `.vs` and `node_modules` are skipped.

```text
DESCRIPTION:
Bump the NuGet package versions. Use the 'minor' type option to bump the
packages to the latest minor or patch versions for the current major version.
Use the 'patch' type option to bump the packages to the latest patch version for
the current minor version.

USAGE:
    dotnet dotbump packages [OPTIONS]

EXAMPLES:
    dotnet dotbump packages
    dotnet dotbump packages --type patch
    dotnet dotbump packages --path ./src --config ./custom-nuget.config --output bump-packages-report.json
    dotnet dotbump packages --debug true --logfile bump-packages-log.txt

OPTIONS:
    -h, --help       Prints help information
        --debug      Enable debug logging for troubleshooting. Includes response data
        --logfile    The file to send the log output to
    -t, --type       The bump type. Defaults to `minor`. Available options are `minor` and `patch`
    -o, --output     Output file name. The name of the file to write the result to. The output format is json
    -c, --config     The nuget config file to use. Defaults to `./nuget.config`
    -p, --path       The root directory to scan. Defaults to the current directory
```

The output file (`--output`) uses the same JSON report format as the `sdk` and `tools` commands, and the command
returns exit code `1` when an error occurs (for example an invalid NuGet configuration).

#### How versions are bumped

* All occurrences of the same package id across the repository are bumped to a single target version.
* The **highest** currently referenced version is used as the starting point for resolving a newer version, so an
  occurrence is never downgraded. If an occurrence happens to be below the resolved target it is upgraded to the
  target as well.
* If no newer version is found, the package is left unchanged.
* Pre-release handling matches the local tools: if the current version is a pre-release, pre-release versions are
  considered but stable versions are preferred; otherwise only stable versions are considered.
* Only files that actually change are written back, and only the version value is changed; everything else (attribute
  quoting, entities, attribute layout, XML declaration, encoding, byte order mark and line endings) is preserved.

#### Private feeds

Private feeds are configured through `nuget.config` in the same way as for the local tools. See
[Private Feeds](#private-feeds) above.

#### Limitations (beta)

The following are intentionally **not supported yet**. Packages that use them are skipped, leaving the existing value
untouched:

* Version **ranges** (e.g. `Version="[1.0.0, 2.0.0)"`).
* **Floating** versions (e.g. `Version="1.2.*"`).
* MSBuild **version properties** (e.g. `Version="$(MyPackageVersion)"`).

Skipped packages are only logged at debug level; run with `--debug true` to see them. They are **not** included in the
output report.

Other current limitations:

* MSBuild `Condition` attributes are ignored, so conditionally referenced packages are treated as active.
* Symlinked files and directories are skipped; only regular files are scanned.
* Explicit `<Import ... />` elements are not followed; only the conventional `Directory.Build.props`,
  `Directory.Build.targets`, `Directory.Packages.props` and project files are scanned.
* `packages.lock.json` files are not updated.
* A `<PackageReference>` without a version (the usual Central Package Management setup) is ignored, because its
  version is defined by the corresponding `<PackageVersion>` in `Directory.Packages.props`, which is scanned instead.

#### Beta feedback

This feature is new. If a package or file format in your repository is not detected correctly, or a version is
resolved unexpectedly, please open an issue with a minimal reproduction (the relevant project/props file and the
package you expected to be bumped) and a debug log file. You can generate one by running the command with
`--debug true --logfile bump-packages-log.txt`, for example:

```shell
dotnet dotbump packages --debug true --logfile bump-packages-log.txt
```

Please review the log before attaching it and redact anything sensitive.
