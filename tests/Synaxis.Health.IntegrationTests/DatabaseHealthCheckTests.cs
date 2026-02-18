// <copyright file="DatabaseHealthCheckTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Health.IntegrationTests
{
    using FluentAssertions;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Synaxis.Health.Checks;
    using Testcontainers.PostgreSql;
    using Xunit;

    /// <summary>
    /// Integration tests for <see cref="DatabaseHealthCheck"/>.
    /// </summary>
    public class DatabaseHealthCheckTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres;
        private readonly Mock<ILogger<DatabaseHealthCheck>> _loggerMock;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseHealthCheckTests"/> class.
        /// </summary>
        public DatabaseHealthCheckTests()
        {
            this._postgres = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("test_health_db")
                .WithUsername("test")
                .WithPassword("test")
                .Build();

            this._loggerMock = new Mock<ILogger<DatabaseHealthCheck>>();
        }

        /// <summary>
        /// Initializes the test container.
        /// </summary>
        /// <returns>A task representing the initialization.</returns>
        public Task InitializeAsync()
        {
            return this._postgres.StartAsync();
        }

        /// <summary>
        /// Disposes the test container.
        /// </summary>
        /// <returns>A task representing the disposal.</returns>
        public Task DisposeAsync()
        {
            return this._postgres.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies that the health check returns healthy when database is accessible.
        /// </summary>
        /// <returns>A task representing the test.</returns>
        [Fact]
        public async Task CheckHealthAsync_WhenDatabaseIsAccessible_ReturnsHealthy()
        {
            // Arrange
            var connectionString = this._postgres.GetConnectionString();
            var healthCheck = new DatabaseHealthCheck(connectionString, this._loggerMock.Object);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("database", healthCheck, HealthStatus.Unhealthy, [], null),
            };

            // Act
            var result = await healthCheck.CheckHealthAsync(context);

            // Assert
            result.Status.Should().Be(HealthStatus.Healthy);
            result.Data.Should().ContainKey("latency_ms");
            result.Data.Should().ContainKey("connection_state");
            result.Data.Should().ContainKey("server_version");
            result.Description.Should().Be("PostgreSQL is healthy");
        }

        /// <summary>
        /// Verifies that the health check returns unhealthy when database is not accessible.
        /// </summary>
        /// <returns>A task representing the test.</returns>
        [Fact]
        public async Task CheckHealthAsync_WhenDatabaseIsNotAccessible_ReturnsUnhealthy()
        {
            // Arrange
            var invalidConnectionString = "Host=invalid_host;Port=5432;Database=test;Username=test;Password=test;Timeout=1";
            var healthCheck = new DatabaseHealthCheck(invalidConnectionString, this._loggerMock.Object);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("database", healthCheck, HealthStatus.Unhealthy, [], null),
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Act
            var result = await healthCheck.CheckHealthAsync(context, cts.Token);

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Data.Should().ContainKey("error");
            result.Exception.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies that the health check includes latency information.
        /// </summary>
        /// <returns>A task representing the test.</returns>
        [Fact]
        public async Task CheckHealthAsync_IncludesLatencyData()
        {
            // Arrange
            var connectionString = this._postgres.GetConnectionString();
            var healthCheck = new DatabaseHealthCheck(connectionString, this._loggerMock.Object);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("database", healthCheck, HealthStatus.Unhealthy, [], null),
            };

            // Act
            var result = await healthCheck.CheckHealthAsync(context);

            // Assert
            result.Status.Should().Be(HealthStatus.Healthy);
            result.Data.Should().ContainKey("latency_ms");
            var latency = (long)result.Data["latency_ms"];
            latency.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}
