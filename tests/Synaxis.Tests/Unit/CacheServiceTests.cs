using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.Core.Contracts;
using Synaxis.Infrastructure.Services;
using Xunit;

namespace Synaxis.Tests.Unit
{
    public class CacheServiceTests
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly ICacheService _service;
        
        public CacheServiceTests()
        {
            _distributedCache = Substitute.For<IDistributedCache>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 });
            _service = new CacheService(_distributedCache, _memoryCache, NullLogger<CacheService>.Instance);
        }
        
        #region Constructor Tests
        
        [Fact]
        public void Constructor_NullDistributedCache_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new CacheService(null, _memoryCache, NullLogger<CacheService>.Instance));
        }
        
        [Fact]
        public void Constructor_NullMemoryCache_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new CacheService(_distributedCache, null, NullLogger<CacheService>.Instance));
        }
        
        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new CacheService(_distributedCache, _memoryCache, null));
        }
        
        #endregion
        
        #region GetAsync Tests
        
        [Fact]
        public async Task GetAsync_NullKey_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.GetAsync<string>(null));
        }
        
        [Fact]
        public async Task GetAsync_EmptyKey_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.GetAsync<string>(""));
        }
        
        [Fact]
        public async Task GetAsync_WhitespaceKey_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.GetAsync<string>("   "));
        }
        
        [Fact]
        public async Task GetAsync_L1CacheHit_ReturnsValueFromMemoryCache()
        {
            // Arrange
            var key = "test:key";
            var value = new TestDto { Id = 1, Name = "Test" };
            
            // Set in L1 cache directly
            _memoryCache.Set(key, value, new MemoryCacheEntryOptions { Size = 1 });
            
            // Act
            var result = await _service.GetAsync<TestDto>(key);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(value.Id, result.Id);
            Assert.Equal(value.Name, result.Name);
            
            // Should not call distributed cache
            await _distributedCache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
        
        [Fact]
        public async Task GetAsync_L2CacheHit_ReturnsValueFromDistributedCache()
        {
            // Arrange
            var key = "test:key";
            var value = new TestDto { Id = 2, Name = "Test2" };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
            
            _distributedCache.GetAsync(key, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(bytes));
            
            // Act
            var result = await _service.GetAsync<TestDto>(key);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(value.Id, result.Id);
            Assert.Equal(value.Name, result.Name);
            
            // Verify it was called
            await _distributedCache.Received(1).GetAsync(key, Arg.Any<CancellationToken>());
            
            // Should also populate L1 cache
            Assert.True(_memoryCache.TryGetValue(key, out TestDto cachedValue));
            Assert.Equal(value.Id, cachedValue.Id);
        }
        
        [Fact]
        public async Task GetAsync_CacheMiss_ReturnsNull()
        {
            // Arrange
            var key = "test:key:missing";
            
            _distributedCache.GetAsync(key, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<byte[]>(null));
            
            // Act
            var result = await _service.GetAsync<TestDto>(key);
            
            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public async Task GetAsync_DistributedCacheReturnsEmptyArray_ReturnsNull()
        {
            // Arrange
            var key = "test:key:empty";
            
            _distributedCache.GetAsync(key, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Array.Empty<byte>()));
            
            // Act
            var result = await _service.GetAsync<TestDto>(key);
            
            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public async Task GetAsync_DistributedCacheThrows_ReturnsNull()
        {
            // Arrange
            var key = "test:key:error";
            
            _distributedCache.GetAsync(key, Arg.Any<CancellationToken>())
                .Returns<byte[]>(x => throw new Exception("Redis connection failed"));
            
            // Act
            var result = await _service.GetAsync<TestDto>(key);
            
            // Assert - Should not throw, should return null
            Assert.Null(result);
        }
        
        [Fact]
        public async Task GetAsync_InvalidJson_ReturnsNull()
        {
            // Arrange
            var key = "test:key:invalid";
            var invalidBytes = new byte[] { 1, 2, 3, 4 }; // Not valid JSON
            
            _distributedCache.GetAsync(key, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(invalidBytes));
            
            // Act
            var result = await _service.GetAsync<TestDto>(key);
            
            // Assert - Should handle deserialization error gracefully
            Assert.Null(result);
        }
        
        #endregion
        
        #region SetAsync Tests
        
        [Fact]
        public async Task SetAsync_NullKey_ThrowsArgumentException()
        {
            // Arrange
            var value = new TestDto { Id = 1, Name = "Test" };
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.SetAsync(null, value));
        }
        
        [Fact]
        public async Task SetAsync_EmptyKey_ThrowsArgumentException()
        {
            // Arrange
            var value = new TestDto { Id = 1, Name = "Test" };
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.SetAsync("", value));
        }
        
        [Fact]
        public async Task SetAsync_NullValue_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.SetAsync<TestDto>("test:key", null));
        }
        
        [Fact]
        public async Task SetAsync_ValidKeyValue_SetsBothCaches()
        {
            // Arrange
            var key = "test:key:set";
            var value = new TestDto { Id = 3, Name = "Test3" };
            
            // Act
            await _service.SetAsync(key, value);
            
            // Assert - Check L1 cache
            Assert.True(_memoryCache.TryGetValue(key, out TestDto cachedValue));
            Assert.Equal(value.Id, cachedValue.Id);
            
            // Assert - Check L2 cache was called
            await _distributedCache.Received(1).SetAsync(
                key, 
                Arg.Any<byte[]>(), 
                Arg.Any<DistributedCacheEntryOptions>(), 
                Arg.Any<CancellationToken>());
        }
        
        [Fact]
        public async Task SetAsync_CustomExpiration_UsesProvidedExpiration()
        {
            // Arrange
            var key = "test:key:expiry";
            var value = new TestDto { Id = 4, Name = "Test4" };
            var expiration = TimeSpan.FromMinutes(30);
            
            // Act
            await _service.SetAsync(key, value, expiration);
            
            // Assert
            await _distributedCache.Received(1).SetAsync(
                key,
                Arg.Any<byte[]>(),
                Arg.Is<DistributedCacheEntryOptions>(opts => 
                    opts.AbsoluteExpirationRelativeToNow == expiration),
                Arg.Any<CancellationToken>());
        }
        
        [Fact]
        public async Task SetAsync_NoExpiration_UsesDefaultExpiration()
        {
            // Arrange
            var key = "test:key:default";
            var value = new TestDto { Id = 5, Name = "Test5" };
            
            // Act
            await _service.SetAsync(key, value);
            
            // Assert - Should use default 15 minutes
            await _distributedCache.Received(1).SetAsync(
                key,
                Arg.Any<byte[]>(),
                Arg.Is<DistributedCacheEntryOptions>(opts => 
                    opts.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(15)),
                Arg.Any<CancellationToken>());
        }
        
        [Fact]
        public async Task SetAsync_DistributedCacheThrows_DoesNotThrow()
        {
            // Arrange
            var key = "test:key:error";
            var value = new TestDto { Id = 6, Name = "Test6" };
            
            _distributedCache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
                .Returns<Task>(x => throw new Exception("Redis connection failed"));
            
            // Act - Should not throw
            await _service.SetAsync(key, value);
            
            // Assert - Should still set in L1 cache
            Assert.True(_memoryCache.TryGetValue(key, out TestDto cachedValue));
            Assert.Equal(value.Id, cachedValue.Id);
        }
        
        #endregion
        
        #region RemoveAsync Tests
        
        [Fact]
        public async Task RemoveAsync_NullKey_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.RemoveAsync(null));
        }
        
        [Fact]
        public async Task RemoveAsync_EmptyKey_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.RemoveAsync(""));
        }
        
        [Fact]
        public async Task RemoveAsync_ValidKey_RemovesFromBothCaches()
        {
            // Arrange
            var key = "test:key:remove";
            var value = new TestDto { Id = 7, Name = "Test7" };
            
            // Set in L1 cache
            _memoryCache.Set(key, value, new MemoryCacheEntryOptions { Size = 1 });
            
            // Act
            await _service.RemoveAsync(key);
            
            // Assert - Should be removed from L1
            Assert.False(_memoryCache.TryGetValue(key, out TestDto _));
            
            // Assert - Should call L2 remove
            await _distributedCache.Received(1).RemoveAsync(key, Arg.Any<CancellationToken>());
        }
        
        [Fact]
        public async Task RemoveAsync_DistributedCacheThrows_DoesNotThrow()
        {
            // Arrange
            var key = "test:key:remove:error";
            
            _distributedCache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns<Task>(x => throw new Exception("Redis connection failed"));
            
            // Act - Should not throw
            await _service.RemoveAsync(key);
            
            // Assert - Method completes without exception
            Assert.True(true);
        }
        
        #endregion
        
        #region RemoveByPatternAsync Tests
        
        [Fact]
        public async Task RemoveByPatternAsync_NullPattern_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.RemoveByPatternAsync(null));
        }
        
        [Fact]
        public async Task RemoveByPatternAsync_EmptyPattern_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.RemoveByPatternAsync(""));
        }
        
        [Fact]
        public async Task RemoveByPatternAsync_ValidPattern_Completes()
        {
            // Arrange
            var pattern = "test:*";
            
            // Act - Should complete without throwing (even though not fully implemented)
            await _service.RemoveByPatternAsync(pattern);
            
            // Assert
            Assert.True(true);
        }
        
        #endregion
        
        #region ExistsAsync Tests
        
        [Fact]
        public async Task ExistsAsync_NullKey_ReturnsFalse()
        {
            // Act
            var result = await _service.ExistsAsync(null);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public async Task ExistsAsync_EmptyKey_ReturnsFalse()
        {
            // Act
            var result = await _service.ExistsAsync("");
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public async Task ExistsAsync_ExistsInL1Cache_ReturnsTrue()
        {
            // Arrange
            var key = "test:key:exists:l1";
            var value = new TestDto { Id = 8, Name = "Test8" };
            
            _memoryCache.Set(key, value, new MemoryCacheEntryOptions { Size = 1 });
            
            // Act
            var result = await _service.ExistsAsync(key);
            
            // Assert
            Assert.True(result);
            
            // Should not call distributed cache
            await _distributedCache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
        
        [Fact]
        public async Task ExistsAsync_ExistsInL2Cache_ReturnsTrue()
        {
            // Arrange
            var key = "test:key:exists:l2";
            var bytes = new byte[] { 1, 2, 3 };
            
            _distributedCache.GetAsync(key, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(bytes));
            
            // Act
            var result = await _service.ExistsAsync(key);
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public async Task ExistsAsync_DoesNotExist_ReturnsFalse()
        {
            // Arrange
            var key = "test:key:notexists";
            
            _distributedCache.GetAsync(key, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<byte[]>(null));
            
            // Act
            var result = await _service.ExistsAsync(key);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public async Task ExistsAsync_DistributedCacheThrows_ReturnsFalse()
        {
            // Arrange
            var key = "test:key:exists:error";
            
            _distributedCache.GetAsync(key, Arg.Any<CancellationToken>())
                .Returns<byte[]>(x => throw new Exception("Redis connection failed"));
            
            // Act
            var result = await _service.ExistsAsync(key);
            
            // Assert
            Assert.False(result);
        }
        
        #endregion
        
        #region GetManyAsync Tests
        
        [Fact]
        public async Task GetManyAsync_NullKeys_ReturnsEmptyDictionary()
        {
            // Act
            var result = await _service.GetManyAsync<TestDto>(null);
            
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        
        [Fact]
        public async Task GetManyAsync_EmptyKeys_ReturnsEmptyDictionary()
        {
            // Act
            var result = await _service.GetManyAsync<TestDto>(Array.Empty<string>());
            
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        
        [Fact]
        public async Task GetManyAsync_ValidKeys_ReturnsMatchingValues()
        {
            // Arrange
            var key1 = "test:key:many:1";
            var key2 = "test:key:many:2";
            var key3 = "test:key:many:3";
            
            var value1 = new TestDto { Id = 9, Name = "Test9" };
            var value2 = new TestDto { Id = 10, Name = "Test10" };
            
            // Set key1 in L1
            _memoryCache.Set(key1, value1, new MemoryCacheEntryOptions { Size = 1 });
            
            // Set key2 in L2
            var bytes2 = JsonSerializer.SerializeToUtf8Bytes(value2);
            _distributedCache.GetAsync(key2, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(bytes2));
            
            // key3 doesn't exist
            _distributedCache.GetAsync(key3, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<byte[]>(null));
            
            // Act
            var result = await _service.GetManyAsync<TestDto>(new[] { key1, key2, key3 });
            
            // Assert
            Assert.Equal(2, result.Count);
            Assert.True(result.ContainsKey(key1));
            Assert.True(result.ContainsKey(key2));
            Assert.False(result.ContainsKey(key3));
            Assert.Equal(value1.Id, result[key1].Id);
            Assert.Equal(value2.Id, result[key2].Id);
        }
        
        #endregion
        
        #region SetManyAsync Tests
        
        [Fact]
        public async Task SetManyAsync_NullItems_DoesNotThrow()
        {
            // Act - Should complete without throwing
            await _service.SetManyAsync<TestDto>(null);
            
            // Assert
            Assert.True(true);
        }
        
        [Fact]
        public async Task SetManyAsync_EmptyItems_DoesNotThrow()
        {
            // Act - Should complete without throwing
            await _service.SetManyAsync(new Dictionary<string, TestDto>());
            
            // Assert
            Assert.True(true);
        }
        
        [Fact]
        public async Task SetManyAsync_ValidItems_SetsAllValues()
        {
            // Arrange
            var items = new Dictionary<string, TestDto>
            {
                { "test:key:setmany:1", new TestDto { Id = 11, Name = "Test11" } },
                { "test:key:setmany:2", new TestDto { Id = 12, Name = "Test12" } },
                { "test:key:setmany:3", new TestDto { Id = 13, Name = "Test13" } }
            };
            
            // Act
            await _service.SetManyAsync(items);
            
            // Assert - Check all are in L1 cache
            foreach (var kvp in items)
            {
                Assert.True(_memoryCache.TryGetValue(kvp.Key, out TestDto cachedValue));
                Assert.Equal(kvp.Value.Id, cachedValue.Id);
            }
            
            // Assert - Check L2 cache was called for each
            await _distributedCache.Received(items.Count).SetAsync(
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<DistributedCacheEntryOptions>(),
                Arg.Any<CancellationToken>());
        }
        
        [Fact]
        public async Task SetManyAsync_WithExpiration_UsesProvidedExpiration()
        {
            // Arrange
            var items = new Dictionary<string, TestDto>
            {
                { "test:key:setmany:exp:1", new TestDto { Id = 14, Name = "Test14" } }
            };
            var expiration = TimeSpan.FromMinutes(45);
            
            // Act
            await _service.SetManyAsync(items, expiration);
            
            // Assert
            await _distributedCache.Received(1).SetAsync(
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Is<DistributedCacheEntryOptions>(opts => 
                    opts.AbsoluteExpirationRelativeToNow == expiration),
                Arg.Any<CancellationToken>());
        }
        
        #endregion
        
        #region GetStatisticsAsync Tests
        
        [Fact]
        public async Task GetStatisticsAsync_InitialState_ReturnsZeroStats()
        {
            // Act
            var result = await _service.GetStatisticsAsync();
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Hits);
            Assert.Equal(0, result.Misses);
            Assert.Equal(0, result.HitRatio);
            Assert.Equal(0, result.TotalKeys);
            Assert.Equal(0, result.MemoryUsageBytes);
        }
        
        [Fact]
        public async Task GetStatisticsAsync_AfterCacheHit_IncrementsHits()
        {
            // Arrange
            var key = "test:key:stats:hit";
            var value = new TestDto { Id = 15, Name = "Test15" };
            
            _memoryCache.Set(key, value, new MemoryCacheEntryOptions { Size = 1 });
            
            // Act
            await _service.GetAsync<TestDto>(key);
            var stats = await _service.GetStatisticsAsync();
            
            // Assert
            Assert.Equal(1, stats.Hits);
            Assert.Equal(0, stats.Misses);
            Assert.Equal(1.0, stats.HitRatio);
        }
        
        [Fact]
        public async Task GetStatisticsAsync_AfterCacheMiss_IncrementsMisses()
        {
            // Arrange
            var key = "test:key:stats:miss";
            
            _distributedCache.GetAsync(key, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<byte[]>(null));
            
            // Act
            await _service.GetAsync<TestDto>(key);
            var stats = await _service.GetStatisticsAsync();
            
            // Assert
            Assert.Equal(0, stats.Hits);
            Assert.Equal(1, stats.Misses);
            Assert.Equal(0, stats.HitRatio);
        }
        
        [Fact]
        public async Task GetStatisticsAsync_MixedHitsAndMisses_CalculatesCorrectRatio()
        {
            // Arrange
            var hitKey = "test:key:stats:hit2";
            var missKey1 = "test:key:stats:miss1";
            var missKey2 = "test:key:stats:miss2";
            
            var value = new TestDto { Id = 16, Name = "Test16" };
            _memoryCache.Set(hitKey, value, new MemoryCacheEntryOptions { Size = 1 });
            
            _distributedCache.GetAsync(missKey1, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<byte[]>(null));
            _distributedCache.GetAsync(missKey2, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<byte[]>(null));
            
            // Act
            await _service.GetAsync<TestDto>(hitKey);
            await _service.GetAsync<TestDto>(missKey1);
            await _service.GetAsync<TestDto>(missKey2);
            var stats = await _service.GetStatisticsAsync();
            
            // Assert
            Assert.Equal(1, stats.Hits);
            Assert.Equal(2, stats.Misses);
            Assert.Equal(1.0 / 3.0, stats.HitRatio, 2); // 33.33%
        }
        
        #endregion
        
        #region InvalidateGloballyAsync Tests
        
        [Fact]
        public async Task InvalidateGloballyAsync_NullKey_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.InvalidateGloballyAsync(null));
        }
        
        [Fact]
        public async Task InvalidateGloballyAsync_EmptyKey_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.InvalidateGloballyAsync(""));
        }
        
        [Fact]
        public async Task InvalidateGloballyAsync_ValidKey_RemovesFromBothCaches()
        {
            // Arrange
            var key = "test:key:invalidate";
            var value = new TestDto { Id = 17, Name = "Test17" };
            
            // Set in L1 cache
            _memoryCache.Set(key, value, new MemoryCacheEntryOptions { Size = 1 });
            
            // Act
            await _service.InvalidateGloballyAsync(key);
            
            // Assert - Should be removed from L1
            Assert.False(_memoryCache.TryGetValue(key, out TestDto _));
            
            // Assert - Should call L2 remove
            await _distributedCache.Received(1).RemoveAsync(key, Arg.Any<CancellationToken>());
        }
        
        [Fact]
        public async Task InvalidateGloballyAsync_RemoveThrows_DoesNotThrow()
        {
            // Arrange
            var key = "test:key:invalidate:error";
            
            _distributedCache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns<Task>(x => throw new Exception("Redis connection failed"));
            
            // Act - Should not throw
            await _service.InvalidateGloballyAsync(key);
            
            // Assert
            Assert.True(true);
        }
        
        #endregion
        
        #region GetTenantKey Tests
        
        [Fact]
        public void GetTenantKey_EmptyTenantId_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _service.GetTenantKey(Guid.Empty, "test:key"));
        }
        
        [Fact]
        public void GetTenantKey_NullKey_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _service.GetTenantKey(Guid.NewGuid(), null));
        }
        
        [Fact]
        public void GetTenantKey_EmptyKey_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _service.GetTenantKey(Guid.NewGuid(), ""));
        }
        
        [Fact]
        public void GetTenantKey_ValidInputs_ReturnsFormattedKey()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var key = "user:profile";
            
            // Act
            var result = _service.GetTenantKey(tenantId, key);
            
            // Assert
            Assert.Equal($"tenant:{tenantId}:{key}", result);
        }
        
        [Fact]
        public void GetTenantKey_DifferentTenants_ProducesDifferentKeys()
        {
            // Arrange
            var tenantId1 = Guid.NewGuid();
            var tenantId2 = Guid.NewGuid();
            var key = "user:profile";
            
            // Act
            var result1 = _service.GetTenantKey(tenantId1, key);
            var result2 = _service.GetTenantKey(tenantId2, key);
            
            // Assert
            Assert.NotEqual(result1, result2);
        }
        
        #endregion
        
        #region Helper Classes
        
        private class TestDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        
        #endregion
    }
}
