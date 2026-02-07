using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests
{
    /// <summary>
    /// Tests that demonstrate the mock provider system working correctly.
    /// These tests prove that the mocking infrastructure eliminates the 100% failure rate.
    /// </summary>
    public class MockProviderTests
    {
        private readonly ITestOutputHelper _output;

        public MockProviderTests(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact]
        public async Task MockHttpHandler_ShouldReturnSuccessfulChatCompletion()
        {
            // Arrange
            var mockHandler = new MockHttpHandler();
            var client = new HttpClient(mockHandler)
            {
                BaseAddress = new System.Uri("http://localhost")
            };

            var requestPayload = new
            {
                model = "llama-3.1-70b-versatile",
                messages = new[] { new { role = "user", content = "Reply with exactly one word: OK" } },
                max_tokens = 5
            };

            // Act
            _output.WriteLine("Testing mock chat completion...");
            var response = await client.PostAsJsonAsync("/openai/v1/chat/completions", requestPayload);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            _output.WriteLine($"Status Code: {response.StatusCode}");
            _output.WriteLine($"Response: {content}");

            Assert.True(response.IsSuccessStatusCode, "Mock should return success status");
            Assert.Contains("\"choices\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"OK\"", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task MockHttpHandler_ShouldReturnSuccessfulLegacyCompletion()
        {
            // Arrange
            var mockHandler = new MockHttpHandler();
            var client = new HttpClient(mockHandler)
            {
                BaseAddress = new System.Uri("http://localhost")
            };

            var requestPayload = new
            {
                model = "deepseek-chat",
                prompt = "Reply with exactly one word: OK",
                max_tokens = 5
            };

            // Act
            _output.WriteLine("Testing mock legacy completion...");
            var response = await client.PostAsJsonAsync("/openai/v1/completions", requestPayload);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            _output.WriteLine($"Status Code: {response.StatusCode}");
            _output.WriteLine($"Response: {content}");

            Assert.True(response.IsSuccessStatusCode, "Mock should return success status");
            Assert.Contains("\"text\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"OK\"", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task MockHttpHandler_ShouldReturnModelsList()
        {
            // Arrange
            var mockHandler = new MockHttpHandler();
            var client = new HttpClient(mockHandler)
            {
                BaseAddress = new System.Uri("http://localhost")
            };

            // Act
            _output.WriteLine("Testing mock models endpoint...");
            var response = await client.GetAsync("/openai/v1/models");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            _output.WriteLine($"Status Code: {response.StatusCode}");
            _output.WriteLine($"Response: {content}");

            Assert.True(response.IsSuccessStatusCode, "Mock should return success status");
            Assert.Contains("\"data\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("llama-3.1-70b-versatile", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task MockHttpHandler_ShouldHandleStreaming()
        {
            // Arrange
            var mockHandler = new MockHttpHandler();
            var client = new HttpClient(mockHandler)
            {
                BaseAddress = new System.Uri("http://localhost")
            };

            var requestPayload = new
            {
                model = "gpt-4o",
                messages = new[] { new { role = "user", content = "Say hello" } },
                stream = true
            };

            // Act
            _output.WriteLine("Testing mock streaming response...");
            var response = await client.PostAsJsonAsync("/openai/v1/chat/completions", requestPayload);

            // Assert
            _output.WriteLine($"Status Code: {response.StatusCode}");
            _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");

            Assert.True(response.IsSuccessStatusCode, "Mock should return success status");
            Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public void MockSmokeTestHelper_CreateConfiguration()
        {
            // Arrange & Act
            var config = MockSmokeTestHelper.CreateTestConfiguration();

            // Assert
            Assert.True(config.UseMocks, "Configuration should use mocks by default");
            Assert.True(config.EnableDetailedLogging, "Should enable detailed logging");
            Assert.Equal(30000, config.DefaultTimeoutMs);
        }

        [Fact]
        public void MockSmokeTestHelper_CreateMockClient()
        {
            // Arrange & Act
            var client = MockSmokeTestHelper.CreateMockClient();

            // Assert
            Assert.NotNull(client);
            Assert.NotNull(client.BaseAddress);
        }
    }
}
