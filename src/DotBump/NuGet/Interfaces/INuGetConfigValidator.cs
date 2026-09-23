// Copyright © Roby Van Damme.

using System.ComponentModel.DataAnnotations;
using DotBump.NuGet.DataModel.NuGetConfiguration;

namespace DotBump.NuGet.Interfaces;

internal interface INuGetConfigValidator
{
    List<ValidationResult> Validate(NuGetConfig config);
}
