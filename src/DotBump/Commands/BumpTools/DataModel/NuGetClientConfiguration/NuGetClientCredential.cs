// Copyright © 2025 Roby Van Damme.

using Destructurama.Attributed;

namespace DotBump.Commands.BumpTools.DataModel.NuGetClientConfiguration;

internal record NuGetClientCredential(string UserName, string Password)
{
    [LogMasked]
    public string UserName { get; } = UserName;

    [LogMasked]
    public string Password { get; } = Password;
}
