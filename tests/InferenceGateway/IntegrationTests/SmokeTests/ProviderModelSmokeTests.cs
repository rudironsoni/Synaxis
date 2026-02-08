// <copyright file="ProviderModelSmokeTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Models;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests
{
    [Trait("Category", "Mocked")]
    [Trait("Category", "Smoke")]
    [Trait("Type", "Integration")]
    public class ProviderModelSmokeTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;

        public ProviderModelSmokeTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
        {
            this._factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this._output = output ?? throw new ArgumentNullException(nameof(output));
            this._factory.OutputHelper = output;
        }

        [Theory]
        [MemberData(nameof(SmokeTestDataGenerator.GenerateChatCompletionCases), MemberType = typeof(SmokeTestDataGenerator))]
        [Trait("Endpoint", "ChatCompletions")]
        public async Task ChatCompletions_SmokeTest(string provider, string model, string canonicalId, EndpointType endpoint)
        {
            var testCase = new SmokeTestCase(provider, model, canonicalId, endpoint);

            // Use mock client for deterministic testing instead of real provider calls
            var client = MockSmokeTestHelper.CreateMockClient();
            var executor = new SmokeTestExecutor(client, new SmokeTestOptions(), this._output);

            var result = await executor.ExecuteAsync(testCase);

            this._output.WriteLine($"Provider={testCase.provider} Model={testCase.model} Success={result.success} TimeMs={result.responseTime.TotalMilliseconds} Attempts={result.attemptCount}");
            if (!string.IsNullOrEmpty(result.error))
            {
                this._output.WriteLine($"Error: {result.error}");
            }

            if (!string.IsNullOrEmpty(result.responseSnippet))
            {
                this._output.WriteLine($"Snippet: {result.responseSnippet}");
            }

            Assert.True(result.success, $"Smoke test failed for {testCase.provider}/{testCase.model}: {result.error}");
        }

        [Theory]
        [MemberData(nameof(SmokeTestDataGenerator.GenerateLegacyCompletionCases), MemberType = typeof(SmokeTestDataGenerator))]
        [Trait("Endpoint", "LegacyCompletions")]
        public async Task LegacyCompletions_SmokeTest(string provider, string model, string canonicalId, EndpointType endpoint)
        {
            var testCase = new SmokeTestCase(provider, model, canonicalId, endpoint);

            // Use mock client for deterministic testing instead of real provider calls
            var client = MockSmokeTestHelper.CreateMockClient();
            var executor = new SmokeTestExecutor(client, new SmokeTestOptions(), this._output);

            var result = await executor.ExecuteAsync(testCase);

            this._output.WriteLine($"Provider={testCase.provider} Model={testCase.model} Success={result.success} TimeMs={result.responseTime.TotalMilliseconds} Attempts={result.attemptCount}");
            if (!string.IsNullOrEmpty(result.error))
            {
                this._output.WriteLine($"Error: {result.error}");
            }

            if (!string.IsNullOrEmpty(result.responseSnippet))
            {
                this._output.WriteLine($"Snippet: {result.responseSnippet}");
            }

            Assert.True(result.success, $"Smoke test failed for {testCase.provider}/{testCase.model}: {result.error}");
        }
    }
}
