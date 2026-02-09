// <copyright file="AuditServiceTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Security
{
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Models;
    using Synaxis.InferenceGateway.Infrastructure.Security;
    using Synaxis.Infrastructure.Data;
    using Xunit;

    public class AuditServiceTests
    {
        [Fact]
        public void Constructor_WithNullDbContext_ThrowsArgumentNullException()
        {
            // Arrange
            SynaxisDbContext nullDbContext = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AuditService(nullDbContext!));
        }

        [Fact]
        public async Task LogAsync_AddsAuditLogToDbContext()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var action = "TestAction";
            var payload = new { Property1 = "Value1", Property2 = 123 };

            using var dbContext = BuildDbContext();
            var auditService = new AuditService(dbContext);

            // Act
            await auditService.LogAsync(tenantId, userId, action, payload);

            // Assert
            var logs = await dbContext.AuditLogs.ToListAsync();
            Assert.Single(logs);
            var log = logs[0];
            Assert.Equal(tenantId, log.OrganizationId);
            Assert.Equal(userId, log.UserId);
            Assert.Equal(action, log.Action);
            Assert.NotNull(log.Metadata);
        }

        [Fact]
        public async Task LogAsync_SetsCorrectProperties()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var action = "TestAction";
            var payload = new { Property1 = "Value1", Property2 = 123 };

            using var dbContext = BuildDbContext();
            var auditService = new AuditService(dbContext);

            // Act
            await auditService.LogAsync(tenantId, userId, action, payload);

            // Assert
            var logs = await dbContext.AuditLogs.ToListAsync();
            Assert.Single(logs);
            var log = logs[0];

            Assert.NotEqual(Guid.Empty, log.Id);
            Assert.Equal(tenantId, log.OrganizationId);
            Assert.Equal(userId, log.UserId);
            Assert.Equal(action, log.Action);
            Assert.NotNull(log.Metadata);

            // Verify payload was serialized correctly - stored in Metadata["payload"]
            var metadataJson = JsonSerializer.Serialize(log.Metadata);
            var metadata = JsonSerializer.Deserialize<JsonElement>(metadataJson);
            var payloadElement = metadata.GetProperty("payload");
            Assert.Equal("Value1", payloadElement.GetProperty("Property1").GetString());
            Assert.Equal(123, payloadElement.GetProperty("Property2").GetInt32());

            // Verify Timestamp was set
            Assert.True(log.Timestamp <= DateTime.UtcNow);
            Assert.True(log.Timestamp >= DateTime.UtcNow.AddSeconds(-5)); // Within 5 seconds
        }

        [Fact]
        public async Task LogAsync_WithNullPayload_SetsPayloadJsonToNull()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var action = "TestAction";
            object? payload = null;

            using var dbContext = BuildDbContext();
            var auditService = new AuditService(dbContext);

            // Act
            await auditService.LogAsync(tenantId, userId, action, payload);

            // Assert
            var logs = await dbContext.AuditLogs.ToListAsync();
            Assert.Single(logs);
            var log = logs[0];
            Assert.Empty(log.Metadata);
        }

        [Fact]
        public async Task LogAsync_WithNullUserId_SetsUserIdToNull()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            Guid? userId = null;
            var action = "TestAction";
            var payload = new { Property1 = "Value1" };

            using var dbContext = BuildDbContext();
            var auditService = new AuditService(dbContext);

            // Act
            await auditService.LogAsync(tenantId, userId, action, payload);

            // Assert
            var logs = await dbContext.AuditLogs.ToListAsync();
            Assert.Single(logs);
            var log = logs[0];
            Assert.Null(log.UserId);
        }

        private static SynaxisDbContext BuildDbContext()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new SynaxisDbContext(options);
        }
    }
}
