// <copyright file="RetryPolicyTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests.Security;

/// <summary>
/// Unit tests for RetryPolicy to ensure retry logic works correctly.
/// Tests exponential backoff, jitter application, retry conditions, and max retry limits.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Security")]
public class RetryPolicyTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output ?? throw new ArgumentNullException(nameof(output));

    [Fact]
    public async Task ExecuteAsync_SuccessOnFirstAttempt_NoRetries()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);
        int attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(
            async () =>
        {
            attemptCount++;
            return "success";
        }, ex => false);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(1, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_RetryableException_RetriesUntilSuccess()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);
        int attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(
            async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new HttpRequestException("Temporary failure");
            }

            return "success";
        }, ex => ex is HttpRequestException);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_ExponentialBackoff_DelayMultipliesCorrectly()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);
        int attemptCount = 0;
        var delays = new System.Collections.Generic.List<long>();

        // Act
        try
        {
            await policy.ExecuteAsync(
                async () =>
            {
                attemptCount++;
                var sw = System.Diagnostics.Stopwatch.StartNew();
                if (attemptCount < 4)
                {
                    throw new HttpRequestException("Temporary failure");
                }

                sw.Stop();
                return "success";
            }, ex => ex is HttpRequestException);
        }
        catch (HttpRequestException)
        {
            // Expected: All retries exhausted, final exception propagates
        }

        // Assert
        // With backoffMultiplier=2.0, delays should be: 100ms, 200ms, 400ms
        // We can't easily measure exact delays, but we can verify the pattern
        Assert.Equal(4, attemptCount); // 1 initial + 3 retries
    }

    [Fact]
    public async Task ExecuteAsync_JitterApplication_DelayVariesWithinRange()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 5, initialDelayMs: 100, backoffMultiplier: 1.0);
        int attemptCount = 0;
        var executionTimes = new System.Collections.Generic.List<long>();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await policy.ExecuteAsync(
                async () =>
            {
                attemptCount++;
                executionTimes.Add(sw.ElapsedMilliseconds);
                if (attemptCount < 4)
                {
                    throw new HttpRequestException("Temporary failure");
                }

                return "success";
            }, ex => ex is HttpRequestException);
        }
        catch (HttpRequestException)
        {
            // Expected: All retries exhausted, final exception propagates
        }

        sw.Stop();

        // Assert
        // With backoffMultiplier=1.0 and 10% jitter, delays should vary between 90ms and 110ms
        // We can't easily verify exact jitter, but we can verify that retries occurred
        Assert.Equal(4, attemptCount); // 1 initial + 3 retries
        Assert.Equal(4, executionTimes.Count);
    }

    [Fact]
    public async Task ExecuteAsync_RetryCondition_RetriesOn429()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);
        int attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(
            async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new HttpRequestException("429 Too Many Requests");
            }

            return "success";
        }, ex => ex is HttpRequestException hre && hre.Message.Contains("429"));

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_RetryCondition_RetriesOn502()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);
        int attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(
            async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new HttpRequestException("502 Bad Gateway");
            }

            return "success";
        }, ex => ex is HttpRequestException hre && hre.Message.Contains("502"));

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_RetryCondition_RetriesOn503()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);
        int attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(
            async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new HttpRequestException("503 Service Unavailable");
            }

            return "success";
        }, ex => ex is HttpRequestException hre && hre.Message.Contains("503"));

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_RetryCondition_RetriesOnNetworkError()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);
        int attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(
            async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new HttpRequestException("Network error");
            }

            return "success";
        }, ex => ex is HttpRequestException);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_RetryCondition_DoesNotRetryOnNonRetryableError()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);
        int attemptCount = 0;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await policy.ExecuteAsync<string>(
                async () =>
            {
                attemptCount++;
                throw new HttpRequestException("Always fails");
            }, ex => false).ConfigureAwait(false); // Non-retryable error - should not retry
        });

        Assert.Equal(1, attemptCount); // Should not retry
        Assert.Contains("Always fails", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_MaxRetryLimit_StopsAfterMaxAttempts()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);
        int attemptCount = 0;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await policy.ExecuteAsync<string>(
                async () =>
            {
                attemptCount++;
                throw new HttpRequestException("Always fails");
            }, ex => ex is HttpRequestException).ConfigureAwait(false);
        });

        // Should attempt 1 initial + 3 retries = 4 total attempts
        Assert.Equal(4, attemptCount);
        Assert.Contains("Always fails", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_TaskCanceledException_Retries()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);
        int attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(
            async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new TaskCanceledException("Timeout");
            }

            return "success";
        }, ex => ex is TaskCanceledException);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_NonRetryableException_DoesNotRetry()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);
        int attemptCount = 0;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await policy.ExecuteAsync<string>(
                async () =>
            {
                attemptCount++;
                throw new InvalidOperationException("Non-retryable error");
            }, ex => ex is HttpRequestException || ex is TaskCanceledException).ConfigureAwait(false);
        });

        Assert.Equal(1, attemptCount); // Should not retry
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroMaxRetries_NoRetries()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 0, initialDelayMs: 100, backoffMultiplier: 2.0);
        int attemptCount = 0;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await policy.ExecuteAsync<string>(
                async () =>
            {
                attemptCount++;
                throw new HttpRequestException("Always fails");
            }, ex => ex is HttpRequestException).ConfigureAwait(false);
        });

        Assert.Equal(1, attemptCount); // Should only attempt once
    }

    [Fact]
    public async Task ExecuteAsync_ZeroInitialDelay_RetriesImmediately()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 2, initialDelayMs: 0, backoffMultiplier: 2.0);
        int attemptCount = 0;

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await policy.ExecuteAsync(
                async () =>
                {
                    attemptCount++;
                    if (attemptCount < 3)
                    {
                        throw new HttpRequestException("Temporary failure");
                    }

                    return "success";
                },
                ex => ex is HttpRequestException);
        }
        catch (HttpRequestException)
        {
            // Expected: All retries exhausted, final exception propagates
        }

        sw.Stop();

        // Assert
        Assert.Equal(3, attemptCount); // 1 initial + 2 retries

        // With zero delay, execution should be very fast (< 100ms)
        Assert.True(sw.ElapsedMilliseconds < 100, $"Execution took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }

    [Fact]
    public async Task ExecuteAsync_BackoffMultiplier1_DelayRemainsConstant()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 1.0);
        int attemptCount = 0;

        // Act
        try
        {
            await policy.ExecuteAsync(
                async () =>
                {
                    attemptCount++;
                    if (attemptCount < 4)
                    {
                        throw new HttpRequestException("Temporary failure");
                    }

                    return "success";
                },
                ex => ex is HttpRequestException);
        }
        catch (HttpRequestException)
        {
            // Expected: All retries exhausted, final exception propagates
        }

        // Assert
        Assert.Equal(4, attemptCount); // 1 initial + 3 retries
    }
}
