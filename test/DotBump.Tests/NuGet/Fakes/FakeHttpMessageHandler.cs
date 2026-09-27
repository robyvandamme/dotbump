// Copyright © Roby Van Damme.

namespace DotBump.Tests.NuGet.Fakes;

internal sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(respond(request));
    }
}
