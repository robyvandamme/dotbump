// Copyright © Roby Van Damme.

using System.Collections.ObjectModel;
using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Common;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpSdk;

public class SecurityReleaseEvaluatorTests
{
    public class IncludesNewerSecurityRelease
    {
        [Fact]
        public void With_Security_Release_Between_Current_And_Target_Returns_True()
        {
            var channelReleases = CreateChannelReleases(
                CreateRelease(security: false, "10.0.404", "10.0.115"),
                CreateRelease(security: true, "10.0.403", "10.0.114"),
                CreateRelease(security: true, "10.0.402", "10.0.113"));
            var evaluator = new SecurityReleaseEvaluator(new Mock<ILogger>().Object);

            var result = evaluator.IncludesNewerSecurityRelease(
                new SemanticVersion("10.0.402"),
                new SemanticVersion("10.0.404"),
                channelReleases);

            result.ShouldBeTrue();
        }

        [Fact]
        public void With_Security_Release_Only_At_Current_Version_Returns_False()
        {
            var channelReleases = CreateChannelReleases(
                CreateRelease(security: false, "10.0.404"),
                CreateRelease(security: true, "10.0.403"));
            var evaluator = new SecurityReleaseEvaluator(new Mock<ILogger>().Object);

            var result = evaluator.IncludesNewerSecurityRelease(
                new SemanticVersion("10.0.403"),
                new SemanticVersion("10.0.404"),
                channelReleases);

            result.ShouldBeFalse();
        }

        [Fact]
        public void With_Security_Release_Above_Target_Returns_False()
        {
            var channelReleases = CreateChannelReleases(
                CreateRelease(security: true, "10.0.404"));
            var evaluator = new SecurityReleaseEvaluator(new Mock<ILogger>().Object);

            var result = evaluator.IncludesNewerSecurityRelease(
                new SemanticVersion("10.0.402"),
                new SemanticVersion("10.0.403"),
                channelReleases);

            result.ShouldBeFalse();
        }

        [Fact]
        public void With_No_Security_Releases_Returns_False()
        {
            var channelReleases = CreateChannelReleases(
                CreateRelease(security: false, "10.0.403", "10.0.114"),
                CreateRelease(security: false, "10.0.402", "10.0.113"));
            var evaluator = new SecurityReleaseEvaluator(new Mock<ILogger>().Object);

            var result = evaluator.IncludesNewerSecurityRelease(
                new SemanticVersion("10.0.402"),
                new SemanticVersion("10.0.404"),
                channelReleases);

            result.ShouldBeFalse();
        }

        [Fact]
        public void With_Security_Release_In_Only_A_Lower_Feature_Band_Returns_False()
        {
            var channelReleases = CreateChannelReleases(
                CreateRelease(security: true, "10.0.114"));
            var evaluator = new SecurityReleaseEvaluator(new Mock<ILogger>().Object);

            var result = evaluator.IncludesNewerSecurityRelease(
                new SemanticVersion("10.0.402"),
                new SemanticVersion("10.0.404"),
                channelReleases);

            result.ShouldBeFalse();
        }

        [Fact]
        public void With_Release_Without_Sdks_Is_Ignored()
        {
            var channelReleases = CreateChannelReleases(
                new ChannelRelease(Security: true, Sdks: null),
                CreateRelease(security: true, "10.0.403"));
            var evaluator = new SecurityReleaseEvaluator(new Mock<ILogger>().Object);

            var result = evaluator.IncludesNewerSecurityRelease(
                new SemanticVersion("10.0.402"),
                new SemanticVersion("10.0.404"),
                channelReleases);

            result.ShouldBeTrue();
        }

        [Fact]
        public void With_Empty_Releases_Returns_False()
        {
            var evaluator = new SecurityReleaseEvaluator(new Mock<ILogger>().Object);

            var result = evaluator.IncludesNewerSecurityRelease(
                new SemanticVersion("10.0.402"),
                new SemanticVersion("10.0.404"),
                CreateChannelReleases());

            result.ShouldBeFalse();
        }

        [Fact]
        public void With_Null_Channel_Releases_Throws_ArgumentNullException()
        {
            var evaluator = new SecurityReleaseEvaluator(new Mock<ILogger>().Object);

            Should.Throw<ArgumentNullException>(
                () => evaluator.IncludesNewerSecurityRelease(
                    new SemanticVersion("10.0.402"),
                    new SemanticVersion("10.0.404"),
                    null!));
        }
    }

    private static ChannelReleases CreateChannelReleases(params ChannelRelease[] releases)
    {
        return new ChannelReleases(new Collection<ChannelRelease>(releases));
    }

    private static ChannelRelease CreateRelease(bool security, params string[] sdks)
    {
        return new ChannelRelease(
            security,
            new Collection<ChannelSdk>(sdks.Select(sdk => new ChannelSdk(sdk)).ToList()));
    }
}
