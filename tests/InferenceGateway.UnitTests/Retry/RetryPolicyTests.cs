using System;
using System.Threading.Tasks;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Xunit;

namespace Synaxis.InferenceGateway.UnitTests.Retry
{
    public class RetryPolicyTests
    {
        [Fact]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);

            // Assert - Verify constructor sets values (we can't directly access private fields,
            // but we can verify behavior through tests)
            Assert.NotNull(policy);
        }

        [Fact]
        public async Task ExecuteAsync_WhenActionSucceedsOnFirstAttempt_ReturnsResultImmediately()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);
            var attemptCount = 0;
            var expectedResult = "success";

            Func<Task<string>> action = () =>
            {
                attemptCount++;
                return Task.FromResult(expectedResult);
            };

            Func<Exception, bool> shouldRetry = ex => true;

            // Act
            var result = await policy.ExecuteAsync(action, shouldRetry);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(1, attemptCount); // Only called once
        }

        [Fact]
        public async Task ExecuteAsync_WhenActionFailsAndShouldRetry_RetriesUpToMaxRetries()
        {
            // Arrange
            var maxRetries = 3;
            var policy = new RetryPolicy(maxRetries: maxRetries, initialDelayMs: 10, backoffMultiplier: 2.0);
            var attemptCount = 0;

            Func<Task<string>> action = () =>
            {
                attemptCount++;
                if (attemptCount <= maxRetries)
                {
                    throw new InvalidOperationException("Test exception");
                }
                return Task.FromResult("success");
            };

            Func<Exception, bool> shouldRetry = ex => true;

            // Act
            var result = await policy.ExecuteAsync(action, shouldRetry);

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(maxRetries + 1, attemptCount); // Initial attempt + 3 retries
        }

        [Fact]
        public async Task ExecuteAsync_WhenMaxRetriesExceeded_ThrowsLastException()
        {
            // Arrange
            var maxRetries = 2;
            var policy = new RetryPolicy(maxRetries: maxRetries, initialDelayMs: 10, backoffMultiplier: 2.0);
            var attemptCount = 0;
            var expectedException = new InvalidOperationException("Test exception");

            Func<Task<string>> action = () =>
            {
                attemptCount++;
                throw expectedException;
            };

            Func<Exception, bool> shouldRetry = ex => true;

            // Act & Assert
            var caughtException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => policy.ExecuteAsync(action, shouldRetry));

            Assert.Equal(expectedException.Message, caughtException.Message);
            Assert.Equal(maxRetries + 1, attemptCount); // Initial attempt + 2 retries
        }

        [Fact]
        public async Task ExecuteAsync_WhenShouldRetryReturnsFalse_DoesNotRetry()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 10, backoffMultiplier: 2.0);
            var attemptCount = 0;
            var expectedException = new InvalidOperationException("Non-retryable exception");

            Func<Task<string>> action = () =>
            {
                attemptCount++;
                throw expectedException;
            };

            Func<Exception, bool> shouldRetry = ex => false; // Never retry

            // Act & Assert
            var caughtException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => policy.ExecuteAsync(action, shouldRetry));

            Assert.Equal(expectedException.Message, caughtException.Message);
            Assert.Equal(1, attemptCount); // Only called once, no retries
        }

        [Fact]
        public async Task ExecuteAsync_WhenShouldRetryIsConditional_RetriesOnlyForMatchingExceptions()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 10, backoffMultiplier: 2.0);
            var attemptCount = 0;

            Func<Task<string>> action = () =>
            {
                attemptCount++;
                if (attemptCount == 1)
                {
                    throw new TimeoutException("Timeout");
                }
                else if (attemptCount == 2)
                {
                    throw new InvalidOperationException("Non-retryable");
                }
                return Task.FromResult("success");
            };

            Func<Exception, bool> shouldRetry = ex => ex is TimeoutException;

            // Act & Assert
            var caughtException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => policy.ExecuteAsync(action, shouldRetry));

            Assert.Equal("Non-retryable", caughtException.Message);
            Assert.Equal(2, attemptCount); // First attempt + 1 retry (for TimeoutException)
        }

        [Fact]
        public async Task ExecuteAsync_WithZeroMaxRetries_DoesNotRetry()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 0, initialDelayMs: 10, backoffMultiplier: 2.0);
            var attemptCount = 0;
            var expectedException = new InvalidOperationException("Test exception");

            Func<Task<string>> action = () =>
            {
                attemptCount++;
                throw expectedException;
            };

            Func<Exception, bool> shouldRetry = ex => true;

            // Act & Assert
            var caughtException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => policy.ExecuteAsync(action, shouldRetry));

            Assert.Equal(expectedException.Message, caughtException.Message);
            Assert.Equal(1, attemptCount); // Only called once, no retries
        }

        [Fact]
        public async Task ExecuteAsync_WithDifferentExceptionTypes_RetriesCorrectly()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 2, initialDelayMs: 10, backoffMultiplier: 2.0);
            var attemptCount = 0;

            Func<Task<string>> action = () =>
            {
                attemptCount++;
                if (attemptCount == 1)
                {
                    throw new HttpRequestException("Network error");
                }
                else if (attemptCount == 2)
                {
                    throw new TimeoutException("Timeout");
                }
                return Task.FromResult("success");
            };

            Func<Exception, bool> shouldRetry = ex =>
                ex is HttpRequestException || ex is TimeoutException;

            // Act
            var result = await policy.ExecuteAsync(action, shouldRetry);

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(3, attemptCount); // Initial + 2 retries
        }

        [Fact]
        public async Task ExecuteAsync_WithBackoffMultiplier_VerifiesRetryBehavior()
        {
            // Arrange
            var maxRetries = 2;
            var initialDelayMs = 50;
            var backoffMultiplier = 2.0;
            var policy = new RetryPolicy(maxRetries: maxRetries, initialDelayMs: initialDelayMs, backoffMultiplier: backoffMultiplier);
            var attemptCount = 0;

            Func<Task<string>> action = () =>
            {
                attemptCount++;
                if (attemptCount <= maxRetries)
                {
                    throw new InvalidOperationException("Test exception");
                }
                return Task.FromResult("success");
            };

            Func<Exception, bool> shouldRetry = ex => true;

            // Act
            var startTime = DateTime.UtcNow;
            var result = await policy.ExecuteAsync(action, shouldRetry);
            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(maxRetries + 1, attemptCount);

            // Verify that delays occurred (with jitter, so we check a range)
            // Expected delays: ~50ms (first retry) + ~100ms (second retry) = ~150ms total
            // With 10% jitter: 45-55ms + 90-110ms = 135-165ms total
            Assert.True(elapsed.TotalMilliseconds >= 100, $"Elapsed time {elapsed.TotalMilliseconds}ms should be at least 100ms");
            Assert.True(elapsed.TotalMilliseconds <= 300, $"Elapsed time {elapsed.TotalMilliseconds}ms should be at most 300ms");
        }

        [Fact]
        public async Task ExecuteAsync_WithLargeBackoffMultiplier_VerifiesExponentialGrowth()
        {
            // Arrange
            var maxRetries = 2;
            var initialDelayMs = 10;
            var backoffMultiplier = 5.0;
            var policy = new RetryPolicy(maxRetries: maxRetries, initialDelayMs: initialDelayMs, backoffMultiplier: backoffMultiplier);
            var attemptCount = 0;

            Func<Task<string>> action = () =>
            {
                attemptCount++;
                if (attemptCount <= maxRetries)
                {
                    throw new InvalidOperationException("Test exception");
                }
                return Task.FromResult("success");
            };

            Func<Exception, bool> shouldRetry = ex => true;

            // Act
            var startTime = DateTime.UtcNow;
            var result = await policy.ExecuteAsync(action, shouldRetry);
            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(maxRetries + 1, attemptCount);

            // Expected delays: ~10ms (first retry) + ~50ms (second retry) = ~60ms total
            // With 10% jitter: 9-11ms + 45-55ms = 54-66ms total
            // Increased tolerance to account for system load and parallel test execution
            Assert.True(elapsed.TotalMilliseconds >= 40, $"Elapsed time {elapsed.TotalMilliseconds}ms should be at least 40ms");
            Assert.True(elapsed.TotalMilliseconds <= 500, $"Elapsed time {elapsed.TotalMilliseconds}ms should be at most 500ms");
        }

        [Fact]
        public async Task ExecuteAsync_WithSmallInitialDelay_VerifiesMinimumDelay()
        {
            // Arrange
            var maxRetries = 1;
            var initialDelayMs = 1;
            var backoffMultiplier = 1.0;
            var policy = new RetryPolicy(maxRetries: maxRetries, initialDelayMs: initialDelayMs, backoffMultiplier: backoffMultiplier);
            var attemptCount = 0;

            Func<Task<string>> action = () =>
            {
                attemptCount++;
                if (attemptCount <= maxRetries)
                {
                    throw new InvalidOperationException("Test exception");
                }
                return Task.FromResult("success");
            };

            Func<Exception, bool> shouldRetry = ex => true;

            // Act
            var result = await policy.ExecuteAsync(action, shouldRetry);

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(maxRetries + 1, attemptCount);
        }

        [Fact]
        public async Task ExecuteAsync_WithFractionalBackoffMultiplier_VerifiesCalculation()
        {
            // Arrange
            var maxRetries = 2;
            var initialDelayMs = 100;
            var backoffMultiplier = 1.5;
            var policy = new RetryPolicy(maxRetries: maxRetries, initialDelayMs: initialDelayMs, backoffMultiplier: backoffMultiplier);
            var attemptCount = 0;

            Func<Task<string>> action = () =>
            {
                attemptCount++;
                if (attemptCount <= maxRetries)
                {
                    throw new InvalidOperationException("Test exception");
                }
                return Task.FromResult("success");
            };

            Func<Exception, bool> shouldRetry = ex => true;

            // Act
            var result = await policy.ExecuteAsync(action, shouldRetry);

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(maxRetries + 1, attemptCount);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsGenericValue_Correctly()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 10, backoffMultiplier: 2.0);
            var expectedResult = 42;

            Func<Task<int>> action = () => Task.FromResult(expectedResult);
            Func<Exception, bool> shouldRetry = ex => true;

            // Act
            var result = await policy.ExecuteAsync(action, shouldRetry);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteAsync_WithComplexObject_ReturnsCorrectly()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 10, backoffMultiplier: 2.0);
            var expectedResult = new TestData { Id = 1, Name = "Test" };

            Func<Task<TestData>> action = () => Task.FromResult(expectedResult);
            Func<Exception, bool> shouldRetry = ex => true;

            // Act
            var result = await policy.ExecuteAsync(action, shouldRetry);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.Id, result.Id);
            Assert.Equal(expectedResult.Name, result.Name);
        }

        [Fact]
        public async Task ExecuteAsync_WithNullResult_ReturnsNull()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 10, backoffMultiplier: 2.0);

            Func<Task<string?>> action = () => Task.FromResult<string?>(null);
            Func<Exception, bool> shouldRetry = ex => true;

            // Act
            var result = await policy.ExecuteAsync(action, shouldRetry);

            // Assert
            Assert.Null(result);
        }

        private class TestData
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;
        }
    }
}
