// <copyright file="AuditQueryTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests.Audit;

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Synaxis.Infrastructure.Services.Audit;
using Xunit;

/// <summary>
/// Integration tests for audit query functionality.
/// </summary>
[Collection("AuditTests")]
public class AuditQueryTests : IClassFixture<AuditTestFixture>
{
    private readonly AuditTestFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditQueryTests"/> class.
    /// </summary>
    /// <param name="fixture">The test fixture.</param>
    public AuditQueryTests(AuditTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests that query logs returns paged results.
    /// </summary>
    [Fact]
    public async Task QueryLogs_ReturnsPagedResults()
    {
        // Arrange
        var request = new AuditQueryRequest(
            OrganizationId: _fixture.TestOrganizationId,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await _fixture.QueryService.QueryLogsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    /// <summary>
    /// Tests that query logs filters by organization.
    /// </summary>
    [Fact]
    public async Task QueryLogs_FiltersByOrganization()
    {
        // Arrange
        var differentOrgId = Guid.NewGuid();
        var request = new AuditQueryRequest(
            OrganizationId: differentOrgId,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await _fixture.QueryService.QueryLogsAsync(request);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that query logs filters by date range.
    /// </summary>
    [Fact]
    public async Task QueryLogs_FiltersByDateRange()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(1);
        var request = new AuditQueryRequest(
            OrganizationId: _fixture.TestOrganizationId,
            FromDate: from,
            ToDate: to,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await _fixture.QueryService.QueryLogsAsync(request);

        // Assert
        result.Items.Should().NotBeEmpty();
        foreach (var item in result.Items)
        {
            item.Timestamp.Should().BeOnOrAfter(from);
            item.Timestamp.Should().BeOnOrBefore(to);
        }
    }

    /// <summary>
    /// Tests that query logs filters by event type.
    /// </summary>
    [Fact]
    public async Task QueryLogs_FiltersByEventType()
    {
        // Arrange
        var request = new AuditQueryRequest(
            OrganizationId: _fixture.TestOrganizationId,
            EventType: "auth.login",
            Page: 1,
            PageSize: 10);

        // Act
        var result = await _fixture.QueryService.QueryLogsAsync(request);

        // Assert
        result.Items.Should().NotBeEmpty();
        foreach (var item in result.Items)
        {
            item.EventType.Should().Be("auth.login");
        }
    }

    /// <summary>
    /// Tests that query logs filters by user.
    /// </summary>
    [Fact]
    public async Task QueryLogs_FiltersByUser()
    {
        // Arrange
        var request = new AuditQueryRequest(
            OrganizationId: _fixture.TestOrganizationId,
            UserId: _fixture.TestUserId,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await _fixture.QueryService.QueryLogsAsync(request);

        // Assert
        result.Items.Should().NotBeEmpty();
        foreach (var item in result.Items)
        {
            item.UserId.Should().Be(_fixture.TestUserId);
        }
    }

    /// <summary>
    /// Tests that get log by ID returns the correct log.
    /// </summary>
    [Fact]
    public async Task GetLogById_ReturnsCorrectLog()
    {
        // Arrange - first query to get a log ID
        var queryRequest = new AuditQueryRequest(
            OrganizationId: _fixture.TestOrganizationId,
            Page: 1,
            PageSize: 1);

        var queryResult = await _fixture.QueryService.QueryLogsAsync(queryRequest);
        var firstLog = queryResult.Items[0];

        // Act
        var result = await _fixture.QueryService.GetLogByIdAsync(firstLog.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(firstLog.Id);
        result.EventType.Should().Be(firstLog.EventType);
    }

    /// <summary>
    /// Tests that get log by ID returns null for non-existent log.
    /// </summary>
    [Fact]
    public async Task GetLogById_ReturnsNull_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _fixture.QueryService.GetLogByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that get statistics aggregates correctly.
    /// </summary>
    [Fact]
    public async Task GetStatistics_AggregatesCorrectly()
    {
        // Arrange & Act
        var result = await _fixture.QueryService.GetStatisticsAsync(_fixture.TestOrganizationId);

        // Assert
        result.Should().NotBeNull();
        result.TotalEvents.Should().BeGreaterThan(0);
        result.EventsByType.Should().NotBeEmpty();
        result.EventsByCategory.Should().NotBeEmpty();
        result.EventsOverTime.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that get statistics respects date filters.
    /// </summary>
    [Fact]
    public async Task GetStatistics_RespectsDateFilters()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(1);

        // Act
        var result = await _fixture.QueryService.GetStatisticsAsync(
            _fixture.TestOrganizationId,
            from,
            to);

        // Assert
        result.TotalEvents.Should().BeGreaterThan(0);
    }
}
