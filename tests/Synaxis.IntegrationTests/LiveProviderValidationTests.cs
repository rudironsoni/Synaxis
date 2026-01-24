using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Synaxis.IntegrationTests;

public class LiveProviderValidationTests : IClassFixture<SynaxisWebApplicationFactory>
{
    private readonly SynaxisWebApplicationFactory _factory;

    public LiveProviderValidationTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _factory.OutputHelper = output;
    }

    [Fact]
    public async Task Validate_Groq_Connectivity()
    {
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var apiKey = config["Synaxis:Providers:Groq:Key"];
        
        Assert.False(string.IsNullOrEmpty(apiKey), "API Key for Groq is missing in Configuration (appsettings.json or Env Var)");

        await ExecuteTest("llama-3.1-8b-instant");
    }

    [Fact]
    public async Task Validate_Cohere_Connectivity()
    {
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var apiKey = config["Synaxis:Providers:Cohere:Key"];
        
        Assert.False(string.IsNullOrEmpty(apiKey), "API Key for Cohere is missing in Configuration (appsettings.json or Env Var)");

        await ExecuteTest("command-r-08-2024");
    }

    [Fact]
    public async Task Validate_Pollinations_Connectivity()
    {
        // Pollinations typically doesn't require a key, but we check if configured/required by environment if needed
        await ExecuteTest("gpt-4o-mini");
    }

    [Fact]
    public async Task Validate_Cloudflare_Connectivity()
    {
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var apiKey = config["Synaxis:Providers:Cloudflare:Key"];
        var accountId = config["Synaxis:Providers:Cloudflare:AccountId"];

        Assert.False(string.IsNullOrEmpty(apiKey), "API Key for Cloudflare is missing in Configuration (appsettings.json or Env Var)");
        Assert.False(string.IsNullOrEmpty(accountId), "Account ID for Cloudflare is missing in Configuration (appsettings.json or Env Var)");

        await ExecuteTest("@cf/meta/llama-3-8b-instruct");
    }

    private async Task ExecuteTest(string modelId)
    {
        var client = _factory.CreateClient();
        
        var request = new
        {
            model = modelId,
            messages = new[]
            {
                new { role = "user", content = "Hello, this is an integration test. Reply with 'OK'." }
            },
            stream = false
        };

        var response = await client.PostAsJsonAsync("/v1/chat/completions", request);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with {response.StatusCode}: {error}");
        }

        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Choices);
        Assert.NotNull(result.Choices[0].Message.Content);
    }

    // Helper records for deserialization matching the Gateway's output
    public record ChatCompletionResponse(string Id, string Object, long Created, string Model, List<Choice> Choices, Usage Usage);
    public record Choice(int Index, Message Message, string Finish_Reason);
    public record Message(string Role, string Content);
    public record Usage(int Prompt_Tokens, int Completion_Tokens, int Total_Tokens);
}
