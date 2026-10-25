// Copyright © Roby Van Damme.

using DotBump.Commands.BumpLanguage.DataModel;

namespace DotBump.Commands.BumpLanguage.Interfaces;

internal interface ILanguageFileService
{
    LanguageManifest GetLanguageManifest(
        string repositoryPath,
        IReadOnlyCollection<string>? excludedPaths = null);

    void SaveLanguageManifest(LanguageManifest manifest);
}
