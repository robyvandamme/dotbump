# dotnet-start

A collection of default settings for new C# .NET repositories

- [.editorconfig](https://editorconfig.org) and [.gitattributes](https://www.git-scm.com/docs/gitattributes) from [Roslyn](https://raw.githubusercontent.com/dotnet/roslyn/master/.editorconfig)
- [.gitignore](https://git-scm.com/docs/gitignore) adapted from [GitHub Visual Studio](https://github.com/github/gitignore/blob/master/VisualStudio.gitignore)
- Global C# language version and [Nullable](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references#nullable-contexts) configuration using [Directory.Build.props](https://docs.microsoft.com/en-us/visualstudio/msbuild/customize-your-build)
- Minimum .NET SDK version using [global.json](https://docs.microsoft.com/en-us/dotnet/core/tools/global-json)
- [StyleCop](https://github.com/DotNetAnalyzers/StyleCopAnalyzers) configuration to match `.editorconfig`

## Project Configuration

You can find example project configurations in the [test](https://github.com/build-ship-repeat/dotnet-start/tree/test) branch.

### Configure a .NET Project With Style

Add the relevant [StyleCop.Analyzers NuGet package](https://www.nuget.org/packages/StyleCop.Analyzers) to your project.
Then add the following to your .csproj file:

```xml

 <ItemGroup>
    <AdditionalFiles Include="..\..\stylecop.json">
      <Link>CodeStyle\stylecop.json</Link>
    </AdditionalFiles>
  </ItemGroup>

```

The above assumes your directory structure to be like `src/my-project`.


### Configure a .NET Project With Documentation

```xml

 <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

```

### Configure a .NET Project Without Documentation


```xml

 <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn),1573,1591,1712</NoWarn>
  </PropertyGroup>

```
