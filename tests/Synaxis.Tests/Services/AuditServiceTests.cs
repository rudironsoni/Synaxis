using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.Services;
using Xunit;

namespace Synaxis.Tests.Services
{
    public class AuditServiceTests : IDisposable
    {
        private readonly SynaxisDbContext _context;
        private readonly Mock<ILogger<AuditService>> _loggerMock;
        private readonly IAuditService _auditService;
        
        public AuditServiceTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new SynaxisDbContext(options);
            _loggerMock = new Mock<ILogger<AuditService>>();
            _auditService = new AuditService(_context, _loggerMock.Object);
        }
        
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
        
        [Fact]
        public async Task LogEventAsync_WithValidEvent_CreatesAuditLog()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            var auditEvent = new AuditEvent
            {
                OrganizationId = org.Id,
                EventType = "user.login",
                EventCategory = "authentication",
                Action = "User logged in",
                ResourceType = "user",
                ResourceId = Guid.NewGuid().ToString(),
                IpAddress = "192.168.1.1",
                UserAgent = "Mozilla/5.0",
                Region = "us-east-1",
                Metadata = new Dictionary<string, object>
                {
                    { "success", true },
                    { "method", "password" }
                }
            };
            
            // Act
            var result = await _auditService.LogEventAsync(auditEvent);
            
            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(org.Id, result.OrganizationId);
            Assert.Equal(auditEvent.EventType, result.EventType);
            Assert.Equal(auditEvent.EventCategory, result.EventCategory);
            Assert.NotNull(result.IntegrityHash);
            Assert.NotNull(result.Timestamp);
        }
        
        [Fact]
        public async Task LogEventAsync_WithNullEvent_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _auditService.LogEventAsync(null)
            );
        }
        
        [Fact]
        public async Task LogEventAsync_WithEmptyOrganizationId_ThrowsException()
        {
            // Arrange
            var auditEvent = new AuditEvent
            {
                OrganizationId = Guid.Empty,
                EventType = "test.event"
            };
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _auditService.LogEventAsync(auditEvent)
            );
        }
        
        [Fact]
        public async Task LogEventAsync_WithEmptyEventType_ThrowsException()
        {
            // Arrange
            var auditEvent = new AuditEvent
            {
                OrganizationId = Guid.NewGuid(),
                EventType = ""
            };
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _auditService.LogEventAsync(auditEvent)
            );
        }
        
        [Fact]
        public async Task LogEventAsync_CreatesChainedHash_WithPreviousLog()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            var event1 = new AuditEvent
            {
                OrganizationId = org.Id,
                EventType = "event.first",
                EventCategory = "test"
            };
            
            var event2 = new AuditEvent
            {
                OrganizationId = org.Id,
                EventType = "event.second",
                EventCategory = "test"
            };
            
            // Act
            var log1 = await _auditService.LogEventAsync(event1);
            var log2 = await _auditService.LogEventAsync(event2);
            
            // Assert
            Assert.Null(log1.PreviousHash); // First log has no previous
            Assert.NotNull(log2.PreviousHash);
            Assert.Equal(log1.IntegrityHash, log2.PreviousHash);
        }
        
        [Fact]
        public async Task LogEventAsync_IsImmutable_CannotBeUpdated()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            var auditEvent = new AuditEvent
            {
                OrganizationId = org.Id,
                EventType = "test.event",
                EventCategory = "test"
            };
            
            // Act
            var log = await _auditService.LogEventAsync(auditEvent);
            var originalHash = log.IntegrityHash;
            
            // Try to modify (should not affect stored value)
            var storedLog = await _context.Set<AuditLog>().FindAsync(log.Id);
            
            // Assert
            Assert.Equal(originalHash, storedLog.IntegrityHash);
            Assert.Equal(log.Timestamp, storedLog.Timestamp);
        }
        
        [Fact]
        public async Task QueryAuditLogsAsync_WithValidQuery_ReturnsLogs()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            // Create multiple audit logs
            for (int i = 0; i < 5; i++)
            {
                var auditEvent = new AuditEvent
                {
                    OrganizationId = org.Id,
                    EventType = $"event.type{i}",
                    EventCategory = "test"
                };
                await _auditService.LogEventAsync(auditEvent);
            }
            
            var query = new AuditQuery
            {
                OrganizationId = org.Id,
                PageSize = 10,
                PageNumber = 1
            };
            
            // Act
            var logs = await _auditService.QueryAuditLogsAsync(query);
            
            // Assert
            Assert.NotNull(logs);
            Assert.Equal(5, logs.Count);
        }
        
        [Fact]
        public async Task QueryAuditLogsAsync_WithEventTypeFilter_ReturnsFilteredLogs()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            await _auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = org.Id,
                EventType = "user.login",
                EventCategory = "auth"
            });
            
            await _auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = org.Id,
                EventType = "user.logout",
                EventCategory = "auth"
            });
            
            await _auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = org.Id,
                EventType = "user.login",
                EventCategory = "auth"
            });
            
            var query = new AuditQuery
            {
                OrganizationId = org.Id,
                EventType = "user.login",
                PageSize = 10,
                PageNumber = 1
            };
            
            // Act
            var logs = await _auditService.QueryAuditLogsAsync(query);
            
            // Assert
            Assert.Equal(2, logs.Count);
            Assert.All(logs, log => Assert.Equal("user.login", log.EventType));
        }
        
        [Fact]
        public async Task QueryAuditLogsAsync_WithDateRange_ReturnsFilteredLogs()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            await _auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = org.Id,
                EventType = "test.event",
                EventCategory = "test"
            });
            
            var query = new AuditQuery
            {
                OrganizationId = org.Id,
                StartDate = DateTime.UtcNow.AddHours(-1),
                EndDate = DateTime.UtcNow.AddHours(1),
                PageSize = 10,
                PageNumber = 1
            };
            
            // Act
            var logs = await _auditService.QueryAuditLogsAsync(query);
            
            // Assert
            Assert.NotEmpty(logs);
        }
        
        [Fact]
        public async Task QueryAuditLogsAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            // Create 15 audit logs
            for (int i = 0; i < 15; i++)
            {
                await _auditService.LogEventAsync(new AuditEvent
                {
                    OrganizationId = org.Id,
                    EventType = $"event.{i}",
                    EventCategory = "test"
                });
            }
            
            var query = new AuditQuery
            {
                OrganizationId = org.Id,
                PageSize = 5,
                PageNumber = 2
            };
            
            // Act
            var logs = await _auditService.QueryAuditLogsAsync(query);
            
            // Assert
            Assert.Equal(5, logs.Count);
        }
        
        [Fact]
        public async Task GetAuditLogAsync_WithValidId_ReturnsLog()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            var createdLog = await _auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = org.Id,
                EventType = "test.event",
                EventCategory = "test"
            });
            
            // Act
            var retrievedLog = await _auditService.GetAuditLogAsync(createdLog.Id);
            
            // Assert
            Assert.NotNull(retrievedLog);
            Assert.Equal(createdLog.Id, retrievedLog.Id);
            Assert.Equal(createdLog.IntegrityHash, retrievedLog.IntegrityHash);
        }
        
        [Fact]
        public async Task GetAuditLogAsync_WithInvalidId_ThrowsException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _auditService.GetAuditLogAsync(invalidId)
            );
        }
        
        [Fact]
        public async Task ExportAuditLogsAsync_WithValidDateRange_ReturnsJsonData()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            await _auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = org.Id,
                EventType = "test.event",
                EventCategory = "test"
            });
            
            var startDate = DateTime.UtcNow.AddHours(-1);
            var endDate = DateTime.UtcNow.AddHours(1);
            
            // Act
            var exportData = await _auditService.ExportAuditLogsAsync(org.Id, startDate, endDate);
            
            // Assert
            Assert.NotNull(exportData);
            Assert.True(exportData.Length > 0);
            
            var json = System.Text.Encoding.UTF8.GetString(exportData);
            Assert.Contains("test.event", json);
        }
        
        [Fact]
        public async Task VerifyIntegrityAsync_WithValidLog_ReturnsTrue()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            var log = await _auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = org.Id,
                EventType = "test.event",
                EventCategory = "test"
            });
            
            // Act
            var isValid = await _auditService.VerifyIntegrityAsync(log.Id);
            
            // Assert
            Assert.True(isValid);
        }
        
        [Fact]
        public async Task VerifyIntegrityAsync_VerifiesChain_WithMultipleLogs()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            var log1 = await _auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = org.Id,
                EventType = "event.first",
                EventCategory = "test"
            });
            
            var log2 = await _auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = org.Id,
                EventType = "event.second",
                EventCategory = "test"
            });
            
            // Act
            var isValid1 = await _auditService.VerifyIntegrityAsync(log1.Id);
            var isValid2 = await _auditService.VerifyIntegrityAsync(log2.Id);
            
            // Assert
            Assert.True(isValid1);
            Assert.True(isValid2);
        }
        
        [Fact]
        public async Task AggregateAnonymizedLogsAsync_ReturnsAggregatedData()
        {
            // Arrange
            var org1 = CreateTestOrganization();
            var org2 = CreateTestOrganization();
            _context.Organizations.Add(org1);
            _context.Organizations.Add(org2);
            await _context.SaveChangesAsync();
            
            // Create logs for different organizations
            await _auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = org1.Id,
                EventType = "user.login",
                EventCategory = "auth",
                Region = "us-east-1"
            });
            
            await _auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = org2.Id,
                EventType = "user.login",
                EventCategory = "auth",
                Region = "eu-west-1"
            });
            
            await _auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = org1.Id,
                EventType = "api.call",
                EventCategory = "api",
                Region = "us-east-1"
            });
            
            var startDate = DateTime.UtcNow.AddHours(-1);
            var endDate = DateTime.UtcNow.AddHours(1);
            
            // Act
            var result = await _auditService.AggregateAnonymizedLogsAsync(startDate, endDate);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalEvents);
            Assert.Equal(2, result.EventsByType["user.login"]);
            Assert.Equal(1, result.EventsByType["api.call"]);
            Assert.Equal(2, result.EventsByCategory["auth"]);
            Assert.Equal(2, result.EventsByRegion["us-east-1"]);
            Assert.Equal(1, result.EventsByRegion["eu-west-1"]);
        }
        
        [Theory]
        [InlineData("user.login", "authentication")]
        [InlineData("user.logout", "authentication")]
        [InlineData("api.call", "api")]
        [InlineData("backup.created", "backup")]
        public async Task LogEventAsync_WithDifferentEventTypes_CreatesCorrectLog(string eventType, string category)
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            var auditEvent = new AuditEvent
            {
                OrganizationId = org.Id,
                EventType = eventType,
                EventCategory = category
            };
            
            // Act
            var log = await _auditService.LogEventAsync(auditEvent);
            
            // Assert
            Assert.Equal(eventType, log.EventType);
            Assert.Equal(category, log.EventCategory);
        }
        
        private Organization CreateTestOrganization()
        {
            return new Organization
            {
                Id = Guid.NewGuid(),
                Slug = $"test-org-{Guid.NewGuid()}",
                Name = "Test Organization",
                PrimaryRegion = "us-east-1",
                Tier = "enterprise",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
