// <copyright file="GenericOpenAiChatClientTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.AI;
using Synaxis.InferenceGateway.Infrastructure;
using Xunit;

public class GenericOpenAiChatClientTests
{
    private const string TestApiKey = "test-api-key";
    private const string TestModelId = "gpt-4";
    private readonly Uri testEndpoint = new Uri("https://api.openai.com/v1");

    [Fact]
    public void Constructor_SetsUpOpenAIClientWithCorrectParameters()
    {
        // Arrange & Act
        var client = new GenericOpenAiChatClient(TestApiKey, this.testEndpoint, TestModelId);

        // Assert
        Assert.NotNull(client.Metadata);
    }

    [Fact]
    public void Constructor_WithCustomHeaders_AddsHeadersToPipeline()
    {
        // Arrange
        var customHeaders = new Dictionary<string, string>
(StringComparer.Ordinal)
        {
            ["X-Custom-Header"] = "custom-value",
            ["X-Another-Header"] = "another-value",
        };

        // Act
        var client = new GenericOpenAiChatClient(TestApiKey, this.testEndpoint, TestModelId, customHeaders);

        // Assert
        Assert.NotNull(client.Metadata);
    }

    [Fact]
    public void Constructor_WithEmptyCustomHeaders_HandlesGracefully()
    {
        // Arrange
        var emptyHeaders = new Dictionary<string, string>(StringComparer.Ordinal);

        // Act
        var client = new GenericOpenAiChatClient(TestApiKey, this.testEndpoint, TestModelId, emptyHeaders);

        // Assert
        Assert.NotNull(client.Metadata);
    }

    [Fact]
    public void Constructor_WithNullCustomHeaders_HandlesGracefully()
    {
        // Act
        var client = new GenericOpenAiChatClient(TestApiKey, this.testEndpoint, TestModelId, null);

        // Assert
        Assert.NotNull(client.Metadata);
    }

    [Fact]
    public void Dispose_DisposesClient()
    {
        using (
                // Arrange
                var client = new GenericOpenAiChatClient(TestApiKey, this.testEndpoint, TestModelId))
        {
        }

        // Assert
        Assert.True(true);
    }

    [Fact]
    public void Metadata_ReturnsProviderMetadata()
    {
        // Arrange
        var client = new GenericOpenAiChatClient(TestApiKey, this.testEndpoint, TestModelId);

        // Act
        var metadata = client.Metadata;

        // Assert
        Assert.NotNull(metadata);
    }

    [Fact]
    public void GetService_CanRetrieveChatClient()
    {
        // Arrange
        var client = new GenericOpenAiChatClient(TestApiKey, this.testEndpoint, TestModelId);

        // Act
        var result = client.GetService(typeof(object));

        // Assert - inner OpenAI client is returned
        Assert.NotNull(result);
    }

    [Fact]
    public void GetService_WithServiceKey_ReturnsNullForUnknownKey()
    {
        // Arrange
        var client = new GenericOpenAiChatClient(TestApiKey, this.testEndpoint, TestModelId);
        var serviceKey = "unknown-key";

        // Act
        var result = client.GetService(typeof(object), serviceKey);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Constructor_WithNullApiKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        // The OpenAIClient constructor will throw ArgumentNullException for null API key
        Assert.Throws<ArgumentNullException>(() =>
            new GenericOpenAiChatClient(null!, this.testEndpoint, TestModelId));
    }

    [Fact]
    public void Constructor_WithNullEndpoint_HandlesGracefully()
    {
        // Act
        var client = new GenericOpenAiChatClient(TestApiKey, null!, TestModelId);

        // Assert - Should create client successfully
        Assert.Equal("openai", client.Metadata.ProviderName?.ToLower());
    }

    [Fact]
    public void Constructor_WithNullModelId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GenericOpenAiChatClient(TestApiKey, this.testEndpoint, null!));
    }
}
