# DotBump

.NET Global Tool to automate dependency upgrade tasks in .NET solutions.

[![Release](https://github.com/robyvandamme/dotbump/actions/workflows/release.yml/badge.svg)](https://github.com/robyvandamme/dotbump/actions/workflows/release.yml)
![NuGet Version](https://img.shields.io/nuget/v/DotBump)
[![Beta Release](https://github.com/robyvandamme/dotbump/actions/workflows/beta-release.yml/badge.svg)](https://github.com/robyvandamme/dotbump/actions/workflows/beta-release.yml)

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
