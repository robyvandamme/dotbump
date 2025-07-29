# DotBump

.NET Global Tool to automate dependency upgrade tasks in .NET solutions.

NOTE: Only tested on MacOS and Linux.

## Features

### Bump the .NET SDK version

Bump the global.json SDK version to the latest specified SDK version.

```text
DESCRIPTION:
Bump the global.json SDK version. Use the 'minor' type option to bump the SDK to
the latest minor or patch version for the current major version. Use the 'patch'
type option to bump the SDK to the latest patch version for the current major 
version. Use the 'lts' type option to bump the SDK to the latest LTS version. 

USAGE:
    dotbump sdk [OPTIONS]

EXAMPLES:
    dotbump sdk -o bump-sdk-result.json
    dotbump sdk -t patch -o bump-sdk-result.json
    dotbump sdk -t lts -o bump-sdk-result.json
    dotbump sdk --type minor -f ./other/global.json
    dotbump sdk --debug true --logfile log.txt

OPTIONS:
    -h, --help       Prints help information                                    
        --debug      Enable debug logging for troubleshooting                   
        --logfile    The file to send the log output to                         
    -t, --type       The bump type. Defaults to `minor`. Available options are  
                     `minor`, `patch` and `lts`                                 
    -f, --file       The global.json file to update. Defaults to `./global.json`
    -o, --output     Output file name. The name of the file to write the result 
                     to. The output format is json                              


```

### Bump .NET Tools Versions

Bumps .NET Tools versions to the latest minor or patch version.

#### Examples

```shell
dotnet dotbump tools
```

Enable debug logging:

````shell
dotnet dotbump tools --debug true
````

## Dependencies

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