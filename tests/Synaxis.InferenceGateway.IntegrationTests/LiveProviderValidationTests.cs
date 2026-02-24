// <copyright file="LiveProviderValidationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests;

[Collection("Integration")]
public class LiveProviderValidationTests
{
    private readonly SynaxisWebApplicationFactory _factory;

    public LiveProviderValidationTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
    {
        this._factory = factory;
        this._factory.OutputHelper = output;
    }

    [Fact]
    public async Task Validate_Groq_Connectivity()
    {
        var config = this._factory.Services.GetRequiredService<IConfiguration>();
        if (!ShouldRunLiveTests(config))
        {
            return;
        }

        var apiKey = config["Synaxis:InferenceGateway:Providers:Groq:Key"];

        if (string.IsNullOrEmpty(apiKey))
        {
            return;
        }

        await this.ExecuteTest("llama-3.1-8b-instant");
    }

    [Fact]
    public async Task Validate_Cohere_Connectivity()
    {
        var config = this._factory.Services.GetRequiredService<IConfiguration>();
        if (!ShouldRunLiveTests(config))
        {
            return;
        }

        var apiKey = config["Synaxis:InferenceGateway:Providers:Cohere:Key"];

        if (string.IsNullOrEmpty(apiKey))
        {
            return;
        }

        await this.ExecuteTest("command-r-08-2024");
    }

    [Fact]
    public async Task Validate_Pollinations_Connectivity()
    {
        var config = this._factory.Services.GetRequiredService<IConfiguration>();
        if (!ShouldRunLiveTests(config))
        {
            return;
        }

        await this.ExecuteTest("gpt-4o-mini");
    }

    [Fact]
    public async Task Validate_Cloudflare_Connectivity()
    {
        var config = this._factory.Services.GetRequiredService<IConfiguration>();
        if (!ShouldRunLiveTests(config))
        {
            return;
        }

        var apiKey = config["Synaxis:InferenceGateway:Providers:Cloudflare:Key"];
        var accountId = config["Synaxis:InferenceGateway:Providers:Cloudflare:AccountId"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(accountId))
        {
            return;
        }

        await this.ExecuteTest("@cf/meta/llama-3-8b-instruct");
    }

    private static bool ShouldRunLiveTests(IConfiguration config)
    {
        var flag = config["Synaxis:Integration:RunLiveProviderTests"];
        return string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ExecuteTest(string modelId)
    {
        var client = this._factory.CreateClient();

        var request = new
        {
            model = modelId,
            messages = new[]
            {
            new { role = "user", content = "Hello, this is an integration test. Reply with 'OK'." },
            },
            stream = false,
        };

        var response = await client.PostAsJsonAsync("/openai/v1/chat/completions", request).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new Exception($"Request failed with {response.StatusCode}: {error}");
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>().ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Choices);
        Assert.NotNull(result.Choices[0].Message.Content);
    }

    // Helper records for deserialization matching the Gateway's output
    public record ChatCompletionResponse(string Id, string Object, long Created, string Model, List<Choice> Choices, Usage Usage);

    public record Choice(int Index, Message Message, string FinishReason);

    public record Message(string Role, string Content);

    public record Usage(int PromptTokens, int CompletionTokens, int TotalTokens);
}
