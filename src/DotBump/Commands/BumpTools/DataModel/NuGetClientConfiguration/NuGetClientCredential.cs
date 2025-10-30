// Copyright © 2025 Roby Van Damme.

using Destructurama.Attributed;

namespace DotBump.Commands.BumpTools.DataModel.NuGetClientConfiguration;

internal record NuGetClientCredential
{
    [LogMasked(ShowFirst = 3)]
    public string UserName { get; set; }

    [LogMasked]
    public string Password { get; set; }
}
