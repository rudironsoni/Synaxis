// <copyright file="AuditExportTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests.Audit;

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Synaxis.Infrastructure.Services.Audit;
using Xunit;

/// <summary>
/// Integration tests for audit export functionality.
/// </summary>
[Collection("AuditTests")]
public class AuditExportTests : IClassFixture<AuditTestFixture>
{
    private readonly AuditTestFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditExportTests"/> class.
    /// </summary>
    /// <param name="fixture">The test fixture.</param>
    public AuditExportTests(AuditTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests that export to JSON creates a valid file.
    /// </summary>
    [Fact]
    public async Task ExportToJson_CreatesValidFile()
    {
        // Arrange
        var request = new AuditExportRequest(
            OrganizationId: _fixture.TestOrganizationId,
            FromDate: DateTime.UtcNow.AddDays(-1),
            ToDate: DateTime.UtcNow.AddDays(1));

        // Act
        var result = await _fixture.ExportService.ExportToJsonAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.FilePath.Should().NotBeNullOrEmpty();
        result.RecordCount.Should().BeGreaterThan(0);
        result.FileSizeBytes.Should().BeGreaterThan(0);

        // Verify file exists and contains valid JSON
        File.Exists(result.FilePath).Should().BeTrue();
        var fileContent = await File.ReadAllTextAsync(result.FilePath);
        var jsonDoc = JsonDocument.Parse(fileContent);
        jsonDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);

        // Cleanup
        File.Delete(result.FilePath);
    }

    /// <summary>
    /// Tests that export to CSV creates a valid file.
    /// </summary>
    [Fact]
    public async Task ExportToCsv_CreatesValidFile()
    {
        // Arrange
        var request = new AuditExportRequest(
            OrganizationId: _fixture.TestOrganizationId,
            FromDate: DateTime.UtcNow.AddDays(-1),
            ToDate: DateTime.UtcNow.AddDays(1));

        // Act
        var result = await _fixture.ExportService.ExportToCsvAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.FilePath.Should().NotBeNullOrEmpty();
        result.RecordCount.Should().BeGreaterThan(0);
        result.FileSizeBytes.Should().BeGreaterThan(0);

        // Verify file exists and contains valid CSV
        File.Exists(result.FilePath).Should().BeTrue();
        var fileContent = await File.ReadAllTextAsync(result.FilePath);
        fileContent.Should().Contain("Id,OrganizationId,UserId,EventType");

        // Cleanup
        File.Delete(result.FilePath);
    }

    /// <summary>
    /// Tests that export respects date range.
    /// </summary>
    [Fact]
    public async Task Export_RespectsDateRange()
    {
        // Arrange
        var request = new AuditExportRequest(
            OrganizationId: _fixture.TestOrganizationId,
            FromDate: DateTime.UtcNow.AddHours(-1),
            ToDate: DateTime.UtcNow.AddDays(1));

        // Act
        var result = await _fixture.ExportService.ExportToJsonAsync(request);

        // Assert
        result.RecordCount.Should().BeGreaterThan(0);

        // Cleanup
        File.Delete(result.FilePath);
    }

    /// <summary>
    /// Tests that export returns empty result for organization with no logs.
    /// </summary>
    [Fact]
    public async Task Export_ReturnsEmptyResult_ForOrganizationWithNoLogs()
    {
        // Arrange
        var request = new AuditExportRequest(
            OrganizationId: Guid.NewGuid(), // Non-existent organization
            FromDate: DateTime.UtcNow.AddDays(-1),
            ToDate: DateTime.UtcNow.AddDays(1));

        // Act
        var result = await _fixture.ExportService.ExportToJsonAsync(request);

        // Assert
        result.RecordCount.Should().Be(0);

        // Cleanup
        if (File.Exists(result.FilePath))
        {
            File.Delete(result.FilePath);
        }
    }

    /// <summary>
    /// Tests that generate report throws NotImplementedException for PDF.
    /// </summary>
    [Fact]
    public async Task GenerateReport_ThrowsNotImplementedException_ForPdf()
    {
        // Arrange
        var request = new AuditReportRequest(
            OrganizationId: _fixture.TestOrganizationId,
            FromDate: DateTime.UtcNow.AddDays(-1),
            ToDate: DateTime.UtcNow.AddDays(1));

        // Act & Assert
        var act = () => _fixture.ExportService.GenerateReportAsync(request);
        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
