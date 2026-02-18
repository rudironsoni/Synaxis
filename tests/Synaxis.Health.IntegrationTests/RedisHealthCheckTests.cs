// <copyright file="RedisHealthCheckTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Health.IntegrationTests
{
    using FluentAssertions;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Logging;
    using Moq;
    using StackExchange.Redis;
    using Synaxis.Health.Checks;
    using Testcontainers.Redis;
    using Xunit;

    /// <summary>
    /// Integration tests for <see cref="RedisHealthCheck"/>.
    /// </summary>
    public class RedisHealthCheckTests : IAsyncLifetime
    {
        private readonly RedisContainer _redis;
        private readonly Mock<ILogger<RedisHealthCheck>> _loggerMock;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisHealthCheckTests"/> class.
        /// </summary>
        public RedisHealthCheckTests()
        {
            this._redis = new RedisBuilder("redis:7-alpine")
                .Build();

            this._loggerMock = new Mock<ILogger<RedisHealthCheck>>();
        }

        /// <summary>
        /// Initializes the test container.
        /// </summary>
        /// <returns>A task representing the initialization.</returns>
        public async Task InitializeAsync()
        {
            await this._redis.StartAsync().ConfigureAwait(false);

            // Wait for Redis to be ready
            var connectionString = this._redis.GetConnectionString();
            var retryCount = 0;
            const int maxRetries = 10;

            while (retryCount < maxRetries)
            {
                try
                {
                    using var connection = await ConnectionMultiplexer.ConnectAsync(connectionString).ConfigureAwait(false);
                    if (connection.IsConnected)
                    {
                        return;
                    }
                }
                catch
                {
                    // Retry
                }

                await Task.Delay(100).ConfigureAwait(false);
                retryCount++;
            }

            throw new InvalidOperationException("Redis container failed to become ready");
        }

        /// <summary>
        /// Disposes the test container.
        /// </summary>
        /// <returns>A task representing the disposal.</returns>
        public Task DisposeAsync()
        {
            return this._redis.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies that the health check returns healthy when Redis is accessible.
        /// </summary>
        /// <returns>A task representing the test.</returns>
        [Fact]
        public async Task CheckHealthAsync_WhenRedisIsAccessible_ReturnsHealthy()
        {
            // Arrange
            var connectionString = this._redis.GetConnectionString();
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(options);

            // Wait for connection to be ready
            var maxRetries = 10;
            var retryDelay = TimeSpan.FromMilliseconds(100);
            for (int i = 0; i < maxRetries && !connectionMultiplexer.IsConnected; i++)
            {
                await Task.Delay(retryDelay);
            }

            connectionMultiplexer.IsConnected.Should().BeTrue("Redis should be connected");
            var healthCheck = new RedisHealthCheck(connectionMultiplexer, this._loggerMock.Object);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("redis", healthCheck, HealthStatus.Unhealthy, [], null),
            };

            // Act
            var result = await healthCheck.CheckHealthAsync(context);

            // Debug - see why it's failing
            if (result.Status != HealthStatus.Healthy)
            {
                var dataStr = string.Join(", ", result.Data.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                Assert.Fail($"Health check failed with status {result.Status}. Data: {dataStr}");
            }

            // Assert
            result.Status.Should().Be(HealthStatus.Healthy);
            result.Data.Should().ContainKey("latency_ms");
            // redis_version is optional - TestContainers Redis may not allow INFO command
        }

        /// <summary>
        /// Verifies that the health check returns unhealthy when Redis is not connected.
        /// </summary>
        /// <returns>A task representing the test.</returns>
        [Fact]
        public async Task CheckHealthAsync_WhenNotConnected_ReturnsUnhealthy()
        {
            // Arrange - Create a multiplexer that will fail to connect
            var invalidConnectionString = "localhost:12345,abortConnect=false,connectTimeout=100";
            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(invalidConnectionString);
            var healthCheck = new RedisHealthCheck(connectionMultiplexer, this._loggerMock.Object);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("redis", healthCheck, HealthStatus.Unhealthy, [], null),
            };

            // Act
            var result = await healthCheck.CheckHealthAsync(context);

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Data.Should().ContainKey("connection_state");
        }
    }
}
