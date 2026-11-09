using System.Net;
using System.Text;
using RblxExecutorUI;

static class ProfileChecks
{
    private sealed class Stub(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { token.ThrowIfCancellationRequested(); Requests++; return Task.FromResult(responder(request)); }
    }
    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    public static async Task Run(Action<bool, string> check)
    {
        check(ClientIdentity.Parse("Unknown|1|Active|0") == null, "Reject unknown placeholder identity");
        check(ClientIdentity.Parse("User|0|job|0") == null, "Reject missing UserId");
        check(ClientIdentity.Parse("User|123|job|-1") == null, "Reject invalid PlaceId");
        check(ClientIdentity.Parse("bad") == null, "Reject malformed identity");
        check(ClientIdentity.Parse("Capivara|123|job|456") is { UserId: 123, Name: "Capivara", PlaceId: 456 }, "Parse native identity");
        int metadataRequests = 0;
        using var handler = new Stub(request =>
        {
            if (request.RequestUri!.Host == "economy.roblox.com") return new(HttpStatusCode.ServiceUnavailable);
            if (request.RequestUri.Host == "thumbnails.roblox.com")
            {
                metadataRequests++;
                return metadataRequests == 1 ? Json("{\"data\":[{\"targetId\":123,\"state\":\"Pending\",\"imageUrl\":null}]}")
                    : Json("{\"data\":[{\"targetId\":123,\"state\":\"Completed\",\"imageUrl\":\"https://tr.rbxcdn.com/avatar.png\"}]}");
            }
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[] { 1, 2, 3 }) };
        });
        using var client = new HttpClient(handler);
        var service = new ClientProfileService(client);
        check(await service.GetPlaceNameAsync(456, default) == null, "Place failure is handled independently");
        check(await service.GetAvatarAsync(123, default) == null, "Pending avatar remains retryable");
        check((await service.GetAvatarAsync(123, default))?.SequenceEqual(new byte[] { 1, 2, 3 }) == true, "Avatar succeeds after pending and place failure");
        using var wrongHandler = new Stub(_ => Json("{\"data\":[{\"targetId\":999,\"state\":\"Completed\",\"imageUrl\":\"https://tr.rbxcdn.com/avatar.png\"}]}"));
        using var wrongClient = new HttpClient(wrongHandler);
        check(await new ClientProfileService(wrongClient).GetAvatarAsync(123, default) == null && wrongHandler.Requests == 1, "Reject other account thumbnail");
        using var malformedHandler = new Stub(_ => Json("{\"data\":[{\"targetId\":\"invalid\",\"state\":true,\"imageUrl\":42}]}"));
        using var malformedClient = new HttpClient(malformedHandler);
        check(await new ClientProfileService(malformedClient).GetAvatarAsync(123, default) == null, "Malformed thumbnail fields are ignored");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        try { await service.GetAvatarAsync(123, cancellation.Token); check(false, "Canceled session"); }
        catch (OperationCanceledException) { check(true, "Canceled session does not continue download"); }
    }
}
