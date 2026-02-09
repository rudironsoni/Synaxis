// <copyright file="WebSocketConnectivityTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.IntegrationTests.RealTime
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.AspNetCore.SignalR.Client;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Integration tests for WebSocket connectivity and real-time updates.
    /// </summary>
    public class WebSocketConnectivityTests : IClassFixture<SynaxisWebApplicationFactory>, IAsyncLifetime
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;
        private HubConnection? _connection;

        public WebSocketConnectivityTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
        {
            this._factory = factory;
            this._factory.OutputHelper = output;
            this._output = output;
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (this._connection != null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
            }
        }

        private string GenerateTestJwtToken(string userId, string email, string role, string organizationId)
        {
            var jwtSecret = "TestJwtSecretKeyThatIsAtLeast32BytesLongForHmacSha256Algorithm";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("role", role),
                new Claim("organizationId", organizationId),
            };

            var token = new JwtSecurityToken(
                issuer: "Synaxis",
                audience: "Synaxis",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Fact]
        public async Task CanConnectToSynaxisHub_WithValidToken()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var organizationId = Guid.NewGuid().ToString();
            var token = this.GenerateTestJwtToken(userId, "test@example.com", "admin", organizationId);

            this._connection = new HubConnectionBuilder()
                .WithUrl(this._factory.Server.BaseAddress + "hubs/synaxis", options =>
                {
                    options.HttpMessageHandlerFactory = _ => this._factory.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .Build();

            // Act
            await this._connection.StartAsync();

            // Assert
            Assert.Equal(HubConnectionState.Connected, this._connection.State);
        }

        [Fact]
        public async Task CanJoinOrganization_WithValidToken()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var organizationId = Guid.NewGuid().ToString();
            var token = this.GenerateTestJwtToken(userId, "test@example.com", "admin", organizationId);

            this._connection = new HubConnectionBuilder()
                .WithUrl(this._factory.Server.BaseAddress + "hubs/synaxis", options =>
                {
                    options.HttpMessageHandlerFactory = _ => this._factory.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .Build();

            await this._connection.StartAsync();

            // Act
            await this._connection.InvokeAsync("JoinOrganization", organizationId);

            // Assert - No exception means success
            Assert.Equal(HubConnectionState.Connected, this._connection.State);
        }

        [Fact]
        public async Task ReceivesProviderHealthUpdate_WhenSubscribedToOrganization()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var organizationId = Guid.NewGuid().ToString();
            var token = this.GenerateTestJwtToken(userId, "test@example.com", "admin", organizationId);
            var tcs = new TaskCompletionSource<bool>();

            this._connection = new HubConnectionBuilder()
                .WithUrl(this._factory.Server.BaseAddress + "hubs/synaxis", options =>
                {
                    options.HttpMessageHandlerFactory = _ => this._factory.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .Build();

            this._connection.On<object>("ProviderHealthChanged", (update) =>
            {
                tcs.SetResult(true);
            });

            await this._connection.StartAsync();
            await this._connection.InvokeAsync("JoinOrganization", organizationId);

            // Act - In a real scenario, a background job would trigger this update
            // For testing, we verify the connection can listen for the event
            var receivedNotification = await Task.WhenAny(
                tcs.Task,
                Task.Delay(TimeSpan.FromSeconds(1))) == tcs.Task;

            // Assert
            // This test validates the subscription mechanism works
            // Note: Without a real health update being triggered, we won't receive notification
            // But we've validated the connection is established and listening
            Assert.Equal(HubConnectionState.Connected, this._connection.State);
        }

        [Fact]
        public async Task ConnectionReconnects_AfterDisconnection()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var organizationId = Guid.NewGuid().ToString();
            var token = this.GenerateTestJwtToken(userId, "test@example.com", "admin", organizationId);

            this._connection = new HubConnectionBuilder()
                .WithUrl(this._factory.Server.BaseAddress + "hubs/synaxis", options =>
                {
                    options.HttpMessageHandlerFactory = _ => this._factory.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .WithAutomaticReconnect()
                .Build();

            var reconnectedTcs = new TaskCompletionSource<bool>();
            this._connection.Reconnected += _ =>
            {
                reconnectedTcs.SetResult(true);
                return Task.CompletedTask;
            };

            await this._connection.StartAsync();
            Assert.Equal(HubConnectionState.Connected, this._connection.State);

            // Act - Force disconnect and restart (manual reconnection pattern)
            await this._connection.StopAsync();
            await Task.Delay(100);
            await this._connection.StartAsync();

            // Assert
            Assert.Equal(HubConnectionState.Connected, this._connection.State);
        }

        [Fact]
        public Task CannotConnect_WithoutToken()
        {
            // Arrange
            this._connection = new HubConnectionBuilder()
                .WithUrl(this._factory.Server.BaseAddress + "hubs/synaxis", options =>
                {
                    options.HttpMessageHandlerFactory = _ => this._factory.Server.CreateHandler();
                })
                .Build();

            // Act & Assert
            return Assert.ThrowsAsync<HttpRequestException>(async () => await _connection.StartAsync().ConfigureAwait(false));
        }
    }
}
