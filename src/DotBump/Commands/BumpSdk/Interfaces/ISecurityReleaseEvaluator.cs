// Copyright © Roby Van Damme.

using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Common;

namespace DotBump.Commands.BumpSdk.Interfaces;

internal interface ISecurityReleaseEvaluator
{
    bool IncludesNewerSecurityRelease(
        SemanticVersion currentVersion,
        SemanticVersion targetVersion,
        ChannelReleases channelReleases);
}
