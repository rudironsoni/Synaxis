// <copyright file="AgentExecutionsControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.IntegrationTests.Controllers;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Synaxis.Agents.Application.DTOs;
using Xunit;

[Trait("Category", "Integration")]
public class AgentExecutionsControllerTests : IClassFixture<WebApplicationFactory<Agents.Api.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Agents.Api.Program> _factory;

    public AgentExecutionsControllerTests(WebApplicationFactory<Agents.Api.Program> factory)
    {
        this._factory = factory;
        this._client = factory.CreateClient();
    }

    [Fact]
    public async Task Execute_ValidRequest_ReturnsCreatedWithExecution()
    {
        // Arrange - Create an agent first
        var createAgentRequest = new CreateAgentRequest
        {
            Name = "Execution Test Agent",
            AgentType = "declarative",
            ConfigurationYaml = "name: Test\nsteps: []",
            TenantId = Guid.NewGuid(),
        };

        var createAgentResponse = await this._client.PostAsJsonAsync("/api/agents/configurations", createAgentRequest);
        createAgentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdAgent = await createAgentResponse.Content.ReadFromJsonAsync<AgentDto>();
        createdAgent.Should().NotBeNull();

        var executeRequest = new ExecuteAgentRequest
        {
            AgentId = createdAgent!.Id,
            InputParameters = new Dictionary<string, object>
            {
                { "input", "test value" },
            },
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/agents/executions", executeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AgentExecutionDto>();
        result.Should().NotBeNull();
        result!.AgentId.Should().Be(createdAgent.Id);
    }

    [Fact]
    public async Task GetById_ExistingExecution_ReturnsOkWithExecution()
    {
        // Arrange - Create an agent and execution
        var createAgentRequest = new CreateAgentRequest
        {
            Name = "Get Execution Test Agent",
            AgentType = "declarative",
            ConfigurationYaml = "name: Test\nsteps: []",
            TenantId = Guid.NewGuid(),
        };

        var createAgentResponse = await this._client.PostAsJsonAsync("/api/agents/configurations", createAgentRequest);
        var createdAgent = await createAgentResponse.Content.ReadFromJsonAsync<AgentDto>();
        createdAgent.Should().NotBeNull();

        var executeRequest = new ExecuteAgentRequest
        {
            AgentId = createdAgent!.Id,
        };

        var executeResponse = await this._client.PostAsJsonAsync("/api/agents/executions", executeRequest);
        var createdExecution = await executeResponse.Content.ReadFromJsonAsync<AgentExecutionDto>();
        createdExecution.Should().NotBeNull();

        // Act
        var response = await this._client.GetAsync($"/api/agents/executions/{createdExecution!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AgentExecutionDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdExecution.Id);
    }

    [Fact]
    public async Task GetById_NonExistentExecution_ReturnsNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/agents/executions/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_ExistingExecution_ReturnsNoContent()
    {
        // Arrange - Create an agent and execution
        var createAgentRequest = new CreateAgentRequest
        {
            Name = "Cancel Execution Test Agent",
            AgentType = "declarative",
            ConfigurationYaml = "name: Test\nsteps: []",
            TenantId = Guid.NewGuid(),
        };

        var createAgentResponse = await this._client.PostAsJsonAsync("/api/agents/configurations", createAgentRequest);
        var createdAgent = await createAgentResponse.Content.ReadFromJsonAsync<AgentDto>();
        createdAgent.Should().NotBeNull();

        var executeRequest = new ExecuteAgentRequest
        {
            AgentId = createdAgent!.Id,
        };

        var executeResponse = await this._client.PostAsJsonAsync("/api/agents/executions", executeRequest);
        var createdExecution = await executeResponse.Content.ReadFromJsonAsync<AgentExecutionDto>();
        createdExecution.Should().NotBeNull();

        // Act
        var response = await this._client.PostAsync($"/api/agents/executions/{createdExecution!.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cancel_NonExistentExecution_ReturnsBadRequest()
    {
        // Act
        var response = await this._client.PostAsync($"/api/agents/executions/{Guid.NewGuid()}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
