// Copyright © Roby Van Damme.

using DotBump.Commands.BumpPackages.DataModel;
using DotBump.Commands.BumpPackages.Interfaces;
using DotBump.Common;
using DotBump.NuGet.DataModel.PackageResolution;
using DotBump.NuGet.Interfaces;
using DotBump.Reports;
using Serilog;

namespace DotBump.Commands.BumpPackages;

internal class BumpPackagesHandler(
    IPackageFileService packageFileService,
    INuGetConfigFileService nugetConfigFileService,
    IPackageVersionResolver packageVersionResolver,
    INuGetConfigValidator nugetConfigValidator,
    ILogger logger) : IBumpPackagesHandler
{
    public async Task<BumpReport> HandleAsync(
        BumpType bumpType,
        string repositoryPath,
        string nugetConfigPath,
        IReadOnlyCollection<string>? excludedPaths = null)
    {
        logger.MethodStart(nameof(BumpPackagesHandler), nameof(HandleAsync), bumpType);

        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);

        var manifest = packageFileService.GetPackageManifest(repositoryPath, excludedPaths);
        var bumpReport = new BumpReport(manifest, bumpType);

        var nuGetConfiguration = nugetConfigFileService.GetNuGetConfiguration(nugetConfigPath);
        var validationErrors = nugetConfigValidator.Validate(nuGetConfiguration);
        if (validationErrors.Any())
        {
            bumpReport.ReportErrors(validationErrors);
            bumpReport.ReportWarnings(manifest.Warnings);
            logger.MethodReturn(nameof(BumpPackagesHandler), nameof(HandleAsync), bumpReport);
            return bumpReport;
        }

        var referenceVersions = GetReferenceVersions(manifest);
        var packagesToBump = referenceVersions
            .Select(reference => new PackageToBump(reference.Key, reference.Value))
            .ToList();

        var bestVersions = await packageVersionResolver
            .ResolveAsync(packagesToBump, bumpType, nuGetConfiguration)
            .ConfigureAwait(false);

        foreach (var (packageId, newVersion) in bestVersions)
        {
            // Never downgrade: only apply a target higher than every current occurrence.
            if (newVersion > referenceVersions[packageId])
            {
                manifest.SetVersion(packageId, newVersion.ToString());
            }
        }

        bumpReport.ReportChanges(manifest);

        if (bumpReport.HasChanges)
        {
            packageFileService.SavePackageManifest(manifest);
        }

        bumpReport.ReportWarnings(manifest.Warnings);

        logger.MethodReturn(nameof(BumpPackagesHandler), nameof(HandleAsync), bumpReport);

        return bumpReport;
    }

    /// <summary>
    /// Gets the distinct packages and the highest current version of each, which is used as the
    /// reference version to resolve a newer version from. Packages without a valid semantic
    /// version are skipped.
    /// </summary>
    private Dictionary<string, SemanticVersion> GetReferenceVersions(PackageManifest manifest)
    {
        var referenceVersions = new Dictionary<string, SemanticVersion>();

        foreach (var packageId in manifest.GetPackageIds())
        {
            var occurrences = manifest.Packages
                .Where(package => string.Equals(package.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var versions = occurrences
                .Select(package => package.SemanticVersion)
                .Where(version => version.IsValid)
                .ToList();

            if (versions.Count == 0)
            {
                var invalidVersions = string.Join(", ", occurrences.Select(package => $"'{package.Version}'"));
                var warning =
                    $"Skipping '{packageId}' because version(s) {invalidVersions} cannot be parsed as a semantic version.";
                logger.Warning("{Warning}", warning);
                manifest.AddWarning(warning);
                continue;
            }

            referenceVersions[packageId] = versions.OrderByDescending(version => version).First();
        }

        return referenceVersions;
    }
}
