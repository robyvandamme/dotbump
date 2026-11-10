// Copyright © Roby Van Damme.

using System.Net;
using DotBump.Commands.BumpSdk;
using DotBump.Common;
using DotBump.Tests.NuGet.Fakes;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpSdk;

public class ReleaseWebServiceTests
{
    private const string ChannelReleasesUrl =
        "https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json";

    public class GetReleasesAsync
    {
        [Fact]
        public async Task Returns_Releases()
        {
            var loggerMock = new Mock<ILogger>();
            var service = new ReleaseWebService(loggerMock.Object, new HttpClient());
            var result = (await service.GetReleasesAsync()).ToList();
            result.ShouldNotBeNull();
            result.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task With_Releases_Index_Json_Populates_Channel_Releases_Url()
        {
            var json = await File.ReadAllTextAsync(Path.Combine(
                Directory.GetCurrentDirectory(),
                "Data",
                "releases-index.json"));
            var service = CreateService(json);

            var result = (await service.GetReleasesAsync()).ToList();

            result.ShouldContain(release =>
                release.ChannelVersion == "10.0" && !string.IsNullOrWhiteSpace(release.ReleasesJson));
        }
    }

    public class GetChannelReleasesAsync
    {
        [Fact]
        public async Task With_Channel_Releases_Json_Returns_Release_History()
        {
            var json = await File.ReadAllTextAsync(Path.Combine(
                Directory.GetCurrentDirectory(),
                "Data",
                "releases-10.0.json"));
            var service = CreateService(json);

            var result = await service.GetChannelReleasesAsync(new Uri(ChannelReleasesUrl));

            result.ShouldSatisfyAllConditions(
                () => result.Releases.Count.ShouldBe(3),
                () => result.Releases[0].Security.ShouldBeFalse(),
                () => result.Releases[0].Sdks.ShouldNotBeNull().Count.ShouldBe(2),
                () => result.Releases[0].Sdk.ShouldNotBeNull().Version.ShouldBe("10.0.404"),
                () => result.Releases[0].Runtime.ShouldNotBeNull().Version.ShouldBe("10.0.15"),
                () => result.Releases[0].AspNetCoreRuntime.ShouldNotBeNull().Version.ShouldBe("10.0.15"),
                () => result.Releases[0].ReleaseVersion.ShouldBe("10.0.15"),
                () => result.Releases[1].Security.ShouldBeTrue(),
                () => result.Releases[1].Sdks.ShouldNotBeNull()
                    .ShouldContain(sdk => sdk.Version == "10.0.403"),
                () => result.Releases[2].Sdks.ShouldNotBeNull()
                    .ShouldContain(sdk => sdk.Version == "10.0.113"));
        }

        [Fact]
        public async Task With_Empty_Response_Throws_DotBumpException_With_Source_Uri()
        {
            var service = CreateService(string.Empty);

            var exception = await Should.ThrowAsync<DotBumpException>(
                () => service.GetChannelReleasesAsync(new Uri(ChannelReleasesUrl)));

            exception.Message.ShouldContain(ChannelReleasesUrl);
        }

        [Fact]
        public async Task With_Response_Without_Releases_Throws_DotBumpException()
        {
            var service = CreateService("{ \"channel-version\": \"10.0\" }");

            await Should.ThrowAsync<DotBumpException>(
                () => service.GetChannelReleasesAsync(new Uri(ChannelReleasesUrl)));
        }

        [Fact]
        public async Task With_Malformed_Json_Throws_DotBumpException_With_Source_Uri()
        {
            var service = CreateService("{ \"releases\": [ ");

            var exception = await Should.ThrowAsync<DotBumpException>(
                () => service.GetChannelReleasesAsync(new Uri(ChannelReleasesUrl)));

            exception.Message.ShouldContain(ChannelReleasesUrl);
        }

        [Fact]
        public async Task With_Sdk_Without_Version_Throws_DotBumpException_With_Source_Uri()
        {
            var service = CreateService(
                "{ \"releases\": [ { \"security\": true, \"sdks\": [ {} ] } ] }");

            var exception = await Should.ThrowAsync<DotBumpException>(
                () => service.GetChannelReleasesAsync(new Uri(ChannelReleasesUrl)));

            exception.Message.ShouldContain(ChannelReleasesUrl);
        }

        [Fact]
        public async Task With_Null_Uri_Throws_ArgumentNullException()
        {
            var service = CreateService("{}");

            await Should.ThrowAsync<ArgumentNullException>(
                () => service.GetChannelReleasesAsync(null!));
        }

        [Theory]
        [InlineData("http://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json")]
        [InlineData("https://example.com/dotnet/release-metadata/10.0/releases.json")]
        [InlineData("https://user:secret@builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json")]
        public async Task With_Untrusted_Url_Throws_DotBumpException(string url)
        {
            var service = CreateService("{}");

            await Should.ThrowAsync<DotBumpException>(
                () => service.GetChannelReleasesAsync(new Uri(url)));
        }
    }

    private static ReleaseWebService CreateService(string responseJson)
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson),
        });

        return new ReleaseWebService(
            new Mock<ILogger>().Object,
            new HttpClient(handler));
    }
}
