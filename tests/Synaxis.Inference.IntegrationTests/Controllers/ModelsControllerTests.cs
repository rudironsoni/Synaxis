// <copyright file="ModelsControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.IntegrationTests.Controllers;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Synaxis.Inference.Api.Controllers;
using Synaxis.Inference.Api.Models;
using Synaxis.Inference.IntegrationTests.Fixtures;
using Xunit;

/// <summary>
/// Integration tests for the ModelsController.
/// </summary>
[Trait("Category", "Integration")]
[Collection("IntegrationTests")]
public class ModelsControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Api.Program> factory;
    private readonly HttpClient client;
    private string? createdModelId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelsControllerTests"/> class.
    /// </summary>
    /// <param name="webApplicationFactory">The web application factory.</param>
    public ModelsControllerTests(CustomWebApplicationFactory webApplicationFactory)
    {
        factory = webApplicationFactory;
        client = factory.CreateClient();
    }

    /// <summary>
    /// Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        // Clean up created model if exists
        if (!string.IsNullOrEmpty(createdModelId))
        {
            _ = client.DeleteAsync($"/api/models/{createdModelId}").Result;
        }

        client.Dispose();
    }

    #region GET /api/models

    /// <summary>
    /// Tests that listing models returns available models.
    /// </summary>
    [Fact]
    public async Task ListModels_ReturnsAvailableModels()
    {
        // Act
        var response = await client.GetAsync("/api/models");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var modelList = await response.Content.ReadFromJsonAsync<ModelListResponse>();
        modelList.Should().NotBeNull();
        modelList!.Object.Should().Be("list");
        modelList.Data.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that listed models have required properties.
    /// </summary>
    [Fact]
    public async Task ListModels_ModelsHaveRequiredProperties()
    {
        // Act
        var response = await client.GetAsync("/api/models");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var modelList = await response.Content.ReadFromJsonAsync<ModelListResponse>();
        modelList.Should().NotBeNull();

        foreach (var model in modelList!.Data)
        {
            model.Id.Should().NotBeNullOrEmpty();
            model.Object.Should().Be("model");
            model.OwnedBy.Should().NotBeNullOrEmpty();
            model.Capabilities.Should().NotBeNull();
        }
    }

    #endregion

    #region GET /api/models/{id}

    /// <summary>
    /// Tests that getting a specific model returns the model details.
    /// </summary>
    [Fact]
    public async Task GetModel_WithValidId_ReturnsModelDetails()
    {
        // Arrange - Register a model first
        var modelId = $"test-model-{Guid.NewGuid():N}";
        var registerRequest = new RegisterModelRequest
        {
            Id = modelId,
            Provider = "test-provider",
            Description = "A test model",
            ContextWindow = 128000,
            MaxTokens = 4096,
            SupportsStreaming = true,
            SupportsFunctionCalling = true,
            SupportsVision = false,
            SupportsJsonMode = true
        };
        var registerResponse = await client.PostAsJsonAsync("/api/models", registerRequest);
        createdModelId = modelId;

        // Act
        var response = await client.GetAsync($"/api/models/{modelId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var model = await response.Content.ReadFromJsonAsync<ModelDetailResponse>();
        model.Should().NotBeNull();
        model!.Id.Should().Be(modelId);
        model.OwnedBy.Should().Be("test-provider");
        model.Description.Should().Be("A test model");
        model.ContextWindow.Should().Be(128000);
        model.MaxTokens.Should().Be(4096);
        model.Capabilities.Streaming.Should().BeTrue();
        model.Capabilities.FunctionCalling.Should().BeTrue();
        model.Capabilities.Vision.Should().BeFalse();
        model.Capabilities.JsonMode.Should().BeTrue();
    }

    /// <summary>
    /// Tests that getting a non-existent model returns 404.
    /// </summary>
    [Fact]
    public async Task GetModel_WithInvalidId_Returns404NotFound()
    {
        // Act
        var response = await client.GetAsync("/api/models/non-existent-model-12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("model_not_found");
    }

    #endregion

    #region POST /api/models

    /// <summary>
    /// Tests that registering a model returns 201 Created.
    /// </summary>
    [Fact]
    public async Task RegisterModel_WithValidRequest_Returns201Created()
    {
        // Arrange
        var modelId = $"new-model-{Guid.NewGuid():N}";
        var request = new RegisterModelRequest
        {
            Id = modelId,
            Provider = "openai",
            Description = "A new test model",
            ContextWindow = 8192,
            MaxTokens = 4096,
            SupportsStreaming = true,
            SupportsFunctionCalling = false,
            SupportsVision = true,
            SupportsJsonMode = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/models", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var model = await response.Content.ReadFromJsonAsync<ModelDetailResponse>();
        model.Should().NotBeNull();
        model!.Id.Should().Be(modelId);
        model.OwnedBy.Should().Be("openai");
        model.Description.Should().Be("A new test model");
        model.ContextWindow.Should().Be(8192);
        model.Capabilities.Streaming.Should().BeTrue();
        model.Capabilities.Vision.Should().BeTrue();

        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/models/{modelId}");

        // Store for cleanup
        createdModelId = modelId;
    }

    /// <summary>
    /// Tests that registering a model with minimal required fields succeeds.
    /// </summary>
    [Fact]
    public async Task RegisterModel_WithMinimalFields_UsesDefaults()
    {
        // Arrange
        var modelId = $"minimal-model-{Guid.NewGuid():N}";
        var request = new RegisterModelRequest
        {
            Id = modelId,
            Provider = "anthropic"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/models", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var model = await response.Content.ReadFromJsonAsync<ModelDetailResponse>();
        model.Should().NotBeNull();
        model!.ContextWindow.Should().Be(128000); // Default value
        model.Capabilities.Streaming.Should().BeTrue(); // Default
        model.Capabilities.FunctionCalling.Should().BeFalse(); // Default
        model.Capabilities.Vision.Should().BeFalse(); // Default
        model.Capabilities.JsonMode.Should().BeTrue(); // Default

        createdModelId = modelId;
    }

    /// <summary>
    /// Tests that registering a duplicate model returns 409 Conflict.
    /// </summary>
    [Fact]
    public async Task RegisterModel_WithDuplicateId_Returns409Conflict()
    {
        // Arrange
        var modelId = $"duplicate-model-{Guid.NewGuid():N}";
        var request = new RegisterModelRequest
        {
            Id = modelId,
            Provider = "test"
        };
        await client.PostAsJsonAsync("/api/models", request);
        createdModelId = modelId;

        // Act - Try to register again
        var response = await client.PostAsJsonAsync("/api/models", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// Tests that registering a model with empty ID returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task RegisterModel_WithEmptyId_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterModelRequest
        {
            Id = string.Empty,
            Provider = "test"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/models", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Tests that registering a model with empty provider returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task RegisterModel_WithEmptyProvider_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterModelRequest
        {
            Id = "test-model",
            Provider = string.Empty
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/models", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /api/models/{id}

    /// <summary>
    /// Tests that updating a model returns 200 OK with updated model.
    /// </summary>
    [Fact]
    public async Task UpdateModel_WithValidRequest_Returns200Ok()
    {
        // Arrange
        var modelId = $"update-model-{Guid.NewGuid():N}";
        var registerRequest = new RegisterModelRequest
        {
            Id = modelId,
            Provider = "test",
            Description = "Original description",
            ContextWindow = 8192,
            SupportsStreaming = false
        };
        await client.PostAsJsonAsync("/api/models", registerRequest);
        createdModelId = modelId;

        var updateRequest = new UpdateModelRequest
        {
            Description = "Updated description",
            ContextWindow = 16384,
            SupportsStreaming = true
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/models/{modelId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var model = await response.Content.ReadFromJsonAsync<ModelDetailResponse>();
        model.Should().NotBeNull();
        model!.Description.Should().Be("Updated description");
        model.ContextWindow.Should().Be(16384);
        model.Capabilities.Streaming.Should().BeTrue();
    }

    /// <summary>
    /// Tests that updating a non-existent model returns 404.
    /// </summary>
    [Fact]
    public async Task UpdateModel_WithInvalidId_Returns404NotFound()
    {
        // Arrange
        var updateRequest = new UpdateModelRequest
        {
            Description = "New description"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/models/non-existent-model", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Tests that updating with partial data only updates provided fields.
    /// </summary>
    [Fact]
    public async Task UpdateModel_WithPartialRequest_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var modelId = $"partial-model-{Guid.NewGuid():N}";
        var registerRequest = new RegisterModelRequest
        {
            Id = modelId,
            Provider = "test",
            Description = "Original",
            ContextWindow = 8192,
            MaxTokens = 2048,
            SupportsStreaming = false,
            SupportsFunctionCalling = false,
            SupportsVision = false,
            SupportsJsonMode = false
        };
        await client.PostAsJsonAsync("/api/models", registerRequest);
        createdModelId = modelId;

        var updateRequest = new UpdateModelRequest
        {
            Description = "Updated"
            // Only updating description
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/models/{modelId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var model = await response.Content.ReadFromJsonAsync<ModelDetailResponse>();
        model.Should().NotBeNull();
        model!.Description.Should().Be("Updated"); // Changed
        model.ContextWindow.Should().Be(8192); // Unchanged
        model.MaxTokens.Should().Be(2048); // Unchanged
        model.Capabilities.Streaming.Should().BeFalse(); // Unchanged
    }

    #endregion

    #region DELETE /api/models/{id}

    /// <summary>
    /// Tests that removing a model returns 204 No Content.
    /// </summary>
    [Fact]
    public async Task RemoveModel_WithValidId_Returns204NoContent()
    {
        // Arrange
        var modelId = $"remove-model-{Guid.NewGuid():N}";
        var registerRequest = new RegisterModelRequest
        {
            Id = modelId,
            Provider = "test"
        };
        await client.PostAsJsonAsync("/api/models", registerRequest);

        // Act
        var response = await client.DeleteAsync($"/api/models/{modelId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify model is removed
        var getResponse = await client.GetAsync($"/api/models/{modelId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Already deleted, no cleanup needed
        createdModelId = null;
    }

    /// <summary>
    /// Tests that removing a non-existent model returns 404.
    /// </summary>
    [Fact]
    public async Task RemoveModel_WithInvalidId_Returns404NotFound()
    {
        // Act
        var response = await client.DeleteAsync("/api/models/non-existent-model-12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/models/providers

    /// <summary>
    /// Tests that listing providers returns available providers.
    /// </summary>
    [Fact]
    public async Task ListProviders_ReturnsAvailableProviders()
    {
        // Act
        var response = await client.GetAsync("/api/models/providers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var providersResponse = await response.Content.ReadFromJsonAsync<ProvidersResponse>();
        providersResponse.Should().NotBeNull();
        providersResponse!.Providers.Should().NotBeNull();
    }

    #endregion

    #region GET /api/models/providers/{provider}/models

    /// <summary>
    /// Tests that listing models for a provider returns provider models.
    /// </summary>
    [Fact]
    public async Task ListProviderModels_WithValidProvider_ReturnsModels()
    {
        // Arrange - Register a model for a specific provider
        var modelId = $"provider-model-{Guid.NewGuid():N}";
        var registerRequest = new RegisterModelRequest
        {
            Id = modelId,
            Provider = "custom-provider",
            Description = "A custom provider model"
        };
        await client.PostAsJsonAsync("/api/models", registerRequest);
        createdModelId = modelId;

        // Act
        var response = await client.GetAsync("/api/models/providers/custom-provider/models");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var providerModels = await response.Content.ReadFromJsonAsync<ProviderModelsResponse>();
        providerModels.Should().NotBeNull();
        providerModels!.Provider.Should().Be("custom-provider");
        providerModels.Models.Should().Contain(m => m.Id == modelId);
    }

    /// <summary>
    /// Tests that listing models for a non-existent provider returns 404.
    /// </summary>
    [Fact]
    public async Task ListProviderModels_WithInvalidProvider_Returns404NotFound()
    {
        // Act
        var response = await client.GetAsync("/api/models/providers/non-existent-provider/models");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
