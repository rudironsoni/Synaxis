
namespace Synaxis.InferenceGateway.Infrastructure.Tests.Security
{
    public class AuditServiceTests
    {
        [Fact]
        public void Constructor_WithNullDbContext_ThrowsArgumentNullException()
        {
            // Arrange
            ControlPlaneDbContext nullDbContext = null!;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
    using Synaxis.InferenceGateway.Infrastructure.Security;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Threading;
    using System;
    using Xunit;

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
            await auditService.LogAsync(tenantId, userId, action, payload).ConfigureAwait(false);

            // Assert
            var logs = await dbContext.AuditLogs.ToListAsync().ConfigureAwait(false);
            Assert.Single(logs);
            var log = logs[0];
            Assert.Equal(tenantId, log.OrganizationId);
            Assert.Equal(userId, log.UserId);
            Assert.Equal(action, log.Action);
            Assert.NotNull(log.NewValues);
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
            await auditService.LogAsync(tenantId, userId, action, payload).ConfigureAwait(false);

            // Assert
            var logs = await dbContext.AuditLogs.ToListAsync().ConfigureAwait(false);
            Assert.Single(logs);
            var log = logs[0];

            Assert.NotEqual(Guid.Empty, log.Id);
            Assert.Equal(tenantId, log.OrganizationId);
            Assert.Equal(userId, log.UserId);
            Assert.Equal(action, log.Action);
            Assert.NotNull(log.NewValues);

            // Verify payload was serialized correctly
            var deserializedPayload = JsonSerializer.Deserialize<JsonElement>(log.NewValues!);
            Assert.Equal("Value1", deserializedPayload.GetProperty("Property1").GetString());
            Assert.Equal(123, deserializedPayload.GetProperty("Property2").GetInt32());

            // Verify CreatedAt was set
            Assert.True(log.CreatedAt <= DateTimeOffset.UtcNow);
            Assert.True(log.CreatedAt >= DateTimeOffset.UtcNow.AddSeconds(-5)); // Within 5 seconds
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
            await auditService.LogAsync(tenantId, userId, action, payload).ConfigureAwait(false);

            // Assert
            var logs = await dbContext.AuditLogs.ToListAsync().ConfigureAwait(false);
            Assert.Single(logs);
            var log = logs[0];
            Assert.Null(log.NewValues);
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
            await auditService.LogAsync(tenantId, userId, action, payload).ConfigureAwait(false);

            // Assert
            var logs = await dbContext.AuditLogs.ToListAsync().ConfigureAwait(false);
            Assert.Single(logs);
            var log = logs[0];
            Assert.Null(log.UserId);
        }

        private static ControlPlaneDbContext BuildDbContext()
        {
            var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ControlPlaneDbContext(options);
        }
    }
}
