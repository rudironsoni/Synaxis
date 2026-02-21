// <copyright file="WorkflowsControllerTests.cs" company="Synaxis">
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
public class WorkflowsControllerTests : IClassFixture<WebApplicationFactory<Agents.Api.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Agents.Api.Program> _factory;

    public WorkflowsControllerTests(WebApplicationFactory<Agents.Api.Program> factory)
    {
        this._factory = factory;
        this._client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPaginatedResult()
    {
        // Act
        var response = await this._client.GetAsync("/api/workflows");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<AgentWorkflowDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithWorkflow()
    {
        // Arrange
        var request = new CreateWorkflowRequest
        {
            Name = "Test Workflow",
            Description = "A test workflow for integration testing",
            WorkflowYaml = "name: TestWorkflow\nsteps:\n  - name: Step1\n    type: action",
            TenantId = Guid.NewGuid().ToString(),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/workflows", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AgentWorkflowDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Workflow");
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_ExistingWorkflow_ReturnsOkWithWorkflow()
    {
        // Arrange - Create a workflow first
        var createRequest = new CreateWorkflowRequest
        {
            Name = "Get By Id Test Workflow",
            Description = "Test Description",
            WorkflowYaml = "name: Test\nsteps: []",
            TenantId = Guid.NewGuid().ToString(),
        };

        var createResponse = await this._client.PostAsJsonAsync("/api/workflows", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdWorkflow = await createResponse.Content.ReadFromJsonAsync<AgentWorkflowDto>();
        createdWorkflow.Should().NotBeNull();

        // Act
        var response = await this._client.GetAsync($"/api/workflows/{createdWorkflow!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AgentWorkflowDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdWorkflow.Id);
        result.Name.Should().Be("Get By Id Test Workflow");
    }

    [Fact]
    public async Task GetById_NonExistentWorkflow_ReturnsNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/workflows/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Execute_ExistingWorkflow_ReturnsAcceptedWithExecution()
    {
        // Arrange - Create a workflow first
        var createRequest = new CreateWorkflowRequest
        {
            Name = "Execute Test Workflow",
            Description = "Test Description",
            WorkflowYaml = "name: ExecuteTest\nsteps:\n  - name: Step1\n    type: action",
            TenantId = Guid.NewGuid().ToString(),
        };

        var createResponse = await this._client.PostAsJsonAsync("/api/workflows", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdWorkflow = await createResponse.Content.ReadFromJsonAsync<AgentWorkflowDto>();
        createdWorkflow.Should().NotBeNull();

        // Act
        var response = await this._client.PostAsync($"/api/workflows/{createdWorkflow!.Id}/execute", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var result = await response.Content.ReadFromJsonAsync<AgentExecutionDto>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Execute_NonExistentWorkflow_ReturnsBadRequest()
    {
        // Act
        var response = await this._client.PostAsync($"/api/workflows/{Guid.NewGuid()}/execute", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
