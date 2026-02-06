 using System;
 using System.Collections.Generic;
 using System.Net;
 using System.Net.Http;
 using System.Threading;
 using System.Threading.Tasks;
 using Microsoft.EntityFrameworkCore;
 using Microsoft.Extensions.Logging;
 using Moq;
 using Moq.Protected;
 using StackExchange.Redis;
 using Synaxis.Core.Models;
 using Synaxis.Infrastructure.Data;
 using Synaxis.Infrastructure.Services;
 using Xunit;

namespace Synaxis.Tests.Unit
{
    public class HealthMonitorTests : IDisposable
    {
        private readonly Mock<IConnectionMultiplexer> _redisMock;
        private readonly Mock<IDatabase> _databaseMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<ILogger<HealthMonitor>> _loggerMock;
        private readonly SynaxisDbContext _context;
        private readonly HealthMonitor _service;
        
        public HealthMonitorTests()
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
            
            // Setup HttpClient mock
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<HealthMonitor>>();
            
            _service = new HealthMonitor(
                _context,
                _redisMock.Object,
                _httpClientFactoryMock.Object,
                _loggerMock.Object
            );
        }
        
        [Fact]
        public async Task CheckRegionHealthAsync_AllHealthy_ReturnsHealthyStatus()
        {
            // Arrange
            var region = "eu-west-1";
            
            // Mock successful database query
            // (In-memory DB is accessible by default)
            
            // Mock successful Redis ping
            _databaseMock.Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            
            _databaseMock.Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue("123456"));
            
            // Mock HTTP client for provider checks
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"status\":\"operational\"}")
                });
            
            var httpClient = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
            
            // Act
            var health = await _service.CheckRegionHealthAsync(region);
            
            // Assert
            Assert.Equal(region, health.Region);
            Assert.True(health.IsHealthy);
            Assert.Equal("healthy", health.Status);
            Assert.True(health.HealthScore >= 70);
            Assert.True(health.DatabaseHealthy);
            Assert.True(health.RedisHealthy);
        }
        
        [Fact]
        public async Task CheckRegionHealthAsync_DatabaseUnhealthy_ReturnsDegradedStatus()
        {
            // Arrange
            var region = "us-east-1";
            
            // Mock database failure by using disposed context
            var disposedContext = new SynaxisDbContext(
                new DbContextOptionsBuilder<SynaxisDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options);
            disposedContext.Dispose();
            
            var serviceWithBadDb = new HealthMonitor(
                disposedContext,
                _redisMock.Object,
                _httpClientFactoryMock.Object,
                _loggerMock.Object
            );
            
            // Mock successful Redis
            _databaseMock.Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            
            _databaseMock.Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue("123456"));
            
            // Mock HTTP client
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
            
            var httpClient = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
            
            // Act
            var health = await serviceWithBadDb.CheckRegionHealthAsync(region);
            
            // Assert
            Assert.False(health.DatabaseHealthy);
            Assert.Contains(health.Issues, i => i.Contains("Database"));
            Assert.True(health.HealthScore < 100); // Should be penalized
        }
        
        [Fact]
        public async Task CheckDatabaseHealthAsync_FastQuery_ReturnsTrue()
        {
            // Arrange
            var region = "eu-west-1";
            
            // Act
            var healthy = await _service.CheckDatabaseHealthAsync(region);
            
            // Assert
            Assert.True(healthy); // In-memory DB should be fast
        }
        
        [Fact]
        public async Task CheckRedisHealthAsync_SuccessfulPing_ReturnsTrue()
        {
            // Arrange
            var region = "us-east-1";
            
            _databaseMock.Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            
            _databaseMock.Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue("123456"));
            
            // Act
            var healthy = await _service.CheckRedisHealthAsync(region);
            
            // Assert
            Assert.True(healthy);
        }
        
        [Fact]
        public async Task CheckRedisHealthAsync_Failure_ReturnsFalse()
        {
            // Arrange
            var region = "sa-east-1";
            
            _databaseMock.Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.SocketFailure, "Connection lost"));
            
            // Act
            var healthy = await _service.CheckRedisHealthAsync(region);
            
            // Assert
            Assert.False(healthy);
        }
        
        [Fact]
        public async Task CheckProviderHealthAsync_SuccessfulResponse_ReturnsAvailable()
        {
            // Arrange
            var provider = "openai";
            
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"status\":\"operational\"}")
                });
            
            var httpClient = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
            
            // Act
            var health = await _service.CheckProviderHealthAsync(provider);
            
            // Assert
            Assert.Equal(provider, health.Provider);
            Assert.True(health.IsAvailable);
            Assert.Equal("operational", health.Status);
            Assert.True(health.ResponseTimeMs >= 0);
        }
        
        [Fact]
        public async Task CheckProviderHealthAsync_Timeout_ReturnsUnavailable()
        {
            // Arrange
            var provider = "openai";
            
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Request timeout"));
            
            var httpClient = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
            
            // Act
            var health = await _service.CheckProviderHealthAsync(provider);
            
            // Assert
            Assert.False(health.IsAvailable);
            Assert.Equal("unavailable", health.Status);
        }
        
        [Fact]
        public async Task GetHealthScoreAsync_ReturnsCorrectScore()
        {
            // Arrange
            var region = "eu-west-1";
            
            _databaseMock.Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            
            _databaseMock.Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue("123456"));
            
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
            
            var httpClient = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
            
            // Act
            var score = await _service.GetHealthScoreAsync(region);
            
            // Assert
            Assert.True(score >= 0 && score <= 100);
        }
        
        [Fact]
        public async Task IsRegionHealthyAsync_HealthyRegion_ReturnsTrue()
        {
            // Arrange
            var region = "us-east-1";
            
            _databaseMock.Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            
            _databaseMock.Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue("123456"));
            
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
            
            var httpClient = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
            
            // Act
            var isHealthy = await _service.IsRegionHealthyAsync(region);
            
            // Assert
            Assert.True(isHealthy);
        }
        
        [Fact]
        public async Task GetNearestHealthyRegionAsync_SelectsClosestHealthyRegion()
        {
            // Arrange
            var fromRegion = "eu-west-1";
            var availableRegions = new List<string> { "us-east-1", "sa-east-1" };
            
            // Add test organizations to ensure database health checks pass
            await _context.Organizations.AddAsync(new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-us",
                Name = "US Org",
                PrimaryRegion = "us-east-1",
                Tier = "enterprise",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _context.Organizations.AddAsync(new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-sa",
                Name = "SA Org",
                PrimaryRegion = "sa-east-1",
                Tier = "enterprise",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            
            // Mock both regions as healthy
            _databaseMock.Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            
            _databaseMock.Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue("123456"));
            
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
            
            var httpClient = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
            
            // Act
            var nearestRegion = await _service.GetNearestHealthyRegionAsync(fromRegion, availableRegions);
            
            // Assert
            Assert.NotNull(nearestRegion);
            Assert.Contains(nearestRegion, availableRegions);
            // us-east-1 is closer to eu-west-1 than sa-east-1
            Assert.Equal("us-east-1", nearestRegion);
        }
        
        [Fact]
        public async Task GetAllRegionHealthAsync_ReturnsHealthForAllRegions()
        {
            // Arrange
            _databaseMock.Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            
            _databaseMock.Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue("123456"));
            
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
            
            var httpClient = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
            
            // Act
            var allHealth = await _service.GetAllRegionHealthAsync();
            
            // Assert
            Assert.NotEmpty(allHealth);
            Assert.Equal(3, allHealth.Count); // 3 regions
            Assert.Contains("eu-west-1", allHealth.Keys);
            Assert.Contains("us-east-1", allHealth.Keys);
            Assert.Contains("sa-east-1", allHealth.Keys);
        }
        
        [Fact]
        public async Task CheckRegionHealthAsync_UsesCaching()
        {
            // Arrange
            var region = "eu-west-1";
            
            _databaseMock.Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            
            _databaseMock.Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue("123456"));
            
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
            
            var httpClient = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
            
            // Act
            var health1 = await _service.CheckRegionHealthAsync(region);
            var health2 = await _service.CheckRegionHealthAsync(region);
            
            // Assert
            Assert.Equal(health1.LastChecked, health2.LastChecked); // Should be cached
        }
        
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
