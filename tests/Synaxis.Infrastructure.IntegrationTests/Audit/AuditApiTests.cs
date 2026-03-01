// <copyright file="AuditApiTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests.Audit;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Services.Audit;
using Xunit;

/// <summary>
/// Integration tests for audit API endpoints.
/// </summary>
public class AuditApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditApiTests"/> class.
    /// </summary>
    /// <param name="factory">The web application factory.</param>
    public AuditApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace services with mocks for testing
                var mockQueryService = new Mock<IAuditQueryService>();
                mockQueryService
                    .Setup(s => s.QueryLogsAsync(It.IsAny<AuditQueryRequest>(), default))
                    .ReturnsAsync(PagedResult<AuditLogDto>.Empty());

                services.AddScoped(_ => mockQueryService.Object);
            });
        });

        _client = _factory.CreateClient();
    }

    /// <summary>
    /// Tests that get logs requires authentication.
    /// </summary>
    [Fact]
    public async Task GetLogs_RequiresAuthentication()
    {
        // Arrange - no authentication header

        // Act
        var response = await _client.GetAsync("/api/v1/audit/logs?organizationId=" + Guid.NewGuid());

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Redirect);
    }

    /// <summary>
    /// Tests that get log by ID requires authentication.
    /// </summary>
    [Fact]
    public async Task GetLog_RequiresAuthentication()
    {
        // Arrange - no authentication header
        var logId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/audit/logs/{logId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Redirect);
    }

    /// <summary>
    /// Tests that get statistics requires authentication.
    /// </summary>
    [Fact]
    public async Task GetStatistics_RequiresAuthentication()
    {
        // Arrange - no authentication header

        // Act
        var response = await _client.GetAsync($"/api/v1/audit/statistics?organizationId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Redirect);
    }

    /// <summary>
    /// Tests that export to JSON requires authentication.
    /// </summary>
    [Fact]
    public async Task ExportToJson_RequiresAuthentication()
    {
        // Arrange - no authentication header
        var request = new AuditExportRequest(
            OrganizationId: Guid.NewGuid(),
            FromDate: DateTime.UtcNow.AddDays(-1),
            ToDate: DateTime.UtcNow.AddDays(1));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/audit/export/json", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Redirect);
    }

    /// <summary>
    /// Tests that export to CSV requires authentication.
    /// </summary>
    [Fact]
    public async Task ExportToCsv_RequiresAuthentication()
    {
        // Arrange - no authentication header
        var request = new AuditExportRequest(
            OrganizationId: Guid.NewGuid(),
            FromDate: DateTime.UtcNow.AddDays(-1),
            ToDate: DateTime.UtcNow.AddDays(1));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/audit/export/csv", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Redirect);
    }

    /// <summary>
    /// Tests that verify integrity requires authentication.
    /// </summary>
    [Fact]
    public async Task VerifyIntegrity_RequiresAuthentication()
    {
        // Arrange - no authentication header
        var logId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/audit/integrity/{logId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Redirect);
    }
}
