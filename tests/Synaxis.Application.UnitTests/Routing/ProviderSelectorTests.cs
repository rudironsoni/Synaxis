// <copyright file="ProviderSelectorTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Tests.Routing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using Synaxis.Routing;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="ProviderSelector"/>.
    /// </summary>
    public class ProviderSelectorTests
    {
        private readonly ILogger<ProviderSelector> _logger;
        private readonly ProviderSelector _selector;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderSelectorTests"/> class.
        /// </summary>
        public ProviderSelectorTests()
        {
            this._logger = Substitute.For<ILogger<ProviderSelector>>();
            this._selector = new ProviderSelector(this._logger);
        }

        [Fact]
        public async Task SelectProviderAsync_WithValidRequest_ReturnsProviderName()
        {
            // Arrange
            var request = new TestRequest();

            // Act
            var result = await this._selector.SelectProviderAsync(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task SelectProviderAsync_LogsDebugMessage_WithRequestType()
        {
            // Arrange
            var request = new TestRequest();

            // Act
            await this._selector.SelectProviderAsync(request, CancellationToken.None);

            // Assert
            this._logger.Received(1).Log(
                LogLevel.Debug,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Selecting provider") && o.ToString()!.Contains("TestRequest")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public async Task SelectProviderAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await this._selector.SelectProviderAsync(null!, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("request");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new ProviderSelector(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public async Task SelectProviderAsync_WithDifferentRequestTypes_ReturnsProvider()
        {
            // Arrange
            var request1 = new TestRequest();
            var request2 = new AnotherTestRequest();

            // Act
            var result1 = await this._selector.SelectProviderAsync(request1, CancellationToken.None);
            var result2 = await this._selector.SelectProviderAsync(request2, CancellationToken.None);

            // Assert
            result1.Should().NotBeNullOrEmpty();
            result2.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task SelectProviderAsync_CurrentImplementation_ReturnsDefaultProvider()
        {
            // Arrange
            var request = new TestRequest();

            // Act
            var result = await this._selector.SelectProviderAsync(request, CancellationToken.None);

            // Assert
            result.Should().Be("default");
        }

        [Fact]
        public async Task SelectProviderAsync_WithCancellationToken_CompletesSuccessfully()
        {
            // Arrange
            var request = new TestRequest();
            using var cts = new CancellationTokenSource();

            // Act
            var result = await this._selector.SelectProviderAsync(request, cts.Token);

            // Assert
            result.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task SelectProviderAsync_WithCancelledToken_StillCompletes()
        {
            // Arrange
            var request = new TestRequest();
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            // Act - The current implementation doesn't check cancellation token
            var result = await this._selector.SelectProviderAsync(request, cts.Token);

            // Assert
            result.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task SelectProviderAsync_CalledMultipleTimes_ReturnsConsistentResults()
        {
            // Arrange
            var request = new TestRequest();

            // Act
            var result1 = await this._selector.SelectProviderAsync(request, CancellationToken.None);
            var result2 = await this._selector.SelectProviderAsync(request, CancellationToken.None);
            var result3 = await this._selector.SelectProviderAsync(request, CancellationToken.None);

            // Assert
            result1.Should().Be(result2);
            result2.Should().Be(result3);
        }

        public sealed record TestRequest;

        public sealed record AnotherTestRequest;
    }
}
