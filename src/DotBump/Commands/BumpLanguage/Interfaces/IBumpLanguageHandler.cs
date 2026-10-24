// Copyright © Roby Van Damme.

using DotBump.Reports;

namespace DotBump.Commands.BumpLanguage.Interfaces;

internal interface IBumpLanguageHandler
{
    Task<BumpReport> HandleAsync(
        string repositoryPath,
        string globalJsonPath,
        IReadOnlyCollection<string>? excludedPaths = null);
}
