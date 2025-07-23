# DotBump

.NET Global Tool to automate dependency upgrade tasks in .NET solutions.

## Features

### Bump the .NET SDK version

Bumps the global.json SDK version to the latest minor or patch version.

#### Examples

```shell
dotnet dotbump sdk
```

Enable debug logging:

````shell
dotnet dotbump sdk --debug true
````

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