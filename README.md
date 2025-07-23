# DotBump

.NET Global Tool to automate dependency upgrade tasks in .NET solutions.
Currently only tested on MacOS and Linux.

## Features

### Bump the .NET SDK version

Bump the global.json SDK version to the latest minor or patch version.

```text

DESCRIPTION:
Bump SDK Version

USAGE:
    dotbump sdk [OPTIONS]

EXAMPLES:
    dotbump sdk -f ./other/global.json -o bump-sdk-result.json --debug true

OPTIONS:
    -h, --help      Prints help information                                     
        --debug     Enable debug logging for troubleshooting                    
    -t, --type      The bump type. Defaults to `minor`. The option is ignored   
                    for now (only the minor option is implemented)              
    -f, --file      The global.json file to update. Defaults to `./global.json` 
    -o, --output    Output file name. The name of the file to write the result  
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