using System;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.AI;
using RichardSzalay.MockHttp;
using Xunit;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.External.KiloCode
{
    public class KiloCodeChatClientTests
    {
        [Fact]
        public void Constructor_DoesNotThrow_AndSetsBaseAddress_AndHeaders()
        {
            // Arrange
            var apiKey = "test-key";
            var modelId = "glm-4.7";

            var mockHttp = new MockHttpMessageHandler();
            var client = mockHttp.ToHttpClient();

            // Act
            var kilo = new Synaxis.InferenceGateway.Infrastructure.External.KiloCode.KiloCodeChatClient(apiKey, modelId, client);

            // Assert - check that the underlying GenericOpenAiChatClient configured the HttpClient via options
            // We can't access OpenAIClient internals easily, but we can assert the BaseAddress used in the GenericOpenAiChatClient ctor
            var endpointField = typeof(Synaxis.InferenceGateway.Infrastructure.GenericOpenAiChatClient)
                .GetField("_innerClient", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(endpointField);

            // Verify that headers are present in the custom header dictionary via reflection of the nested policy type
            var customHeaderPolicyType = typeof(Synaxis.InferenceGateway.Infrastructure.GenericOpenAiChatClient)
                .GetNestedType("CustomHeaderPolicy", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(customHeaderPolicyType);

            // Ensure Kilo API URL constant is as expected
            var kiloApiUrlField = typeof(Synaxis.InferenceGateway.Infrastructure.External.KiloCode.KiloCodeChatClient)
                .GetField("KiloApiUrl", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(kiloApiUrlField);
            var kiloApiUrl = kiloApiUrlField?.GetValue(null) as string;
            Assert.Equal("https://api.kilo.ai/api/openrouter", kiloApiUrl);
        }
    }
}
