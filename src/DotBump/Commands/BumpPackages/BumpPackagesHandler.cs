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
    public async Task<BumpReport> HandleAsync(BumpType bumpType, string repositoryPath, string nugetConfigPath)
    {
        logger.MethodStart(nameof(BumpPackagesHandler), nameof(HandleAsync), bumpType);

        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);

        var manifest = packageFileService.GetPackageManifest(repositoryPath);
        var bumpReport = new BumpReport(manifest, bumpType);

        var nuGetConfiguration = nugetConfigFileService.GetNuGetConfiguration(nugetConfigPath);
        var validationErrors = nugetConfigValidator.Validate(nuGetConfiguration);
        if (validationErrors.Any())
        {
            bumpReport.ReportErrors(validationErrors);
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
            var versions = manifest.Packages
                .Where(package => string.Equals(package.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
                .Select(package => package.SemanticVersion)
                .Where(version => version.IsValid)
                .ToList();

            if (versions.Count == 0)
            {
                logger.Debug(
                    "Skipping {PackageId} because none of its versions is a valid semantic version",
                    packageId);
                continue;
            }

            referenceVersions[packageId] = versions.OrderByDescending(version => version).First();
        }

        return referenceVersions;
    }
}
