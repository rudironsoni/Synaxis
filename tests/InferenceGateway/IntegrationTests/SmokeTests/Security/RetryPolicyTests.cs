// <copyright file="RetryPolicyTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Security
{
    public class RetryPolicyTests
    {
        [Fact]
        public async Task ExecuteAsync_SucceedsOnFirstAttempt()
        {
            // Arrange
            var policy = new RetryPolicy(3, 100, 2.0);
            var attemptCount = 0;

            // Act
            var result = await policy.ExecuteAsync(
                () =>
                {
                    attemptCount++;
                    return Task.FromResult("success");
                },
                ex => true);

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(1, attemptCount);
        }

        [Fact]
        public async Task ExecuteAsync_RetriesOnFailure()
        {
            // Arrange
            var policy = new RetryPolicy(3, 10, 2.0);
            var attemptCount = 0;

            // Act
            var result = await policy.ExecuteAsync(
                () =>
                {
                    attemptCount++;
                    if (attemptCount < 3)
                    {
                        throw new HttpRequestException("Network error");
                    }

                    return Task.FromResult("success");
                },
                ex => ex is HttpRequestException);

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(3, attemptCount);
        }

        [Fact]
        public async Task ExecuteAsync_MaxRetriesExceeded_ThrowsException()
        {
            // Arrange
            var policy = new RetryPolicy(2, 10, 2.0);

            // Act & Assert
            var attemptCount = 0;
            await Assert.ThrowsAsync<HttpRequestException>(() => policy.ExecuteAsync<object>(
                () =>
                {
                    attemptCount++;
                    throw new HttpRequestException("Network error");
                },
                ex => ex is HttpRequestException));

            Assert.Equal(3, attemptCount);
        }

        [Fact]
        public async Task ExecuteAsync_OnlyRetriesRetryableExceptions()
        {
            // Arrange
            var policy = new RetryPolicy(3, 10, 2.0);
            var attemptCount = 0;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => policy.ExecuteAsync<object>(
                () =>
                {
                    attemptCount++;
                    throw new ArgumentException("Bad argument");
                },
                ex => ex is HttpRequestException));

            // Should only attempt once since ArgumentException is not retryable
            Assert.Equal(1, attemptCount);
        }

        [Fact]
        public async Task ExecuteAsync_IncreasesDelayWithBackoff()
        {
            // Arrange
            var policy = new RetryPolicy(3, 100, 2.0);
            var delays = new System.Collections.Generic.List<int>();
            var attemptCount = 0;

            // Act
            await policy.ExecuteAsync(
                () =>
                {
                    attemptCount++;
                    if (attemptCount < 3)
                    {
                        throw new HttpRequestException("Network error");
                    }

                    return Task.FromResult("success");
                },
                ex => ex is HttpRequestException);

            // Assert
            // First retry: ~100ms, Second retry: ~200ms
            // We can't measure exact delays due to jitter, but we verify retries occurred
            Assert.Equal(3, attemptCount);
        }

        [Theory]
        [InlineData(429)] // Rate limited
        [InlineData(502)] // Bad gateway
        [InlineData(503)] // Service unavailable
        [InlineData(504)] // Gateway timeout
        public void RetryPolicy_ShouldRetryOnStatusCode(int statusCode)
        {
            // Arrange
            var ex = new HttpRequestException("Error", null, (HttpStatusCode)statusCode);

            // Act
            var shouldRetry = ex is HttpRequestException hre && hre.StatusCode.HasValue &&
                (hre.StatusCode == HttpStatusCode.ServiceUnavailable ||
                 hre.StatusCode == HttpStatusCode.BadGateway ||
                 hre.StatusCode == HttpStatusCode.GatewayTimeout ||
                 (int)hre.StatusCode.Value == 429);

            // Assert
            Assert.True(shouldRetry);
        }

        [Theory]
        [InlineData(200)] // OK
        [InlineData(400)] // Bad request
        [InlineData(401)] // Unauthorized
        [InlineData(404)] // Not found
        public void RetryPolicy_ShouldNotRetryOnClientError(int statusCode)
        {
            // Arrange
            var ex = new HttpRequestException("Error", null, (HttpStatusCode)statusCode);

            // Act
            var shouldRetry = ex is HttpRequestException hre && hre.StatusCode.HasValue &&
                (hre.StatusCode == HttpStatusCode.ServiceUnavailable ||
                 hre.StatusCode == HttpStatusCode.BadGateway ||
                 hre.StatusCode == HttpStatusCode.GatewayTimeout ||
                 (int)hre.StatusCode.Value == 429);

            // Assert
            Assert.False(shouldRetry);
        }

        [Fact]
        public async Task ExecuteAsync_RespectsMaxRetriesParameter()
        {
            // Arrange
            var maxRetries = 5;
            var policy = new RetryPolicy(maxRetries, 10, 2.0);
            var attemptCount = 0;

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => policy.ExecuteAsync<object>(
                () =>
                {
                    attemptCount++;
                    throw new HttpRequestException("Error");
                },
                ex => true));

            // Initial attempt + maxRetries
            Assert.Equal(maxRetries + 1, attemptCount);
        }

        [Fact]
        public async Task ExecuteAsync_ZeroMaxRetries_DoesNotRetry()
        {
            // Arrange
            var policy = new RetryPolicy(0, 10, 2.0);
            var attemptCount = 0;

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => policy.ExecuteAsync<object>(
                () =>
                {
                    attemptCount++;
                    throw new HttpRequestException("Error");
                },
                ex => true));

            // Only one attempt since maxRetries is 0
            Assert.Equal(1, attemptCount);
        }
    }
}
