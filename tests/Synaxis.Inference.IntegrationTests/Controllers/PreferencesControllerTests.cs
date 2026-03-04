// <copyright file="PreferencesControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.IntegrationTests.Controllers;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Synaxis.Inference.Api.Controllers;
using Synaxis.Inference.IntegrationTests.Fixtures;
using Xunit;

/// <summary>
/// Integration tests for the PreferencesController.
/// </summary>
[Trait("Category", "Integration")]
[Collection("IntegrationTests")]
public class PreferencesControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Api.Program> factory;
    private readonly HttpClient client;
    private string? createdUserId;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreferencesControllerTests"/> class.
    /// </summary>
    /// <param name="webApplicationFactory">The web application factory.</param>
    public PreferencesControllerTests(CustomWebApplicationFactory webApplicationFactory)
    {
        factory = webApplicationFactory;
        client = factory.CreateClient();
    }

    /// <summary>
    /// Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        client.Dispose();
    }

    #region GET /api/preferences/{userId}

    /// <summary>
    /// Tests that getting preferences for an existing user returns the preferences.
    /// </summary>
    [Fact]
    public async Task GetPreferences_WithExistingUser_ReturnsPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString("N");
        var createRequest = new CreatePreferencesRequest
        {
            UserId = userId,
            DefaultModel = "gpt-4o",
            Temperature = 0.8,
            MaxTokens = 2048,
            StreamingEnabled = true,
            Theme = "dark",
            Language = "en"
        };
        await client.PostAsJsonAsync("/api/preferences", createRequest);
        createdUserId = userId;

        // Act
        var response = await client.GetAsync($"/api/preferences/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preferences = await response.Content.ReadFromJsonAsync<UserPreferences>();
        preferences.Should().NotBeNull();
        preferences!.UserId.Should().Be(userId);
        preferences.DefaultModel.Should().Be("gpt-4o");
        preferences.Temperature.Should().Be(0.8);
        preferences.MaxTokens.Should().Be(2048);
        preferences.StreamingEnabled.Should().BeTrue();
        preferences.Theme.Should().Be("dark");
        preferences.Language.Should().Be("en");
    }

    /// <summary>
    /// Tests that getting preferences for a non-existent user returns 404.
    /// </summary>
    [Fact]
    public async Task GetPreferences_WithNonExistentUser_Returns404NotFound()
    {
        // Act
        var response = await client.GetAsync("/api/preferences/non-existent-user-12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/preferences

    /// <summary>
    /// Tests that creating preferences returns 201 Created.
    /// </summary>
    [Fact]
    public async Task CreatePreferences_WithValidRequest_Returns201Created()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString("N");
        var request = new CreatePreferencesRequest
        {
            UserId = userId,
            DefaultModel = "gpt-4o-mini",
            Temperature = 0.5,
            MaxTokens = 1024,
            StreamingEnabled = false,
            Theme = "light",
            Language = "es"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/preferences", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var preferences = await response.Content.ReadFromJsonAsync<UserPreferences>();
        preferences.Should().NotBeNull();
        preferences!.UserId.Should().Be(userId);
        preferences.DefaultModel.Should().Be("gpt-4o-mini");
        preferences.Temperature.Should().Be(0.5);
        preferences.MaxTokens.Should().Be(1024);
        preferences.StreamingEnabled.Should().BeFalse();
        preferences.Theme.Should().Be("light");
        preferences.Language.Should().Be("es");
        preferences.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        preferences.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/preferences/{userId}");

        // Store for cleanup
        createdUserId = userId;
    }

    /// <summary>
    /// Tests that creating preferences with default values uses sensible defaults.
    /// </summary>
    [Fact]
    public async Task CreatePreferences_WithMinimalRequest_UsesDefaultValues()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString("N");
        var request = new CreatePreferencesRequest
        {
            UserId = userId
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/preferences", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var preferences = await response.Content.ReadFromJsonAsync<UserPreferences>();
        preferences.Should().NotBeNull();
        preferences!.Temperature.Should().Be(0.7); // Default temperature
        preferences.StreamingEnabled.Should().BeTrue(); // Default streaming
        preferences.Theme.Should().Be("system"); // Default theme
        preferences.Language.Should().Be("en"); // Default language

        createdUserId = userId;
    }

    /// <summary>
    /// Tests that creating preferences for an existing user returns 409 Conflict.
    /// </summary>
    [Fact]
    public async Task CreatePreferences_WithExistingUser_Returns409Conflict()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString("N");
        var createRequest = new CreatePreferencesRequest
        {
            UserId = userId,
            DefaultModel = "gpt-4o"
        };
        await client.PostAsJsonAsync("/api/preferences", createRequest);
        createdUserId = userId;

        var duplicateRequest = new CreatePreferencesRequest
        {
            UserId = userId,
            DefaultModel = "gpt-4o-mini"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/preferences", duplicateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// Tests that creating preferences with empty user ID returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task CreatePreferences_WithEmptyUserId_Returns400BadRequest()
    {
        // Arrange
        var request = new CreatePreferencesRequest
        {
            UserId = string.Empty,
            DefaultModel = "gpt-4o"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/preferences", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /api/preferences/{userId}

    /// <summary>
    /// Tests that updating preferences returns 200 OK with updated preferences.
    /// </summary>
    [Fact]
    public async Task UpdatePreferences_WithValidRequest_Returns200Ok()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString("N");
        var createRequest = new CreatePreferencesRequest
        {
            UserId = userId,
            DefaultModel = "gpt-4o",
            Temperature = 0.5,
            Theme = "light"
        };
        await client.PostAsJsonAsync("/api/preferences", createRequest);
        createdUserId = userId;

        var updateRequest = new UpdatePreferencesRequest
        {
            DefaultModel = "gpt-4o-mini",
            Temperature = 0.9,
            Theme = "dark"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/preferences/{userId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preferences = await response.Content.ReadFromJsonAsync<UserPreferences>();
        preferences.Should().NotBeNull();
        preferences!.DefaultModel.Should().Be("gpt-4o-mini");
        preferences.Temperature.Should().Be(0.9);
        preferences.Theme.Should().Be("dark");
        preferences.StreamingEnabled.Should().BeTrue(); // Unchanged
        preferences.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tests that updating preferences with partial data only updates provided fields.
    /// </summary>
    [Fact]
    public async Task UpdatePreferences_WithPartialRequest_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString("N");
        var createRequest = new CreatePreferencesRequest
        {
            UserId = userId,
            DefaultModel = "gpt-4o",
            Temperature = 0.5,
            MaxTokens = 1024,
            StreamingEnabled = true,
            Theme = "light",
            Language = "en"
        };
        await client.PostAsJsonAsync("/api/preferences", createRequest);
        createdUserId = userId;

        var updateRequest = new UpdatePreferencesRequest
        {
            Temperature = 0.9
            // Only updating temperature
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/preferences/{userId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preferences = await response.Content.ReadFromJsonAsync<UserPreferences>();
        preferences.Should().NotBeNull();
        preferences!.Temperature.Should().Be(0.9); // Updated
        preferences.DefaultModel.Should().Be("gpt-4o"); // Unchanged
        preferences.MaxTokens.Should().Be(1024); // Unchanged
        preferences.StreamingEnabled.Should().BeTrue(); // Unchanged
        preferences.Theme.Should().Be("light"); // Unchanged
        preferences.Language.Should().Be("en"); // Unchanged
    }

    /// <summary>
    /// Tests that updating preferences for a non-existent user returns 404.
    /// </summary>
    [Fact]
    public async Task UpdatePreferences_WithNonExistentUser_Returns404NotFound()
    {
        // Arrange
        var updateRequest = new UpdatePreferencesRequest
        {
            DefaultModel = "gpt-4o"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/preferences/non-existent-user", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT /api/preferences/{userId}/default-model

    /// <summary>
    /// Tests that changing the default model returns 200 OK.
    /// </summary>
    [Fact]
    public async Task ChangeDefaultModel_WithValidRequest_Returns200Ok()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString("N");
        var createRequest = new CreatePreferencesRequest
        {
            UserId = userId,
            DefaultModel = "gpt-4o"
        };
        await client.PostAsJsonAsync("/api/preferences", createRequest);
        createdUserId = userId;

        var changeRequest = new ChangeDefaultModelRequest
        {
            ModelId = "gpt-4o-mini"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/preferences/{userId}/default-model", changeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preferences = await response.Content.ReadFromJsonAsync<UserPreferences>();
        preferences.Should().NotBeNull();
        preferences!.DefaultModel.Should().Be("gpt-4o-mini");
        preferences.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tests that changing the default model with empty model ID returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task ChangeDefaultModel_WithEmptyModelId_Returns400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString("N");
        var createRequest = new CreatePreferencesRequest
        {
            UserId = userId,
            DefaultModel = "gpt-4o"
        };
        await client.PostAsJsonAsync("/api/preferences", createRequest);
        createdUserId = userId;

        var changeRequest = new ChangeDefaultModelRequest
        {
            ModelId = string.Empty
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/preferences/{userId}/default-model", changeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Tests that changing the default model for a non-existent user returns 404.
    /// </summary>
    [Fact]
    public async Task ChangeDefaultModel_WithNonExistentUser_Returns404NotFound()
    {
        // Arrange
        var changeRequest = new ChangeDefaultModelRequest
        {
            ModelId = "gpt-4o"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/preferences/non-existent-user/default-model", changeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
