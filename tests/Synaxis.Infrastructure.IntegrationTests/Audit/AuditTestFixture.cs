// <copyright file="AuditTestFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests.Audit;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.Repositories;
using Synaxis.Infrastructure.Services.Audit;
using Xunit;

/// <summary>
/// Test fixture for audit integration tests.
/// Provides shared setup for audit-related tests.
/// </summary>
public class AuditTestFixture : IAsyncLifetime
{
    /// <summary>
    /// Gets the database context for tests.
    /// </summary>
    public SynaxisDbContext DbContext { get; private set; } = null!;

    /// <summary>
    /// Gets the audit log repository.
    /// </summary>
    public IAuditLogRepository Repository { get; private set; } = null!;

    /// <summary>
    /// Gets the audit query service.
    /// </summary>
    public IAuditQueryService QueryService { get; private set; } = null!;

    /// <summary>
    /// Gets the audit export service.
    /// </summary>
    public IAuditExportService ExportService { get; private set; } = null!;

    /// <summary>
    /// Gets the test organization ID.
    /// </summary>
    public Guid TestOrganizationId { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the test user ID.
    /// </summary>
    public Guid TestUserId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: $"AuditTests_{Guid.NewGuid()}")
            .Options;

        DbContext = new SynaxisDbContext(options);

        var loggerMock = new Mock<ILogger<AuditLogRepository>>();
        var queryLoggerMock = new Mock<ILogger<AuditQueryService>>();
        var exportLoggerMock = new Mock<ILogger<AuditExportService>>();

        Repository = new AuditLogRepository(DbContext, loggerMock.Object);
        QueryService = new AuditQueryService(Repository, queryLoggerMock.Object);
        ExportService = new AuditExportService(Repository, exportLoggerMock.Object);

        // Seed test data
        await SeedTestDataAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
    }

    /// <summary>
    /// Seeds test data for audit tests.
    /// </summary>
    private async Task SeedTestDataAsync()
    {
        var auditLogs = new List<AuditLog>
        {
            CreateAuditLog("auth.login", "authentication", "login", "user", TestUserId.ToString()),
            CreateAuditLog("auth.logout", "authentication", "logout", "user", TestUserId.ToString()),
            CreateAuditLog("data.read", "data_access", "read", "document", Guid.NewGuid().ToString()),
            CreateAuditLog("data.write", "data_access", "write", "document", Guid.NewGuid().ToString()),
            CreateAuditLog("auth.login_failed", "authentication", "failed_login", "user", TestUserId.ToString()),
            CreateAuditLog("auth.login_failed", "authentication", "failed_login", "user", TestUserId.ToString()),
            CreateAuditLog("auth.login_failed", "authentication", "failed_login", "user", TestUserId.ToString()),
            CreateAuditLog("data.export", "data_access", "export", "report", Guid.NewGuid().ToString()),
        };

        foreach (var log in auditLogs)
        {
            await Repository.AddAsync(log);
        }
    }

    /// <summary>
    /// Creates a test audit log.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="eventCategory">The event category.</param>
    /// <param name="action">The action.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <returns>A new audit log.</returns>
    public AuditLog CreateAuditLog(
        string eventType,
        string eventCategory,
        string action,
        string resourceType,
        string resourceId)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = TestOrganizationId,
            UserId = TestUserId,
            EventType = eventType,
            EventCategory = eventCategory,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal),
            IpAddress = "192.168.1.1",
            UserAgent = "TestAgent/1.0",
            Region = "us-east-1",
            IntegrityHash = "test-hash",
            PreviousHash = string.Empty,
            Timestamp = DateTime.UtcNow,
        };
    }
}

/// <summary>
/// Collection definition for audit tests.
/// </summary>
[CollectionDefinition("AuditTests")]
public class AuditTestCollection : ICollectionFixture<AuditTestFixture>
{
}
