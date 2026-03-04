// <copyright file="TemplatesControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.IntegrationTests.Controllers;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Synaxis.Inference.Api.Models;
using Synaxis.Inference.IntegrationTests.Fixtures;
using Xunit;

/// <summary>
/// Integration tests for the TemplatesController.
/// </summary>
[Trait("Category", "Integration")]
[Collection("IntegrationTests")]
public class TemplatesControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Api.Program> factory;
    private readonly HttpClient client;
    private string? createdTemplateId;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplatesControllerTests"/> class.
    /// </summary>
    /// <param name="webApplicationFactory">The web application factory.</param>
    public TemplatesControllerTests(CustomWebApplicationFactory webApplicationFactory)
    {
        factory = webApplicationFactory;
        client = factory.CreateClient();
    }

    /// <summary>
    /// Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        // Clean up created template if exists
        if (!string.IsNullOrEmpty(createdTemplateId))
        {
            _ = client.DeleteAsync($"/api/templates/{createdTemplateId}").Result;
        }

        client.Dispose();
    }

    #region GET /api/templates

    /// <summary>
    /// Tests that listing templates returns a list of templates.
    /// </summary>
    [Fact]
    public async Task ListTemplates_WithExistingTemplates_ReturnsListOfTemplates()
    {
        // Arrange - Create a template first
        var createRequest = new CreateTemplateRequest
        {
            Name = "Test Template for Listing",
            Description = "A test template",
            Content = "Hello {{name}}!",
            Variables = ["name"]
        };
        var createResponse = await client.PostAsJsonAsync("/api/templates", createRequest);
        var createdTemplate = await createResponse.Content.ReadFromJsonAsync<PromptTemplate>();
        createdTemplateId = createdTemplate?.Id;

        // Act
        var response = await client.GetAsync("/api/templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var templates = await response.Content.ReadFromJsonAsync<List<PromptTemplate>>();
        templates.Should().NotBeNull();
        templates.Should().Contain(t => t.Name == "Test Template for Listing");
    }

    /// <summary>
    /// Tests that listing templates returns an empty list when no templates exist.
    /// </summary>
    [Fact]
    public async Task ListTemplates_WhenNoTemplates_ReturnsEmptyList()
    {
        // Act
        var response = await client.GetAsync("/api/templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var templates = await response.Content.ReadFromJsonAsync<List<PromptTemplate>>();
        templates.Should().NotBeNull();
    }

    #endregion

    #region GET /api/templates/{id}

    /// <summary>
    /// Tests that getting a specific template returns the template.
    /// </summary>
    [Fact]
    public async Task GetTemplate_WithValidId_ReturnsTemplate()
    {
        // Arrange
        var createRequest = new CreateTemplateRequest
        {
            Name = "Test Template",
            Description = "A test template for get operation",
            Content = "Hello {{name}}!",
            Variables = ["name"]
        };
        var createResponse = await client.PostAsJsonAsync("/api/templates", createRequest);
        var createdTemplate = await createResponse.Content.ReadFromJsonAsync<PromptTemplate>();
        createdTemplateId = createdTemplate?.Id;

        // Act
        var response = await client.GetAsync($"/api/templates/{createdTemplateId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var template = await response.Content.ReadFromJsonAsync<PromptTemplate>();
        template.Should().NotBeNull();
        template!.Id.Should().Be(createdTemplateId);
        template.Name.Should().Be("Test Template");
        template.Content.Should().Be("Hello {{name}}!");
    }

    /// <summary>
    /// Tests that getting a non-existent template returns 404.
    /// </summary>
    [Fact]
    public async Task GetTemplate_WithInvalidId_Returns404NotFound()
    {
        // Act
        var response = await client.GetAsync("/api/templates/non-existent-id-12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/templates

    /// <summary>
    /// Tests that creating a template returns 201 Created with the created template.
    /// </summary>
    [Fact]
    public async Task CreateTemplate_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new CreateTemplateRequest
        {
            Name = "New Test Template",
            Description = "A new test template",
            Content = "Hello {{name}}, welcome to {{place}}!",
            Variables = ["name", "place"]
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/templates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var template = await response.Content.ReadFromJsonAsync<PromptTemplate>();
        template.Should().NotBeNull();
        template!.Name.Should().Be("New Test Template");
        template.Description.Should().Be("A new test template");
        template.Content.Should().Be("Hello {{name}}, welcome to {{place}}!");
        template.Variables.Should().Contain(["name", "place"]);
        template.Id.Should().NotBeNullOrEmpty();

        // Store ID for cleanup
        createdTemplateId = template.Id;

        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/templates/{template.Id}");
    }

    /// <summary>
    /// Tests that creating a template with missing name returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task CreateTemplate_WithMissingName_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateTemplateRequest
        {
            Name = string.Empty,
            Content = "Hello {{name}}!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/templates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Tests that creating a template with missing content returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task CreateTemplate_WithMissingContent_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateTemplateRequest
        {
            Name = "Test Template",
            Content = string.Empty
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/templates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /api/templates/{id}

    /// <summary>
    /// Tests that updating a template returns 200 OK with the updated template.
    /// </summary>
    [Fact]
    public async Task UpdateTemplate_WithValidRequest_Returns200Ok()
    {
        // Arrange
        var createRequest = new CreateTemplateRequest
        {
            Name = "Original Template Name",
            Description = "Original description",
            Content = "Hello {{name}}!"
        };
        var createResponse = await client.PostAsJsonAsync("/api/templates", createRequest);
        var createdTemplate = await createResponse.Content.ReadFromJsonAsync<PromptTemplate>();
        createdTemplateId = createdTemplate?.Id;

        var updateRequest = new UpdateTemplateRequest
        {
            Name = "Updated Template Name",
            Description = "Updated description"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/templates/{createdTemplateId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTemplate = await response.Content.ReadFromJsonAsync<PromptTemplate>();
        updatedTemplate.Should().NotBeNull();
        updatedTemplate!.Name.Should().Be("Updated Template Name");
        updatedTemplate.Description.Should().Be("Updated description");
        updatedTemplate.Content.Should().Be("Hello {{name}}!"); // Unchanged
    }

    /// <summary>
    /// Tests that updating a non-existent template returns 404.
    /// </summary>
    [Fact]
    public async Task UpdateTemplate_WithInvalidId_Returns404NotFound()
    {
        // Arrange
        var updateRequest = new UpdateTemplateRequest
        {
            Name = "Updated Name"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/templates/non-existent-id", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/templates/{id}

    /// <summary>
    /// Tests that deleting a template returns 204 No Content.
    /// </summary>
    [Fact]
    public async Task DeleteTemplate_WithValidId_Returns204NoContent()
    {
        // Arrange
        var createRequest = new CreateTemplateRequest
        {
            Name = "Template to Delete",
            Content = "Hello {{name}}!"
        };
        var createResponse = await client.PostAsJsonAsync("/api/templates", createRequest);
        var createdTemplate = await createResponse.Content.ReadFromJsonAsync<PromptTemplate>();
        var templateId = createdTemplate?.Id;

        // Act
        var response = await client.DeleteAsync($"/api/templates/{templateId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify template is deleted
        var getResponse = await client.GetAsync($"/api/templates/{templateId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Clear ID since we already deleted it
        createdTemplateId = null;
    }

    /// <summary>
    /// Tests that deleting a non-existent template returns 404.
    /// </summary>
    [Fact]
    public async Task DeleteTemplate_WithInvalidId_Returns404NotFound()
    {
        // Act
        var response = await client.DeleteAsync("/api/templates/non-existent-id-12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/templates/{id}/share

    /// <summary>
    /// Tests that sharing a template returns the updated template.
    /// </summary>
    [Fact]
    public async Task ShareTemplate_WithValidRequest_ReturnsUpdatedTemplate()
    {
        // Arrange
        var createRequest = new CreateTemplateRequest
        {
            Name = "Template to Share",
            Content = "Hello {{name}}!"
        };
        var createResponse = await client.PostAsJsonAsync("/api/templates", createRequest);
        var createdTemplate = await createResponse.Content.ReadFromJsonAsync<PromptTemplate>();
        createdTemplateId = createdTemplate?.Id;

        var shareRequest = new ShareTemplateRequest
        {
            UserIds = ["user1", "user2"],
            MakePublic = true
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/templates/{createdTemplateId}/share", shareRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var template = await response.Content.ReadFromJsonAsync<PromptTemplate>();
        template.Should().NotBeNull();
        template!.IsPublic.Should().BeTrue();
    }

    /// <summary>
    /// Tests that sharing a non-existent template returns 404.
    /// </summary>
    [Fact]
    public async Task ShareTemplate_WithInvalidId_Returns404NotFound()
    {
        // Arrange
        var shareRequest = new ShareTemplateRequest
        {
            UserIds = ["user1"]
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/templates/non-existent-id/share", shareRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
