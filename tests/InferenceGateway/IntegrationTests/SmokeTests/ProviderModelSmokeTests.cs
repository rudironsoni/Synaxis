using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Models;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests
{
    [Trait("Category", "Smoke")]
    [Trait("Type", "Integration")]
    public class ProviderModelSmokeTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;

        public ProviderModelSmokeTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _factory.OutputHelper = output;
        }

        [Theory]
        [MemberData(nameof(SmokeTestDataGenerator.GenerateChatCompletionCases), MemberType = typeof(SmokeTestDataGenerator))]
        [Trait("Endpoint", "ChatCompletions")]
        public async Task ChatCompletions_SmokeTest(SmokeTestCase testCase)
        {
            ValidateProviderConfigured(testCase.Provider);

            var client = _factory.CreateClient();
            var executor = new SmokeTestExecutor(client, new SmokeTestOptions(), _output);

            var result = await executor.ExecuteAsync(testCase).ConfigureAwait(false);

            _output.WriteLine($"Provider={testCase.Provider} Model={testCase.Model} Success={result.Success} TimeMs={result.ResponseTime.TotalMilliseconds} Attempts={result.AttemptCount}");
            if (!string.IsNullOrEmpty(result.Error)) _output.WriteLine($"Error: {result.Error}");
            if (!string.IsNullOrEmpty(result.ResponseSnippet)) _output.WriteLine($"Snippet: {result.ResponseSnippet}");

            Assert.True(result.Success, $"Smoke test failed for {testCase.Provider}/{testCase.Model}: {result.Error}");
        }

        [Theory]
        [MemberData(nameof(SmokeTestDataGenerator.GenerateLegacyCompletionCases), MemberType = typeof(SmokeTestDataGenerator))]
        [Trait("Endpoint", "LegacyCompletions")]
        public async Task LegacyCompletions_SmokeTest(SmokeTestCase testCase)
        {
            ValidateProviderConfigured(testCase.Provider);

            var client = _factory.CreateClient();
            var executor = new SmokeTestExecutor(client, new SmokeTestOptions(), _output);

            var result = await executor.ExecuteAsync(testCase).ConfigureAwait(false);

            _output.WriteLine($"Provider={testCase.Provider} Model={testCase.Model} Success={result.Success} TimeMs={result.ResponseTime.TotalMilliseconds} Attempts={result.AttemptCount}");
            if (!string.IsNullOrEmpty(result.Error)) _output.WriteLine($"Error: {result.Error}");
            if (!string.IsNullOrEmpty(result.ResponseSnippet)) _output.WriteLine($"Snippet: {result.ResponseSnippet}");

            Assert.True(result.Success, $"Smoke test failed for {testCase.Provider}/{testCase.Model}: {result.Error}");
        }

        private void ValidateProviderConfigured(string provider)
        {
            // Pollinations does not require an API key in our configuration - skip validation
            if (string.Equals(provider, "Pollinations", StringComparison.OrdinalIgnoreCase)) return;

            var config = _factory.Services.GetRequiredService<IConfiguration>();
            var key = config[$"Synaxis:InferenceGateway:Providers:{provider}:Key"];

            if (string.IsNullOrEmpty(key) || key.Contains("REPLACE", StringComparison.OrdinalIgnoreCase) || key.Contains("INSERT", StringComparison.OrdinalIgnoreCase) || key.Contains("CHANGE", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Fail($"Provider '{provider}' is not configured with a valid API key. Please set Synaxis:InferenceGateway:Providers:{provider}:Key or the SYNAPLEXER_{provider.ToUpperInvariant()}_KEY environment variable.");
            }
        }
    }
}
