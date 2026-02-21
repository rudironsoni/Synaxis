// <copyright file="AgentConfigurationsControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.IntegrationTests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synaxis.Agents.Application.DTOs;
using Synaxis.Agents.IntegrationTests;
using Xunit;
using Xunit.Abstractions;

[Trait("Category", "Integration")]
public class AgentConfigurationsControllerTests : IClassFixture<AgentsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly AgentsWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public AgentConfigurationsControllerTests(AgentsWebApplicationFactory factory, ITestOutputHelper output)
    {
        this._factory = factory;
        this._client = factory.CreateClient();
        this._output = output;

        // Debug: Verify event store type
        var eventStore = factory.Services.GetService(typeof(Synaxis.Abstractions.Cloud.IEventStore));
        this._output.WriteLine($"EventStore type: {eventStore?.GetType().FullName ?? "null"}");
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPaginatedResult()
    {
        // Act
        var response = await this._client.GetAsync("/api/agents/configurations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<AgentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_ExistingAgent_ReturnsOkWithAgent()
    {
        // Arrange - Create an agent first
        var createRequest = new CreateAgentRequest
        {
            Name = "Test Agent",
            Description = "Test Description",
            AgentType = "declarative",
            ConfigurationYaml = "name: Test\nsteps: []",
            TenantId = Guid.NewGuid(),
        };

        // Clear event store before test
        using (var scope = this._factory.Services.CreateScope())
        {
            var testEventStore = scope.ServiceProvider.GetRequiredService<Synaxis.Abstractions.Cloud.IEventStore>() as TestEventStore;
            testEventStore?.Clear();
        }

        var createResponse = await this._client.PostAsJsonAsync("/api/agents/configurations", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdAgent = await createResponse.Content.ReadFromJsonAsync<AgentDto>();
        createdAgent.Should().NotBeNull();
        this._output.WriteLine($"Created agent with ID: {createdAgent!.Id}");

        // Debug: Check event store contents
        var eventStore = this._factory.Services.GetRequiredService<Synaxis.Abstractions.Cloud.IEventStore>();
        var streamId = createdAgent.Id.ToString();
        var events = await eventStore.ReadStreamAsync(streamId);
        this._output.WriteLine($"Event store has {events.Count} events for stream {streamId}");

        // Act
        var response = await this._client.GetAsync($"/api/agents/configurations/{createdAgent.Id}");
        var responseContent = await response.Content.ReadAsStringAsync();
        this._output.WriteLine($"Get response: {response.StatusCode} - {responseContent}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AgentDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdAgent.Id);
        result.Name.Should().Be("Test Agent");
    }

    [Fact]
    public async Task GetById_NonExistentAgent_ReturnsNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/agents/configurations/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithAgent()
    {
        // Arrange
        var request = new CreateAgentRequest
        {
            Name = "New Test Agent",
            Description = "A test agent for integration testing",
            AgentType = "declarative",
            ConfigurationYaml = "name: TestAgent\nsteps:\n  - name: Step1\n    type: llm",
            TenantId = Guid.NewGuid(),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/agents/configurations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AgentDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("New Test Agent");
        result.AgentType.Should().Be("declarative");
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange - Empty name should fail validation
        var request = new CreateAgentRequest
        {
            Name = "",
            AgentType = "declarative",
            ConfigurationYaml = "name: Test\nsteps: []",
            TenantId = Guid.NewGuid(),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/agents/configurations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ExistingAgent_ReturnsOkWithUpdatedAgent()
    {
        // Arrange - Create an agent first
        var createRequest = new CreateAgentRequest
        {
            Name = "Original Name",
            Description = "Original Description",
            AgentType = "declarative",
            ConfigurationYaml = "name: Original\nsteps: []",
            TenantId = Guid.NewGuid(),
        };

        var createResponse = await this._client.PostAsJsonAsync("/api/agents/configurations", createRequest);
        var createdAgent = await createResponse.Content.ReadFromJsonAsync<AgentDto>();
        createdAgent.Should().NotBeNull();

        var updateRequest = new UpdateAgentRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            ConfigurationYaml = "name: Updated\nsteps: []",
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/agents/configurations/{createdAgent!.Id}", updateRequest);

        // Debug: Print response details
        var responseContent = await response.Content.ReadAsStringAsync();
        this._output.WriteLine($"Response Status: {response.StatusCode}");
        this._output.WriteLine($"Response Content: {responseContent}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AgentDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Delete_ExistingAgent_ReturnsNoContent()
    {
        // Arrange - Create an agent first
        var createRequest = new CreateAgentRequest
        {
            Name = "Agent To Delete",
            AgentType = "declarative",
            ConfigurationYaml = "name: Delete\nsteps: []",
            TenantId = Guid.NewGuid(),
        };

        var createResponse = await this._client.PostAsJsonAsync("/api/agents/configurations", createRequest);
        var createdAgent = await createResponse.Content.ReadFromJsonAsync<AgentDto>();
        createdAgent.Should().NotBeNull();

        // Act
        var response = await this._client.DeleteAsync($"/api/agents/configurations/{createdAgent!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify agent is deleted
        var getResponse = await this._client.GetAsync($"/api/agents/configurations/{createdAgent.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExistentAgent_ReturnsBadRequest()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/agents/configurations/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
