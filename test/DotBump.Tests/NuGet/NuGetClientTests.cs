// Copyright © Roby Van Damme.

using System.Net;
using System.Text;
using DotBump.Common;
using DotBump.NuGet;
using DotBump.Tests.NuGet.Fakes;
using DotBump.Tests.TestHelpers;
using Serilog;
using Serilog.Events;
using Shouldly;

namespace DotBump.Tests.NuGet;

public class NuGetClientTests
{
    public class GetServiceIndexAsync
    {
        [Fact]
        public async Task With_Valid_Response_Returns_Service_Index()
        {
            var client = CreateClient(_ => JsonResponse(
                "{\"version\":\"3.0.0\",\"resources\":[{\"@id\":\"https://example.com/reg\",\"@type\":\"RegistrationsBaseUrl\"}]}"));

            var serviceIndex = await client.GetServiceIndexAsync("https://example.com/index.json");

            serviceIndex.ShouldSatisfyAllConditions(
                () => serviceIndex.Version.ShouldBe("3.0.0"),
                () => serviceIndex.Resources.ShouldContain(resource => resource.Type == "RegistrationsBaseUrl"));
        }

        [Fact]
        public async Task With_Unparseable_Response_Throws_DotBumpException()
        {
            var (client, sink) = CreateClientWithSink(_ => JsonResponse("null"));

            await Should.ThrowAsync<DotBumpException>(() =>
                client.GetServiceIndexAsync("https://example.com/index.json"));

            sink.Events.ShouldContain(logEvent =>
                logEvent.Level == LogEventLevel.Warning
                && logEvent.MessageTemplate.Text == "Unable to deserialize service index for {Source}");
        }

        [Fact]
        public async Task With_Http_Request_Exception_Logs_And_Rethrows()
        {
            var (client, sink) = CreateClientWithSink(_ => throw new HttpRequestException("connection refused"));

            await Should.ThrowAsync<HttpRequestException>(() =>
                client.GetServiceIndexAsync("https://example.com/index.json"));

            sink.Events.ShouldContain(logEvent =>
                logEvent.Level == LogEventLevel.Error
                && logEvent.MessageTemplate.Text == "An error occurred connecting to {Source}");
        }

        [Fact]
        public async Task With_Whitespace_Url_Throws_ArgumentException()
        {
            var client = CreateClient(_ => JsonResponse("{}"));

            await Should.ThrowAsync<ArgumentException>(() => client.GetServiceIndexAsync(" "));
        }
    }

    public class GetPackageInformationAsync
    {
        [Fact]
        public async Task With_Valid_Response_Returns_Registration_Index()
        {
            var client = CreateClient(_ => JsonResponse("{\"count\":0,\"items\":[]}"));

            var registrationIndex = await client.GetPackageInformationAsync("https://example.com/reg", "MyPackage");

            registrationIndex.ShouldNotBeNull();
            registrationIndex!.Count.ShouldBe(0);
        }

        [Fact]
        public async Task With_Not_Found_Response_Returns_Null_Without_Error()
        {
            var (client, sink) = CreateClientWithSink(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

            var registrationIndex = await client.GetPackageInformationAsync("https://example.com/reg", "MyPackage");

            registrationIndex.ShouldBeNull();
            sink.Events.ShouldNotContain(logEvent => logEvent.Level == LogEventLevel.Error);
        }

        [Fact]
        public async Task With_Non_Not_Found_Error_Logs_And_Rethrows()
        {
            var (client, sink) = CreateClientWithSink(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

            await Should.ThrowAsync<HttpRequestException>(() =>
                client.GetPackageInformationAsync("https://example.com/reg", "MyPackage"));

            sink.Events.ShouldContain(logEvent =>
                logEvent.Level == LogEventLevel.Error
                && logEvent.MessageTemplate.Text == "An HTTP Request exception occurred calling {PackageUrl}");
        }

        [Fact]
        public async Task With_Unparseable_Response_Returns_Null()
        {
            var client = CreateClient(_ => JsonResponse("null"));

            var registrationIndex = await client.GetPackageInformationAsync("https://example.com/reg", "MyPackage");

            registrationIndex.ShouldBeNull();
        }

        [Fact]
        public async Task With_Whitespace_Base_Url_Throws_ArgumentException()
        {
            var client = CreateClient(_ => JsonResponse("{}"));

            await Should.ThrowAsync<ArgumentException>(() =>
                client.GetPackageInformationAsync(" ", "MyPackage"));
        }

        [Fact]
        public async Task With_Whitespace_Package_Id_Throws_ArgumentException()
        {
            var client = CreateClient(_ => JsonResponse("{}"));

            await Should.ThrowAsync<ArgumentException>(() =>
                client.GetPackageInformationAsync("https://example.com/reg", " "));
        }
    }

    public class Dispose
    {
        [Fact]
        public void With_Client_Disposed_Twice_Does_Not_Throw()
        {
            var client = CreateClient(_ => JsonResponse("{}"));

            client.Dispose();

            Should.NotThrow(client.Dispose);
        }
    }

    private static NuGetClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        return new NuGetClient(
            new HttpClient(new FakeHttpMessageHandler(respond)),
            new LoggerConfiguration().CreateLogger());
    }

    private static (NuGetClient Client, TestLogSink Sink) CreateClientWithSink(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var sink = new TestLogSink();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();

        return (new NuGetClient(new HttpClient(new FakeHttpMessageHandler(respond)), logger), sink);
    }

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }
}
