# DotBump

.NET Global Tool to automate dependency upgrade tasks in .NET solutions.

NOTE: Only tested on MacOS and Linux.

## Installation

```shell
dotnet tool install DotBump
```

## Features

### Bump the .NET SDK version

Bump the global.json SDK version to the latest specified SDK version based on the current release index.

```text
DESCRIPTION:
Bump the global.json SDK version. Use the 'minor' type option to bump the SDK to
the latest minor or patch version for the current major version. Use the 'patch'
type option to bump the SDK to the latest patch version for the current major 
version.

USAGE:
    dotnet dotbump sdk [OPTIONS]

EXAMPLES:
    dotnet dotbump sdk -o bump-sdk-result.json --security-only true
    dotnet dotbump sdk -t patch -o bump-sdk-result.json -s true
    dotnet dotbump sdk --type patch -f ./other/global.json
    dotnet dotbump sdk --debug true --logfile log.txt

OPTIONS:
    -h, --help             Prints help information                              
        --debug            Enable debug logging for troubleshooting             
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

### Bump .NET Tools Versions (Planned for v0.2)

Bumps .NET Tools versions to the latest minor or patch version.

#### Examples

```shell
dotnet dotbump tools
```

## Development Dependencies

### Application

* Serilog: trialing Serilog without Microsoft logging extensions, might change in the future.
* Spectre.Console.Cli: trialing. Probably the default for future CLI projects.

### Code Style

* [.editorconfig](.editorconfig): current default
* StyleCop Analyzers (Unstable): current default
* Roslynator.Analyzers: trialing

### Testing

* xUnit: current default
* Shouldly: current default
* Moq: current default
* Coverlet: current default

### Build

* Nuke: current default
* GitHub Actions: current default on GitHub

### Versioning

* GitVersion: current default with Conventional Commits. Not sure if this will cover all possible scenario's yet for a CLI.