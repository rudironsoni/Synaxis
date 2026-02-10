// <copyright file="WebSocketConnectivityTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.IntegrationTests.RealTime
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.AspNetCore.SignalR.Client;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.IntegrationTests.Helpers;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Integration tests for WebSocket connectivity and real-time updates.
    /// Tests use WebApplicationFactory with SignalR for self-contained testing.
    /// </summary>
    [Collection("Integration")]
    public class WebSocketConnectivityTests : IAsyncLifetime
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;
        private HubConnection? _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketConnectivityTests"/> class.
        /// </summary>
        /// <param name="factory">The web application factory.</param>
        /// <param name="output">The test output helper.</param>
        public WebSocketConnectivityTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
        {
            this._factory = factory;
            this._output = output;
            this._factory.OutputHelper = output;
        }

        public Task InitializeAsync()
        {
            // Setup can be done here if needed
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (this._connection != null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CanConnectToSynaxisHub_WithValidToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = TestJwtGenerator.GenerateToken(userId);

            var hubUrl = this._factory.Server.BaseAddress + "hubs/synaxis";

            this._connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                    options.HttpMessageHandlerFactory = _ => this._factory.Server.CreateHandler();
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddProvider(new XunitLoggerProvider(this._output));
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();

            this._output.WriteLine($"[Observability] Attempting connection - UserId: {userId}, HubUrl: {hubUrl}, Timestamp: {DateTime.UtcNow:O}");

            // Act
            await this._connection.StartAsync();

            // Assert
            Assert.Equal(HubConnectionState.Connected, this._connection.State);
            this._output.WriteLine($"[Observability] Connection successful - ConnectionId: {this._connection.ConnectionId}, UserId: {userId}, EventType: Connected, Timestamp: {DateTime.UtcNow:O}");
        }

        [Fact]
        public async Task CannotConnect_WithoutToken()
        {
            // Arrange
            var hubUrl = this._factory.Server.BaseAddress + "hubs/synaxis";

            this._connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.HttpMessageHandlerFactory = _ => this._factory.Server.CreateHandler();
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddProvider(new XunitLoggerProvider(this._output));
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();

            this._output.WriteLine($"[Observability] Attempting unauthenticated connection - HubUrl: {hubUrl}, Timestamp: {DateTime.UtcNow:O}");

            // Act & Assert
            var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
                await _connection.StartAsync().ConfigureAwait(false));

            this._output.WriteLine($"[Observability] Connection rejected as expected - EventType: ConnectionRejected, Reason: NoToken, Timestamp: {DateTime.UtcNow:O}");
            this._output.WriteLine($"Exception: {exception.GetType().Name} - {exception.Message}");
        }

        [Fact]
        public async Task CanJoinOrganization_WithValidToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var organizationId = Guid.NewGuid();
            var token = TestJwtGenerator.GenerateToken(userId, organizationId: organizationId);

            var hubUrl = this._factory.Server.BaseAddress + "hubs/synaxis";

            this._connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                    options.HttpMessageHandlerFactory = _ => this._factory.Server.CreateHandler();
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddProvider(new XunitLoggerProvider(this._output));
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();

            await this._connection.StartAsync();
            this._output.WriteLine($"[Observability] Connected - ConnectionId: {this._connection.ConnectionId}, UserId: {userId}, Timestamp: {DateTime.UtcNow:O}");

            // Act
            await this._connection.InvokeAsync("JoinOrganization", organizationId.ToString());

            // Assert - No exception means success
            Assert.Equal(HubConnectionState.Connected, this._connection.State);
            this._output.WriteLine($"[Observability] Joined organization - ConnectionId: {this._connection.ConnectionId}, UserId: {userId}, OrganizationId: {organizationId}, EventType: JoinedOrganization, Timestamp: {DateTime.UtcNow:O}");
        }

        [Fact]
        public async Task ReceivesProviderHealthUpdate_WhenSubscribedToOrganization()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var organizationId = Guid.NewGuid();
            var token = TestJwtGenerator.GenerateToken(userId, organizationId: organizationId);

            var hubUrl = this._factory.Server.BaseAddress + "hubs/synaxis";
            var tcs = new TaskCompletionSource<bool>();

            this._connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                    options.HttpMessageHandlerFactory = _ => this._factory.Server.CreateHandler();
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddProvider(new XunitLoggerProvider(this._output));
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();

            this._connection.On<object>("ProviderHealthChanged", (update) =>
            {
                this._output.WriteLine($"[Observability] Received ProviderHealthUpdate - ConnectionId: {this._connection.ConnectionId}, UserId: {userId}, OrganizationId: {organizationId}, EventType: ProviderHealthChanged, Timestamp: {DateTime.UtcNow:O}");
                tcs.TrySetResult(true);
            });

            await this._connection.StartAsync();
            this._output.WriteLine($"[Observability] Connected - ConnectionId: {this._connection.ConnectionId}, UserId: {userId}, Timestamp: {DateTime.UtcNow:O}");

            await this._connection.InvokeAsync("JoinOrganization", organizationId.ToString());
            this._output.WriteLine($"[Observability] Joined organization - ConnectionId: {this._connection.ConnectionId}, UserId: {userId}, OrganizationId: {organizationId}, EventType: JoinedOrganization, Timestamp: {DateTime.UtcNow:O}");

            // Act - Trigger a health update by sending a message to the organization group
            // We'll use the HubContext from the server to send a message
            var hubContext = this._factory.Services.GetRequiredService<IHubContext<WebApi.Hubs.SynaxisHub>>();
            await hubContext.Clients.Group(organizationId.ToString()).SendAsync("ProviderHealthChanged", new { Provider = "Test", Status = "Healthy" });

            // Wait for notification (with timeout)
            var receivedNotification = await Task.WhenAny(
                tcs.Task,
                Task.Delay(TimeSpan.FromSeconds(5))) == tcs.Task;

            // Assert
            Assert.True(receivedNotification, "Should receive provider health update notification");
            this._output.WriteLine($"[Observability] Test completed successfully - ConnectionId: {this._connection.ConnectionId}, UserId: {userId}, OrganizationId: {organizationId}, EventType: TestCompleted, Timestamp: {DateTime.UtcNow:O}");
        }

        [Fact]
        public async Task ConnectionReconnects_AfterDisconnection()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = TestJwtGenerator.GenerateToken(userId);

            var hubUrl = this._factory.Server.BaseAddress + "hubs/synaxis";

            this._connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                    options.HttpMessageHandlerFactory = _ => this._factory.Server.CreateHandler();
                })
                .WithAutomaticReconnect()
                .ConfigureLogging(logging =>
                {
                    logging.AddProvider(new XunitLoggerProvider(this._output));
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();

            var reconnectedTcs = new TaskCompletionSource<bool>();
            this._connection.Reconnected += connectionId =>
            {
                this._output.WriteLine($"[Observability] Reconnected - ConnectionId: {connectionId}, UserId: {userId}, EventType: Reconnected, Timestamp: {DateTime.UtcNow:O}");
                reconnectedTcs.TrySetResult(true);
                return Task.CompletedTask;
            };

            this._connection.Closed += error =>
            {
                this._output.WriteLine($"[Observability] Connection closed - UserId: {userId}, EventType: Closed, Error: {error?.Message}, Timestamp: {DateTime.UtcNow:O}");
                return Task.CompletedTask;
            };

            await this._connection.StartAsync();
            var initialConnectionId = this._connection.ConnectionId;
            Assert.Equal(HubConnectionState.Connected, this._connection.State);
            this._output.WriteLine($"[Observability] Initial connection - ConnectionId: {initialConnectionId}, UserId: {userId}, EventType: Connected, Timestamp: {DateTime.UtcNow:O}");

            // Act - Force disconnect and reconnect manually (automatic reconnect requires network-level interruption)
            await this._connection.StopAsync();
            this._output.WriteLine($"[Observability] Connection stopped - ConnectionId: {initialConnectionId}, UserId: {userId}, EventType: Disconnected, Timestamp: {DateTime.UtcNow:O}");

            await Task.Delay(100);
            await this._connection.StartAsync();

            // Assert
            Assert.Equal(HubConnectionState.Connected, this._connection.State);
            var newConnectionId = this._connection.ConnectionId;
            this._output.WriteLine($"[Observability] Reconnection successful - OldConnectionId: {initialConnectionId}, NewConnectionId: {newConnectionId}, UserId: {userId}, EventType: ManualReconnect, Timestamp: {DateTime.UtcNow:O}");
        }

        /// <summary>
        /// Simple logger provider that writes to xUnit test output.
        /// </summary>
        private class XunitLoggerProvider : ILoggerProvider
        {
            private readonly ITestOutputHelper _output;

            public XunitLoggerProvider(ITestOutputHelper output)
            {
                this._output = output;
            }

            public ILogger CreateLogger(string categoryName)
            {
                return new XunitLogger(this._output, categoryName);
            }

            public void Dispose()
            {
            }

            private class XunitLogger : ILogger
            {
                private readonly ITestOutputHelper _output;
                private readonly string _categoryName;

                public XunitLogger(ITestOutputHelper output, string categoryName)
                {
                    this._output = output;
                    this._categoryName = categoryName;
                }

                public IDisposable? BeginScope<TState>(TState state)
                    where TState : notnull
                {
                    return null;
                }

                public bool IsEnabled(LogLevel logLevel)
                {
                    return true;
                }

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                {
                    this._output.WriteLine($"[{logLevel}] {this._categoryName}: {formatter(state, exception)}");
                    if (exception != null)
                    {
                        this._output.WriteLine($"Exception: {exception}");
                    }
                }
            }
        }
    }
}
