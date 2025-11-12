# DotBump

.NET Tool to automate dependency upgrade tasks in .NET solutions.

[![Release](https://github.com/robyvandamme/dotbump/actions/workflows/release.yml/badge.svg)](https://github.com/robyvandamme/dotbump/actions/workflows/release.yml)
[![NuGet Version](https://img.shields.io/nuget/v/DotBump?color=004D81)](https://www.nuget.org/packages/DotBump/)
[![Pre Release](https://github.com/robyvandamme/dotbump/actions/workflows/pre-release.yml/badge.svg)](https://github.com/robyvandamme/dotbump/actions/workflows/pre-release.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=robyvandamme_dotbump&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=robyvandamme_dotbump)

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
type option to bump the SDK to the latest patch version for the current minor
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
        --debug            Enable debug logging for troubleshooting. Includes response data             
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
Bump the local .NET tools versions. Use the 'minor' type option to bump the tools to the latest minor or patch versions for the current major version. Use the 'patch' type option to bump the tools to 
the latest patch version for the current minor version. 

USAGE:
    dotnet dotbump tools [OPTIONS]

EXAMPLES:
    dotnet dotbump tools -o bump-tools-report.json
    dotnet dotbump tools -c other-nuget.config -t patch -o bump-tools-report.json
    dotnet dotbump tools --debug true --logfile log.txt

OPTIONS:
    -h, --help       Prints help information                                                                 
        --debug      Enable debug logging for troubleshooting. Includes response data                                                
        --logfile    The file to send the log output to                                                      
    -t, --type       The bump type. Defaults to `minor`. Available options are `minor` and `patch`           
    -o, --output     Output file name. The name of the file to write the result to. The output format is json
    -c, --config     The nuget config file to use. Defaults to `./nuget.config`      

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

#### Pre-releases

When the current version is a pre-release version, pre-release versions will be taken into account for new versions.


