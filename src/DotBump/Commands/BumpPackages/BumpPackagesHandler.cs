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
        IReadOnlyCollection<string>? excludedPaths = null,
        bool preRelease = false,
        IReadOnlyCollection<string>? skipPackages = null)
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

        var variantsByPackage = GetPackagesToBump(manifest, skipPackages);
        var packagesToBump = variantsByPackage.Keys.ToList();

        var bestVersions = await packageVersionResolver
            .ResolveAsync(packagesToBump, bumpType, nuGetConfiguration, preRelease)
            .ConfigureAwait(false);

        foreach (var (package, newVersion) in bestVersions)
        {
            // Never downgrade: only apply a target higher than every current occurrence in the variant.
            if (!(newVersion > package.CurrentVersion))
            {
                continue;
            }

            foreach (var variant in variantsByPackage[package])
            {
                manifest.SetVersion(variant, newVersion.ToString());
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
    /// Gets the package variants to resolve, keyed by package id and the highest current version in
    /// the variant. Variants that match a skip pattern and variants without a valid semantic version
    /// are skipped.
    /// </summary>
    private Dictionary<PackageToBump, List<PackageVariant>> GetPackagesToBump(
        PackageManifest manifest,
        IReadOnlyCollection<string>? skipPackages)
    {
        var variantsByPackage = new Dictionary<PackageToBump, List<PackageVariant>>();
        var skipMatcher = NamePatternMatcher.Create(skipPackages);
        var skippedPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var variant in manifest.GetVariants())
        {
            if (skipMatcher.TryMatch(variant.PackageId, out var matchedPattern))
            {
                if (skippedPackageIds.Add(variant.PackageId))
                {
                    var skipWarning =
                        $"Skipping '{variant.PackageId}' because it matches --skip-package pattern '{matchedPattern}'.";
                    logger.Warning("{Warning}", skipWarning);
                    manifest.AddWarning(skipWarning);
                }

                continue;
            }

            var referenceVersion = variant.HighestCurrentVersion;
            if (referenceVersion == null)
            {
                var invalidVersions = string.Join(", ", variant.Occurrences.Select(package => $"'{package.Version}'"));
                var condition = variant.Condition == null ? string.Empty : $" under condition '{variant.Condition}'";
                var warning =
                    $"Skipping '{variant.PackageId}'{condition} because version(s) {invalidVersions} cannot be parsed as a semantic version.";
                logger.Warning("{Warning}", warning);
                manifest.AddWarning(warning);
                continue;
            }

            var packageToBump = new PackageToBump(variant.PackageId, referenceVersion);

            if (!variantsByPackage.TryGetValue(packageToBump, out var variants))
            {
                variants = [];
                variantsByPackage[packageToBump] = variants;
            }

            variants.Add(variant);
        }

        return variantsByPackage;
    }
}
