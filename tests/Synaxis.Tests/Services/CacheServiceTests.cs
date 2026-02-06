using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.Infrastructure.Services;
using Xunit;

namespace Synaxis.Tests.Services
{
    public class CacheServiceTests
    {
        private readonly Mock<IDistributedCache> _mockDistributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<ILogger<CacheService>> _mockLogger;
        private readonly CacheService _cacheService;
        
        public CacheServiceTests()
        {
            _mockDistributedCache = new Mock<IDistributedCache>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _mockLogger = new Mock<ILogger<CacheService>>();
            _cacheService = new CacheService(_mockDistributedCache.Object, _memoryCache, _mockLogger.Object);
        }
        
        [Fact]
        public async Task GetAsync_KeyNotInCache_ReturnsNull()
        {
            // Arrange
            _mockDistributedCache.Setup(x => x.GetAsync("test-key", default))
                .ReturnsAsync((byte[])null);
            
            // Act
            var result = await _cacheService.GetAsync<TestData>("test-key");
            
            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public async Task SetAsync_ValidData_StoresInBothCaches()
        {
            // Arrange
            var testData = new TestData { Id = 1, Name = "Test" };
            var key = "test-key";
            
            _mockDistributedCache.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default
            )).Returns(Task.CompletedTask);
            
            // Act
            await _cacheService.SetAsync(key, testData);
            
            // Assert
            _mockDistributedCache.Verify(x => x.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default
            ), Times.Once);
        }
        
        [Fact]
        public async Task GetAsync_L1CacheHit_ReturnsFromMemory()
        {
            // Arrange
            var testData = new TestData { Id = 1, Name = "Test" };
            var key = "test-key";
            
            // Pre-populate L1 cache
            _memoryCache.Set(key, testData);
            
            // Act
            var result = await _cacheService.GetAsync<TestData>(key);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(testData.Id, result.Id);
            Assert.Equal(testData.Name, result.Name);
            
            // L2 cache should not be called
            _mockDistributedCache.Verify(x => x.GetAsync(It.IsAny<string>(), default), Times.Never);
        }
        
        [Fact]
        public async Task GetAsync_L2CacheHit_PopulatesL1Cache()
        {
            // Arrange
            var testData = new TestData { Id = 1, Name = "Test" };
            var key = "test-key";
            var serialized = JsonSerializer.SerializeToUtf8Bytes(testData);
            
            _mockDistributedCache.Setup(x => x.GetAsync(key, default))
                .ReturnsAsync(serialized);
            
            // Act
            var result = await _cacheService.GetAsync<TestData>(key);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(testData.Id, result.Id);
            Assert.Equal(testData.Name, result.Name);
            
            // L2 should be called once
            _mockDistributedCache.Verify(x => x.GetAsync(key, default), Times.Once);
            
            // L1 should now contain the value
            Assert.True(_memoryCache.TryGetValue(key, out TestData _));
        }
        
        [Fact]
        public async Task RemoveAsync_RemovesFromBothCaches()
        {
            // Arrange
            var key = "test-key";
            var testData = new TestData { Id = 1, Name = "Test" };
            
            // Pre-populate L1 cache
            _memoryCache.Set(key, testData);
            
            _mockDistributedCache.Setup(x => x.RemoveAsync(key, default))
                .Returns(Task.CompletedTask);
            
            // Act
            await _cacheService.RemoveAsync(key);
            
            // Assert
            Assert.False(_memoryCache.TryGetValue(key, out TestData _));
            _mockDistributedCache.Verify(x => x.RemoveAsync(key, default), Times.Once);
        }
        
        [Fact]
        public async Task ExistsAsync_KeyInL1Cache_ReturnsTrue()
        {
            // Arrange
            var key = "test-key";
            _memoryCache.Set(key, new TestData { Id = 1, Name = "Test" });
            
            // Act
            var exists = await _cacheService.ExistsAsync(key);
            
            // Assert
            Assert.True(exists);
            _mockDistributedCache.Verify(x => x.GetAsync(It.IsAny<string>(), default), Times.Never);
        }
        
        [Fact]
        public async Task ExistsAsync_KeyInL2Cache_ReturnsTrue()
        {
            // Arrange
            var key = "test-key";
            var testData = new TestData { Id = 1, Name = "Test" };
            var serialized = JsonSerializer.SerializeToUtf8Bytes(testData);
            
            _mockDistributedCache.Setup(x => x.GetAsync(key, default))
                .ReturnsAsync(serialized);
            
            // Act
            var exists = await _cacheService.ExistsAsync(key);
            
            // Assert
            Assert.True(exists);
        }
        
        [Fact]
        public async Task ExistsAsync_KeyNotInCache_ReturnsFalse()
        {
            // Arrange
            _mockDistributedCache.Setup(x => x.GetAsync("nonexistent", default))
                .ReturnsAsync((byte[])null);
            
            // Act
            var exists = await _cacheService.ExistsAsync("nonexistent");
            
            // Assert
            Assert.False(exists);
        }
        
        [Fact]
        public async Task GetManyAsync_MultipleKeys_ReturnsMatchingValues()
        {
            // Arrange
            var keys = new[] { "key1", "key2", "key3" };
            var data1 = new TestData { Id = 1, Name = "Test1" };
            var data2 = new TestData { Id = 2, Name = "Test2" };
            
            _mockDistributedCache.Setup(x => x.GetAsync("key1", default))
                .ReturnsAsync(JsonSerializer.SerializeToUtf8Bytes(data1));
            _mockDistributedCache.Setup(x => x.GetAsync("key2", default))
                .ReturnsAsync(JsonSerializer.SerializeToUtf8Bytes(data2));
            _mockDistributedCache.Setup(x => x.GetAsync("key3", default))
                .ReturnsAsync((byte[])null);
            
            // Act
            var results = await _cacheService.GetManyAsync<TestData>(keys);
            
            // Assert
            Assert.Equal(2, results.Count);
            Assert.Contains("key1", results.Keys);
            Assert.Contains("key2", results.Keys);
            Assert.DoesNotContain("key3", results.Keys);
        }
        
        [Fact]
        public async Task SetManyAsync_MultipleItems_StoresAll()
        {
            // Arrange
            var items = new Dictionary<string, TestData>
            {
                { "key1", new TestData { Id = 1, Name = "Test1" } },
                { "key2", new TestData { Id = 2, Name = "Test2" } }
            };
            
            _mockDistributedCache.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default
            )).Returns(Task.CompletedTask);
            
            // Act
            await _cacheService.SetManyAsync(items);
            
            // Assert
            _mockDistributedCache.Verify(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default
            ), Times.Exactly(2));
        }
        
        [Fact]
        public async Task GetStatisticsAsync_AfterOperations_ReturnsStats()
        {
            // Arrange
            var testData = new TestData { Id = 1, Name = "Test" };
            var serialized = JsonSerializer.SerializeToUtf8Bytes(testData);
            
            // Set up cache hits
            _mockDistributedCache.Setup(x => x.GetAsync("existing", default))
                .ReturnsAsync(serialized);
            _mockDistributedCache.Setup(x => x.GetAsync("nonexistent", default))
                .ReturnsAsync((byte[])null);
            
            // Perform operations
            await _cacheService.GetAsync<TestData>("existing"); // hit
            await _cacheService.GetAsync<TestData>("nonexistent"); // miss
            await _cacheService.GetAsync<TestData>("nonexistent"); // miss
            
            // Act
            var stats = await _cacheService.GetStatisticsAsync();
            
            // Assert
            Assert.Equal(1, stats.Hits);
            Assert.Equal(2, stats.Misses);
            Assert.Equal(1.0 / 3.0, stats.HitRatio, 2);
        }
        
        [Fact]
        public async Task InvalidateGloballyAsync_RemovesKeyAndLogs()
        {
            // Arrange
            var key = "test-key";
            var testData = new TestData { Id = 1, Name = "Test" };
            _memoryCache.Set(key, testData);
            
            _mockDistributedCache.Setup(x => x.RemoveAsync(key, default))
                .Returns(Task.CompletedTask);
            
            // Act
            await _cacheService.InvalidateGloballyAsync(key);
            
            // Assert
            Assert.False(_memoryCache.TryGetValue(key, out TestData _));
            _mockDistributedCache.Verify(x => x.RemoveAsync(key, default), Times.Once);
        }
        
        [Fact]
        public void GetTenantKey_ValidInputs_ReturnsFormattedKey()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var key = "user:settings";
            
            // Act
            var tenantKey = _cacheService.GetTenantKey(tenantId, key);
            
            // Assert
            Assert.Equal($"tenant:{tenantId}:{key}", tenantKey);
        }
        
        [Fact]
        public void GetTenantKey_EmptyTenantId_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _cacheService.GetTenantKey(Guid.Empty, "test-key")
            );
        }
        
        [Fact]
        public void GetTenantKey_NullKey_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _cacheService.GetTenantKey(Guid.NewGuid(), null)
            );
        }
        
        [Fact]
        public async Task GetAsync_HitRatio_CalculatesCorrectly()
        {
            // Arrange
            var testData = new TestData { Id = 1, Name = "Test" };
            var serialized = JsonSerializer.SerializeToUtf8Bytes(testData);
            
            _mockDistributedCache.Setup(x => x.GetAsync("hit1", default))
                .ReturnsAsync(serialized);
            _mockDistributedCache.Setup(x => x.GetAsync("hit2", default))
                .ReturnsAsync(serialized);
            _mockDistributedCache.Setup(x => x.GetAsync("miss1", default))
                .ReturnsAsync((byte[])null);
            
            // Act - 2 hits, 1 miss = 66.67% hit ratio
            await _cacheService.GetAsync<TestData>("hit1");
            await _cacheService.GetAsync<TestData>("hit2");
            await _cacheService.GetAsync<TestData>("miss1");
            
            var stats = await _cacheService.GetStatisticsAsync();
            
            // Assert
            Assert.Equal(2, stats.Hits);
            Assert.Equal(1, stats.Misses);
            Assert.Equal(0.6666666666666666, stats.HitRatio, 10);
        }
        
        private class TestData
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
