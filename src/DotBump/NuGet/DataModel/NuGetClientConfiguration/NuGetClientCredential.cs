// Copyright © Roby Van Damme.

using Destructurama.Attributed;

namespace DotBump.NuGet.DataModel.NuGetClientConfiguration;

internal record NuGetClientCredential(string UserName, string Password)
{
    [LogMasked]
    public string UserName { get; } = UserName;

    [LogMasked]
    public string Password { get; } = Password;
}
