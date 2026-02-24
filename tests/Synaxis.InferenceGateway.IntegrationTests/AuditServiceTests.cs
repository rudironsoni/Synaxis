// <copyright file="AuditServiceTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.IntegrationTests;

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Application.Security;
using Synaxis.InferenceGateway.Infrastructure.Security;
using Synaxis.Infrastructure.Data;
using Xunit;
using Xunit.Abstractions;

public class AuditServiceTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output ?? throw new ArgumentNullException(nameof(output));

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_ForNullDbContext()
    {
        Assert.Throws<ArgumentNullException>(() => new AuditService(null!));
    }

    [Fact]
    public void Constructor_ShouldInitializeSuccessfully_WithValidDbContext()
    {
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        Assert.NotNull(service);
    }

    [Fact]
    public async Task LogAsync_ShouldCreateAuditLog_WithAllParameters()
    {
        // Arrange
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var action = "user.login";
        var payload = new { username = "testuser", ipAddress = "192.168.1.1" };

        // Act
        await service.LogAsync(tenantId, userId, action, payload);

        // Assert
        var log = await dbContext.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(tenantId, log.OrganizationId);
        Assert.Equal(userId, log.UserId);
        Assert.Equal(action, log.Action);
        Assert.NotNull(log.Metadata);
        Assert.True(log.Timestamp <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task LogAsync_ShouldCreateAuditLog_WithNullUserId()
    {
        // Arrange
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        var tenantId = Guid.NewGuid();
        var action = "system.startup";
        var payload = new { version = "1.0.0", timestamp = DateTime.UtcNow };

        // Act
        await service.LogAsync(tenantId, null, action, payload);

        // Assert
        var log = await dbContext.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(tenantId, log.OrganizationId);
        Assert.Null(log.UserId);
        Assert.Equal(action, log.Action);
        Assert.NotNull(log.Metadata);
    }

    [Fact]
    public async Task LogAsync_ShouldCreateAuditLog_WithNullPayload()
    {
        // Arrange
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var action = "user.logout";

        // Act
        await service.LogAsync(tenantId, userId, action, null);

        // Assert
        var log = await dbContext.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(tenantId, log.OrganizationId);
        Assert.Equal(userId, log.UserId);
        Assert.Equal(action, log.Action);
        Assert.Empty(log.Metadata);
    }

    [Fact]
    public async Task LogAsync_ShouldCreateAuditLog_WithEmptyPayload()
    {
        // Arrange
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var action = "user.ping";
        var payload = new { }; // Empty object

        // Act
        await service.LogAsync(tenantId, userId, action, payload);

        // Assert
        var log = await dbContext.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(tenantId, log.OrganizationId);
        Assert.Equal(userId, log.UserId);
        Assert.Equal(action, log.Action);
    }

    [Fact]
    public async Task LogAsync_ShouldCreateAuditLog_WithComplexPayload()
    {
        // Arrange
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var action = "api.request";
        var payload = new
        {
            endpoint = "/v1/chat/completions",
            method = "POST",
            headers = new { authorization = "Bearer token", contentType = "application/json" },
            body = new { model = "llama-3.3-70b-versatile", messages = new[] { new { role = "user", content = "Hello" } } },
        };

        // Act
        await service.LogAsync(tenantId, userId, action, payload);

        // Assert
        var log = await dbContext.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(tenantId, log.OrganizationId);
        Assert.Equal(userId, log.UserId);
        Assert.Equal(action, log.Action);
        Assert.NotNull(log.Metadata);
    }

    [Fact]
    public async Task LogAsync_ShouldGenerateUniqueId_ForEachLog()
    {
        // Arrange
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        var tenantId = Guid.NewGuid();
        var action = "test.action";

        // Act
        await service.LogAsync(tenantId, null, action, null);
        await service.LogAsync(tenantId, null, action, null);

        // Assert
        var logs = await dbContext.AuditLogs.ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.NotEqual(logs[0].Id, logs[1].Id);
    }

    [Fact]
    public async Task LogAsync_ShouldSetCorrectTimestamp()
    {
        // Arrange
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        var tenantId = Guid.NewGuid();
        var action = "timestamp.test";
        var beforeLog = DateTime.UtcNow;

        // Act
        await service.LogAsync(tenantId, null, action, null);
        var afterLog = DateTime.UtcNow;

        // Assert
        var log = await dbContext.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.True(log.Timestamp >= beforeLog.AddSeconds(-1));
        Assert.True(log.Timestamp <= afterLog.AddSeconds(1));
    }

    [Fact]
    public async Task LogAsync_ShouldHandleLongActionNames()
    {
        // Arrange
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        var tenantId = Guid.NewGuid();
        var longAction = new string('a', 255);
        var payload = new { test = "value" };

        // Act
        await service.LogAsync(tenantId, null, longAction, payload);

        // Assert
        var log = await dbContext.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(longAction, log.Action);
    }

    [Fact]
    public async Task LogAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        var tenantId = Guid.NewGuid();
        var action = "cancellation.test";
        var cancellationToken = new CancellationToken(true); // Already cancelled

        // Act & Assert - EF Core throws TaskCanceledException
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await service.LogAsync(tenantId, null, action, null, cancellationToken).ConfigureAwait(false));
    }

    [Fact]
    public async Task LogAsync_ShouldHandleSpecialCharactersInAction()
    {
        // Arrange
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        var tenantId = Guid.NewGuid();
        var action = "action-with-dashes_and.underscores";
        var payload = new { special = "chars!@#$%^&*()" };

        // Act
        await service.LogAsync(tenantId, null, action, payload);

        // Assert
        var log = await dbContext.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(action, log.Action);
    }

    [Fact]
    public async Task LogAsync_ShouldHandleMultipleTenants()
    {
        // Arrange
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var action = "multi.tenant";

        // Act
        await service.LogAsync(tenant1, null, action, null);
        await service.LogAsync(tenant2, null, action, null);

        // Assert
        var logs = await dbContext.AuditLogs.ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Equal(tenant1, logs[0].OrganizationId);
        Assert.Equal(tenant2, logs[1].OrganizationId);
    }

    [Fact]
    public async Task LogAsync_ShouldHandlePayloadWithNullValues()
    {
        // Arrange
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        var tenantId = Guid.NewGuid();
        var action = "null.payload";
        var payload = new { value1 = "not null", value2 = (string?)null, value3 = 42 };

        // Act
        await service.LogAsync(tenantId, null, action, payload);

        // Assert
        var log = await dbContext.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.NotNull(log.Metadata);
    }

    [Fact]
    public async Task LogAsync_ShouldHandleLargePayloads()
    {
        // Arrange
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        var tenantId = Guid.NewGuid();
        var action = "large.payload";
        var largeString = new string('x', 10000);
        var payload = new { largeData = largeString };

        // Act
        await service.LogAsync(tenantId, null, action, payload);

        // Assert
        var log = await dbContext.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.NotNull(log.Metadata);
    }

    [Fact]
    public async Task LogAsync_ShouldUseUtcTime()
    {
        // Arrange
        using var dbContext = this.CreateInMemoryDbContext();
        var service = new AuditService(dbContext);

        var tenantId = Guid.NewGuid();
        var action = "utc.time";

        // Act
        await service.LogAsync(tenantId, null, action, null);

        // Assert
        var log = await dbContext.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(DateTimeKind.Utc, log.Timestamp.Kind);
        Assert.True((DateTime.UtcNow - log.Timestamp).TotalSeconds < 5);
    }

    private SynaxisDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SynaxisDbContext(options);
    }
}
