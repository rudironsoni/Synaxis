// <copyright file="ServiceDiscoveryHealthCheckTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Health.IntegrationTests
{
    using FluentAssertions;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Synaxis.Health.Checks;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="ServiceDiscoveryHealthCheck"/>.
    /// </summary>
    public class ServiceDiscoveryHealthCheckTests
    {
        private readonly Mock<IServiceDiscoveryClient> _discoveryClientMock;
        private readonly Mock<ILogger<ServiceDiscoveryHealthCheck>> _loggerMock;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceDiscoveryHealthCheckTests"/> class.
        /// </summary>
        public ServiceDiscoveryHealthCheckTests()
        {
            this._discoveryClientMock = new Mock<IServiceDiscoveryClient>();
            this._loggerMock = new Mock<ILogger<ServiceDiscoveryHealthCheck>>();
        }

        /// <summary>
        /// Verifies that the health check returns healthy when registry is accessible.
        /// </summary>
        /// <returns>A task representing the test.</returns>
        [Fact]
        public async Task CheckHealthAsync_WhenRegistryIsAccessible_ReturnsHealthy()
        {
            // Arrange
            this._discoveryClientMock
                .Setup(x => x.IsRegistryAccessibleAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            this._discoveryClientMock
                .Setup(x => x.GetRegisteredServicesCountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);

            var healthCheck = new ServiceDiscoveryHealthCheck(
                this._discoveryClientMock.Object,
                this._loggerMock.Object);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    "service-discovery",
                    healthCheck,
                    HealthStatus.Unhealthy,
                    [],
                    null),
            };

            // Act
            var result = await healthCheck.CheckHealthAsync(context);

            // Assert
            result.Status.Should().Be(HealthStatus.Healthy);
            result.Data.Should().ContainKey("latency_ms");
            result.Data.Should().ContainKey("registered_services_count");
            result.Data["registered_services_count"].Should().Be(5);
        }

        /// <summary>
        /// Verifies that the health check returns unhealthy when registry is not accessible.
        /// </summary>
        /// <returns>A task representing the test.</returns>
        [Fact]
        public async Task CheckHealthAsync_WhenRegistryIsNotAccessible_ReturnsUnhealthy()
        {
            // Arrange
            this._discoveryClientMock
                .Setup(x => x.IsRegistryAccessibleAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var healthCheck = new ServiceDiscoveryHealthCheck(
                this._discoveryClientMock.Object,
                this._loggerMock.Object);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    "service-discovery",
                    healthCheck,
                    HealthStatus.Unhealthy,
                    [],
                    null),
            };

            // Act
            var result = await healthCheck.CheckHealthAsync(context);

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Data.Should().ContainKey("registry_accessible");
            result.Data["registry_accessible"].Should().Be(false);
        }

        /// <summary>
        /// Verifies that the health check handles exceptions gracefully.
        /// </summary>
        /// <returns>A task representing the test.</returns>
        [Fact]
        public async Task CheckHealthAsync_WhenExceptionThrown_ReturnsUnhealthy()
        {
            // Arrange
            this._discoveryClientMock
                .Setup(x => x.IsRegistryAccessibleAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test error"));

            var healthCheck = new ServiceDiscoveryHealthCheck(
                this._discoveryClientMock.Object,
                this._loggerMock.Object);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    "service-discovery",
                    healthCheck,
                    HealthStatus.Unhealthy,
                    [],
                    null),
            };

            // Act
            var result = await healthCheck.CheckHealthAsync(context);

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Exception.Should().NotBeNull();
            result.Exception!.Message.Should().Be("Test error");
        }

        /// <summary>
        /// Verifies that cancellation token is respected.
        /// </summary>
        /// <returns>A task representing the test.</returns>
        [Fact]
        public async Task CheckHealthAsync_WhenCancelled_ThrowsOperationCanceledException()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            this._discoveryClientMock
                .Setup(x => x.IsRegistryAccessibleAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var healthCheck = new ServiceDiscoveryHealthCheck(
                this._discoveryClientMock.Object,
                this._loggerMock.Object);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    "service-discovery",
                    healthCheck,
                    HealthStatus.Unhealthy,
                    [],
                    null),
            };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => healthCheck.CheckHealthAsync(context, cts.Token));
        }
    }
}
