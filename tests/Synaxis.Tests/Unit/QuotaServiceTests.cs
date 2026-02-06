using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.Core.Contracts;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.Services;
using Xunit;

namespace Synaxis.Tests.Unit
{
    public class QuotaServiceTests : IDisposable
    {
        private readonly Mock<IConnectionMultiplexer> _redisMock;
        private readonly Mock<IDatabase> _databaseMock;
        private readonly Mock<ITenantService> _tenantServiceMock;
        private readonly Mock<ILogger<QuotaService>> _loggerMock;
        private readonly SynaxisDbContext _context;
        private readonly QuotaService _service;
        
        public QuotaServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new SynaxisDbContext(options);
            
            // Setup Redis mocks
            _redisMock = new Mock<IConnectionMultiplexer>();
            _databaseMock = new Mock<IDatabase>();
            _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(_databaseMock.Object);
            
            // Setup TenantService mock
            _tenantServiceMock = new Mock<ITenantService>();
            _tenantServiceMock.Setup(t => t.GetOrganizationLimitsAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new OrganizationLimits
                {
                    MaxConcurrentRequests = 10,
                    MonthlyRequestLimit = 10000,
                    MonthlyTokenLimit = 100000,
                    MaxTeams = 1,
                    MaxUsersPerTeam = 3,
                    MaxKeysPerUser = 2,
                    DataRetentionDays = 30
                });
            
            _loggerMock = new Mock<ILogger<QuotaService>>();
            
            _service = new QuotaService(
                _context,
                _redisMock.Object,
                _tenantServiceMock.Object,
                _loggerMock.Object
            );
        }
        
        [Fact]
        public async Task CheckQuotaAsync_WithinLimit_ReturnsAllowed()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var request = new QuotaCheckRequest
            {
                MetricType = "requests",
                IncrementBy = 1,
                TimeGranularity = "minute",
                WindowType = WindowType.Fixed
            };
            
            // Mock Redis response: current=50, ttl=30, status=allowed
            _databaseMock.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisResult.Create(new RedisValue[] { 50, 30, "allowed" }));
            
            // Act
            var result = await _service.CheckQuotaAsync(orgId, request);
            
            // Assert
            Assert.True(result.IsAllowed);
            Assert.Equal(QuotaAction.Allow, result.Action);
        }
        
        [Fact]
        public async Task CheckQuotaAsync_ExceedsLimit_ReturnsThrottled()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var request = new QuotaCheckRequest
            {
                MetricType = "requests",
                IncrementBy = 1,
                TimeGranularity = "minute",
                WindowType = WindowType.Fixed
            };
            
            // Mock Redis response: current=200, ttl=30, status=exceeded
            _databaseMock.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisResult.Create(new RedisValue[] { 200, 30, "exceeded" }));
            
            // Act
            var result = await _service.CheckQuotaAsync(orgId, request);
            
            // Assert
            Assert.False(result.IsAllowed);
            Assert.Equal(QuotaAction.Throttle, result.Action);
            Assert.NotNull(result.Details);
            Assert.Equal(200, result.Details.CurrentUsage);
            Assert.NotNull(result.Details.RetryAfter);
        }
        
        [Fact]
        public async Task CheckQuotaAsync_SlidingWindow_UsesCorrectLuaScript()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var request = new QuotaCheckRequest
            {
                MetricType = "tokens",
                IncrementBy = 100,
                TimeGranularity = "minute",
                WindowType = WindowType.Sliding
            };
            
            // Mock Redis response
            _databaseMock.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisResult.Create(new RedisValue[] { 500, 60, "allowed" }));
            
            // Act
            var result = await _service.CheckQuotaAsync(orgId, request);
            
            // Assert
            Assert.True(result.IsAllowed);
            
            // Verify that ScriptEvaluateAsync was called with 4 arguments (sliding window includes timestamp)
            _databaseMock.Verify(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.Is<RedisValue[]>(args => args.Length == 4), // Sliding window has 4 args
                It.IsAny<CommandFlags>()), Times.Once);
        }
        
        [Fact]
        public async Task CheckUserQuotaAsync_ExceedsUserLimit_ReturnsThrottled()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new QuotaCheckRequest
            {
                MetricType = "requests",
                IncrementBy = 1,
                TimeGranularity = "minute",
                WindowType = WindowType.Fixed
            };
            
            // Mock Redis response: exceeded
            _databaseMock.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisResult.Create(new RedisValue[] { 150, 25, "exceeded" }));
            
            // Act
            var result = await _service.CheckUserQuotaAsync(orgId, userId, request);
            
            // Assert
            Assert.False(result.IsAllowed);
            Assert.Equal(QuotaAction.Throttle, result.Action);
        }
        
        [Fact]
        public async Task IncrementUsageAsync_UpdatesRedisCounters()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var metrics = new UsageMetrics
            {
                UserId = userId,
                MetricType = "tokens",
                Value = 500,
                Model = "gpt-4"
            };
            
            _databaseMock.Setup(db => db.StringIncrementAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<long>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(500);
            
            _databaseMock.Setup(db => db.KeyExpireAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<ExpireWhen>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            
            // Act
            await _service.IncrementUsageAsync(orgId, metrics);
            
            // Assert
            _databaseMock.Verify(db => db.StringIncrementAsync(
                It.IsAny<RedisKey>(),
                500,
                It.IsAny<CommandFlags>()), Times.AtLeast(2)); // Monthly and daily
        }
        
        [Fact]
        public async Task GetEffectiveLimitsAsync_ReturnsOrganizationLimits()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            
            // Act
            var limits = await _service.GetEffectiveLimitsAsync(orgId);
            
            // Assert
            Assert.NotNull(limits);
            Assert.Equal(10, limits.MaxConcurrentRequests);
            Assert.Equal(10000, limits.MonthlyRequestLimit);
            Assert.Equal(100000, limits.MonthlyTokenLimit);
            Assert.True(limits.RequestsPerMinute > 0);
            Assert.True(limits.TokensPerMinute > 0);
        }
        
        [Fact]
        public async Task GetUsageAsync_ReturnsUsageReport()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var query = new UsageQuery
            {
                From = DateTime.UtcNow.AddDays(-30),
                To = DateTime.UtcNow,
                MetricType = "requests",
                Granularity = "month"
            };
            
            _databaseMock.Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue("5000"));
            
            // Act
            var report = await _service.GetUsageAsync(orgId, query);
            
            // Assert
            Assert.NotNull(report);
            Assert.Equal(orgId, report.OrganizationId);
            Assert.NotEmpty(report.UsageByMetric);
            Assert.Equal(5000, report.UsageByMetric["requests"]);
        }
        
        [Fact]
        public async Task ResetUsageAsync_DeletesRedisKeys()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var metricType = "requests";
            
            var mockServer = new Mock<IServer>();
            var mockEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6379);
            
            _redisMock.Setup(r => r.GetEndPoints(It.IsAny<bool>()))
                .Returns(new System.Net.EndPoint[] { mockEndpoint });
            
            _redisMock.Setup(r => r.GetServer(It.IsAny<System.Net.EndPoint>(), It.IsAny<object>()))
                .Returns(mockServer.Object);
            
            var keys = new List<RedisKey>
            {
                $"quota:org:{orgId}:requests:month",
                $"quota:org:{orgId}:requests:day"
            };
            
            mockServer.Setup(s => s.KeysAsync(
                It.IsAny<int>(),
                It.IsAny<RedisValue>(),
                It.IsAny<int>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CommandFlags>()))
                .Returns(AsyncEnumerable(keys));
            
            _databaseMock.Setup(db => db.KeyDeleteAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            
            // Act
            await _service.ResetUsageAsync(orgId, metricType);
            
            // Assert
            _databaseMock.Verify(db => db.KeyDeleteAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()), Times.AtLeastOnce);
        }
        
        [Theory]
        [InlineData("minute", WindowType.Fixed)]
        [InlineData("hour", WindowType.Fixed)]
        [InlineData("day", WindowType.Fixed)]
        [InlineData("minute", WindowType.Sliding)]
        [InlineData("hour", WindowType.Sliding)]
        public async Task CheckQuotaAsync_DifferentGranularities_HandlesCorrectly(string granularity, WindowType windowType)
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var request = new QuotaCheckRequest
            {
                MetricType = "requests",
                IncrementBy = 1,
                TimeGranularity = granularity,
                WindowType = windowType
            };
            
            _databaseMock.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisResult.Create(new RedisValue[] { 50, 30, "allowed" }));
            
            // Act
            var result = await _service.CheckQuotaAsync(orgId, request);
            
            // Assert
            Assert.True(result.IsAllowed);
        }
        
        [Fact]
        public async Task CheckQuotaAsync_RedisFailure_FailsOpen()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var request = new QuotaCheckRequest
            {
                MetricType = "requests",
                IncrementBy = 1,
                TimeGranularity = "minute",
                WindowType = WindowType.Fixed
            };
            
            // Mock Redis failure
            _databaseMock.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
                .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.InternalFailure, "Connection failed"));
            
            // Act
            var result = await _service.CheckQuotaAsync(orgId, request);
            
            // Assert - Should fail open (allow request)
            Assert.True(result.IsAllowed);
        }
        
        public void Dispose()
        {
            _context?.Dispose();
        }
        
        private static async IAsyncEnumerable<T> AsyncEnumerable<T>(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                yield return item;
            }
            await Task.CompletedTask;
        }
    }
}
