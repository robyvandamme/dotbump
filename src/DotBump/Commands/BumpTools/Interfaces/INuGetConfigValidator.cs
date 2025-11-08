// Copyright © 2025 Roby Van Damme.

using System.ComponentModel.DataAnnotations;
using DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface INuGetConfigValidator
{
    List<ValidationResult> Validate(NuGetConfig config);
}
