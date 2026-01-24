using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synaplexer.Application.Dtos;
using Synaplexer.Domain.Interfaces;
using Synaplexer.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synaplexer.API.UnitTests.Integration
{
    public class ProviderMatrixTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly ITestOutputHelper _output;

        public ProviderMatrixTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
        }

        public static IEnumerable<object[]> GetProviderModelMatrix()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            services.AddLogging();
            services.AddSynaplexerInfrastructure(configuration);

            using var serviceProvider = services.BuildServiceProvider();
            var providers = serviceProvider.GetServices<ILlmProvider>();

            foreach (var provider in providers)
            {
                var type = provider.GetType();
                var providerName = provider.Name;

                // Look for AvailableModels (private static) or SupportedModels (public/private instance/static)
                var modelsField = type.GetField("AvailableModels", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                var modelsProperty = type.GetProperty("SupportedModels", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

                IEnumerable<string>? models = null;

                if (modelsField != null)
                {
                    models = modelsField.GetValue(provider) as IEnumerable<string>;
                }

                if (models == null && modelsProperty != null)
                {
                    models = modelsProperty.GetValue(provider) as IEnumerable<string>;
                }

                if (models != null)
                {
                    foreach (var model in models)
                    {
                        yield return new object[] { providerName, model };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetProviderModelMatrix))]
        public async Task Verify_Provider_Model_Health(string providerName, string modelId)
        {
            // Arrange
            _ = providerName; // Suppress xUnit1026
            var client = _factory.CreateClient();
            var request = new
            {
                Model = modelId,
                Messages = new[]
                {
                    new { Role = "user", Content = "Hello" }
                },
                Temperature = 0.7,
                MaxTokens = 10
            };

            // Act
            var response = await client.PostAsJsonAsync("/v1/chat/completions", request);

            // Assert
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"[FAILURE BODY] {providerName}/{modelId}: {response.StatusCode} - {errorBody}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(result.TryGetProperty("choices", out var choices));
            Assert.True(choices.GetArrayLength() > 0);
        }
    }
}
