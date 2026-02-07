using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;
using Synaxis.InferenceGateway.Infrastructure.External.DuckDuckGo;
using Synaxis.InferenceGateway.Infrastructure;
using Microsoft.Extensions.AI;
using Xunit;
using System.Collections.Generic;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.External.DuckDuckGo;

public class DuckDuckGoChatClientTests
{
    [Fact]
    public async Task EnsureTokenIsRetrievedAndUsed_OnPost()
    {
        var mock = new MockHttpMessageHandler();

        // First, status call returns token header
        mock.When(HttpMethod.Get, "https://duckduckgo.com/duckchat/v1/status")
            .Respond(req =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.OK);
                res.Headers.Add("x-vqd-4", "initial-token");
                res.Content = JsonContent.Create(new { status = "ok" });
                return res;
            });

        // Then, chat call will assert header present and return a reply
        mock.When(HttpMethod.Post, "https://duckduckgo.com/duckchat/v1/chat")
            .WithHeaders("x-vqd-4", "initial-token")
            .Respond(req =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { reply = "hello from ddg" }),
                };
                // Update token for next request
                res.Headers.Add("x-vqd-4", "next-token");
                return res;
            });

        var client = new HttpClient(mock);
        var ddg = new DuckDuckGoChatClient(client, "gpt-4o-mini");

        var response = await ddg.GetResponseAsync(new List<Microsoft.Extensions.AI.ChatMessage> { new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, "hi") }).ConfigureAwait(false);

        Assert.Single(response.Messages);
        Assert.Equal("hello from ddg", response.Messages[0].Text);
    }

    [Fact]
    public async Task ReturnsEmptyResponse_OnNonSuccess()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, "https://duckduckgo.com/duckchat/v1/status")
            .Respond(HttpStatusCode.OK);

        mock.When(HttpMethod.Post, "https://duckduckgo.com/duckchat/v1/chat")
            .Respond(HttpStatusCode.InternalServerError);

        var client = new HttpClient(mock);
        var ddg = new DuckDuckGoChatClient(client, "gpt-4o-mini");

        var response = await ddg.GetResponseAsync(new List<Microsoft.Extensions.AI.ChatMessage> { new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, "hi") }).ConfigureAwait(false);

        Assert.Empty(response.Messages);
    }
}
