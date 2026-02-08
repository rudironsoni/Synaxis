// <copyright file="OpenAIRequestParserTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.Translation;
using Synaxis.InferenceGateway.WebApi.Helpers;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests.Helpers
{
    public class OpenAIRequestParserTests
    {
        [Fact]
        public async Task ParseAsync_WithNullContext_ReturnsNull()
        {
            // Act
            var result = await OpenAIRequestParser.ParseAsync(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ParseAsync_WithEmptyBody_ReturnsNull()
        {
            // Arrange
            var context = CreateHttpContextWithBody(string.Empty);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ParseAsync_WithWhitespaceBody_ReturnsNull()
        {
            // Arrange
            var context = CreateHttpContextWithBody("   \n\t  ");

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ParseAsync_WithValidJson_ReturnsRequest()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[]
                {
                new { role = "user", content = "Hello" },
                },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("gpt-4", result.Model);
            Assert.Single(result.Messages);
            Assert.Equal("user", result.Messages[0].Role);
            Assert.Equal("Hello", result.Messages[0].Content?.ToString());
        }

        [Fact]
        public async Task ParseAsync_WithValidJsonAndContentLength_ReturnsRequest()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[]
                {
                new { role = "user", content = "Hello" },
                },
            });
            var context = CreateHttpContextWithBody(json, contentLength: json.Length);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("gpt-4", result.Model);
        }

        [Fact]
        public async Task ParseAsync_WithCaseInsensitiveProperties_ThrowsBadHttpRequestException()
        {
            // Arrange
            var json = @"{ ""MODEL"": ""gpt-4"", ""MESSAGES"": [{""ROLE"": ""USER"", ""CONTENT"": ""Hello""}] }";
            var context = CreateHttpContextWithBody(json);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
                () => OpenAIRequestParser.ParseAsync(context));

            Assert.Contains("Role must be one of: system, user, assistant, tool", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ParseAsync_WithStreamingEnabled_ReturnsRequestWithStreamTrue()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                stream = true,
                messages = new[]
                {
                new { role = "user", content = "Hello" },
                },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Stream);
        }

        [Fact]
        public async Task ParseAsync_WithStreamingDisabled_ReturnsRequestWithStreamFalse()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                stream = false,
                messages = new[]
                {
                new { role = "user", content = "Hello" },
                },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Stream);
        }

        [Fact]
        public async Task ParseAsync_WithAllOptions_ReturnsCompleteRequest()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[]
                {
                new { role = "system", content = "You are helpful" },
                new { role = "user", content = "Hello" },
                },
                temperature = 0.7,
                top_p = 0.9,
                max_tokens = 100,
                stream = true,
                stop = "STOP",
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("gpt-4", result.Model);
            Assert.Equal(2, result.Messages.Count);
            Assert.Equal(0.7, result.Temperature);
            Assert.Equal(0.9, result.TopP);
            Assert.Equal(100, result.MaxTokens);
            Assert.True(result.Stream);
        }

        [Fact]
        public async Task ParseAsync_WithTools_ReturnsRequestWithTools()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[]
                {
                new { role = "user", content = "What's the weather?" },
                },
                tools = new[]
                {
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "get_weather",
                        description = "Get weather information"
                    }
                },
                },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Tools);
            Assert.Single(result.Tools);
            Assert.Equal("function", result.Tools[0].Type);
            Assert.Equal("get_weather", result.Tools[0].Function?.Name);
        }

        [Fact]
        public async Task ParseAsync_WithResponseFormat_ReturnsRequestWithResponseFormat()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[]
                {
                new { role = "user", content = "Generate JSON" },
                },
                response_format = new { type = "json_object" },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ResponseFormat);
        }

        [Fact]
        public async Task ParseAsync_WithInvalidJson_ThrowsBadHttpRequestException()
        {
            // Arrange
            var context = CreateHttpContextWithBody("{ invalid json }");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
                () => OpenAIRequestParser.ParseAsync(context));
            Assert.Contains("Invalid JSON", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ParseAsync_WithMalformedJson_ThrowsBadHttpRequestException()
        {
            // Arrange
            var context = CreateHttpContextWithBody("{\"model\": \"gpt-4\", \"messages\": }");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
                () => OpenAIRequestParser.ParseAsync(context));
            Assert.Contains("Invalid JSON", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ParseAsync_WithIncompleteJson_ThrowsBadHttpRequestException()
        {
            // Arrange
            var context = CreateHttpContextWithBody("{\"model\": \"gpt-4\"");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
                () => OpenAIRequestParser.ParseAsync(context));
            Assert.Contains("Invalid JSON", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ParseAsync_WithEmptyJsonObject_ReturnsEmptyRequest()
        {
            // Arrange
            var context = CreateHttpContextWithBody("{}");

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Model);
            Assert.Empty(result.Messages);
        }

        [Fact]
        public async Task ParseAsync_WithSystemMessage_ReturnsRequest()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[]
                {
                new { role = "system", content = "You are a helpful assistant" },
                },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Messages);
            Assert.Equal("system", result.Messages[0].Role);
            Assert.Equal("You are a helpful assistant", result.Messages[0].Content?.ToString());
        }

        [Fact]
        public async Task ParseAsync_WithAssistantMessage_ReturnsRequest()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[]
                {
                new { role = "assistant", content = "Hello! How can I help you?" },
                },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Messages);
            Assert.Equal("assistant", result.Messages[0].Role);
        }

        [Fact]
        public async Task ParseAsync_WithToolMessage_ReturnsRequest()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[]
                {
                new { role = "tool", content = "Tool result", tool_call_id = "call_123" },
                },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Messages);
            Assert.Equal("tool", result.Messages[0].Role);
            Assert.Equal("call_123", result.Messages[0].ToolCallId);
        }

        [Fact]
        public async Task ParseAsync_WithMultipleMessages_ReturnsAllMessages()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[]
                {
                new { role = "system", content = "You are helpful" },
                new { role = "user", content = "Hello" },
                new { role = "assistant", content = "Hi there" },
                new { role = "user", content = "How are you?" },
                },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Messages.Count);
            Assert.Equal("system", result.Messages[0].Role);
            Assert.Equal("user", result.Messages[1].Role);
            Assert.Equal("assistant", result.Messages[2].Role);
            Assert.Equal("user", result.Messages[3].Role);
        }

        [Fact]
        public async Task ParseAsync_WithMessageName_ReturnsRequestWithName()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[]
                {
                new { role = "user", content = "Hello", name = "John" },
                },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Messages);
            Assert.Equal("John", result.Messages[0].Name);
        }

        [Fact]
        public async Task ParseAsync_WithEmptyMessagesArray_ThrowsBadHttpRequestException()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = Array.Empty<object>(),
            });
            var context = CreateHttpContextWithBody(json);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
                () => OpenAIRequestParser.ParseAsync(context));

            Assert.Contains("At least one message is required", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ParseAsync_WithMessagesAsNull_ThrowsBadHttpRequestException()
        {
            // Arrange
            var json = @"{ ""model"": ""gpt-4"", ""messages"": null }";
            var context = CreateHttpContextWithBody(json);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
                () => OpenAIRequestParser.ParseAsync(context));

            Assert.Contains("Messages are required", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ParseAsync_WithContentLengthExceedingDefaultLimit_ThrowsBadHttpRequestException()
        {
            // Arrange
            var largeBody = new string('x', 11 * 1024 * 1024); // 11 MB
            var context = CreateHttpContextWithBody(largeBody, contentLength: largeBody.Length);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
                () => OpenAIRequestParser.ParseAsync(context));
            Assert.Contains("Request body too large", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ParseAsync_WithContentLengthWithinDefaultLimit_Succeeds()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = "Hello" } },
            });
            var context = CreateHttpContextWithBody(json, contentLength: json.Length);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ParseAsync_WithChunkedBodyExceedingDefaultLimit_ThrowsBadHttpRequestException()
        {
            // Arrange - create a body that will exceed limit when read in chunks
            var largeBody = new string('x', 11 * 1024 * 1024); // 11 MB
            var context = CreateHttpContextWithBody(largeBody, contentLength: null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
                () => OpenAIRequestParser.ParseAsync(context));
            Assert.Contains("Request body too large", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ParseAsync_WithCustomMaxBodySizeFromConfiguration_UsesConfiguredLimit()
        {
            // Arrange
            var customMaxSize = 1024L; // 1 KB
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = "Hello" } },
            });
            var context = CreateHttpContextWithBody(json, contentLength: json.Length, maxBodySize: customMaxSize);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ParseAsync_WithBodyExceedingCustomLimit_ThrowsBadHttpRequestException()
        {
            // Arrange
            var customMaxSize = 50L; // 50 bytes - small limit
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = "Hello world this is a long message that exceeds the limit" } },
            });

            // Don't pass Content-Length so it goes through chunked reading path
            var context = CreateHttpContextWithBody(json, contentLength: null, maxBodySize: customMaxSize);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
                () => OpenAIRequestParser.ParseAsync(context));
            Assert.Contains("Request body too large", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ParseAsync_WhenConfigurationServiceThrowsException_UsesDefaultLimit()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = "Hello" } },
            });
            var context = CreateHttpContextWithBody(json, contentLength: json.Length, throwOnConfig: true);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ParseAsync_WhenConfigurationOptionsIsNull_UsesDefaultLimit()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = "Hello" } },
            });
            var context = CreateHttpContextWithBody(json, contentLength: json.Length, nullOptions: true);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ParseAsync_AfterReading_BodyPositionIsReset()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = "Hello" } },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, context.Request.Body.Position);
        }

        [Fact]
        public async Task ParseAsync_WithNullContentLength_ReadsBodyInChunks()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = "Hello" } },
            });
            var context = CreateHttpContextWithBody(json, contentLength: null);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("gpt-4", result.Model);
        }

        [Fact]
        public async Task ParseAsync_WithVeryLongContent_ReadsSuccessfully()
        {
            // Arrange
            var longContent = new string('a', 10000);
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = longContent } },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(longContent, result.Messages[0].Content?.ToString());
        }

        [Fact]
        public async Task ParseAsync_WithUnicodeContent_PreservesCharacters()
        {
            // Arrange
            var unicodeContent = "Hello 世界 🌍 ñáéíóú";
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = unicodeContent } },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(unicodeContent, result.Messages[0].Content?.ToString());
        }

        [Fact]
        public async Task ParseAsync_WithNestedJsonInContent_ParsesCorrectly()
        {
            // Arrange
            var nestedContent = "{\"key\": \"value\", \"number\": 123}";
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = nestedContent } },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nestedContent, result.Messages[0].Content?.ToString());
        }

        [Fact]
        public async Task ParseAsync_WithArrayStopSequences_ParsesCorrectly()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = "Hello" } },
                stop = new[] { "STOP", "END", "HALT" },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Stop);
        }

        [Fact]
        public async Task ParseAsync_WithToolChoice_ParsesCorrectly()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = "Hello" } },
                tool_choice = new { type = "function", function = new { name = "get_weather" } },
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ToolChoice);
        }

        [Fact]
        public async Task ParseAsync_WithZeroMaxTokens_ThrowsBadHttpRequestException()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = "Hello" } },
                max_tokens = 0,
            });
            var context = CreateHttpContextWithBody(json);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
                () => OpenAIRequestParser.ParseAsync(context));

            Assert.Contains("MaxTokens must be a positive integer", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ParseAsync_WithZeroTemperature_ParsesCorrectly()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = "Hello" } },
                temperature = 0.0,
            });
            var context = CreateHttpContextWithBody(json);

            // Act
            var result = await OpenAIRequestParser.ParseAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0.0, result.Temperature);
        }

#pragma warning disable SA1124 // Do not use regions
        #region Helper Methods
#pragma warning restore SA1124 // Do not use regions

        private static HttpContext CreateHttpContextWithBody(
            string body,
            long? contentLength = null,
            long? maxBodySize = null,
            bool throwOnConfig = false,
            bool nullOptions = false)
        {
            var httpContext = new DefaultHttpContext();
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            httpContext.Request.Body = memoryStream;
            httpContext.Request.ContentLength = contentLength;

            // Setup service provider with mocks
            var services = new ServiceCollection();

            if (throwOnConfig)
            {
                var mockServiceProvider = new Mock<IServiceProvider>();
                mockServiceProvider
                    .Setup(sp => sp.GetService(typeof(IOptions<SynaxisConfiguration>)))
                    .Throws(new Exception("Configuration error"));
                httpContext.RequestServices = mockServiceProvider.Object;
            }
            else if (nullOptions)
            {
                var mockOptions = new Mock<IOptions<SynaxisConfiguration>>();
                mockOptions.Setup(o => o.Value).Returns(() => null!);
                services.AddSingleton(mockOptions.Object);
                httpContext.RequestServices = services.BuildServiceProvider();
            }
            else if (maxBodySize.HasValue)
            {
                var config = new SynaxisConfiguration
                {
                    MaxRequestBodySize = maxBodySize.Value,
                };
                services.Configure<SynaxisConfiguration>(opts =>
                {
                    opts.MaxRequestBodySize = config.MaxRequestBodySize;
                });
                httpContext.RequestServices = services.BuildServiceProvider();
            }
            else
            {
                httpContext.RequestServices = services.BuildServiceProvider();
            }

            return httpContext;
        }

        #endregion
    }
}
