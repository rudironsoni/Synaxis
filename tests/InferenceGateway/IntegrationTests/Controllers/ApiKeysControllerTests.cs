// <copyright file="ApiKeysControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.IntegrationTests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Json;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Integration tests for ApiKeysController.
    /// </summary>
    [Trait("Category", "Integration")]
    public class ApiKeysControllerTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;

        public ApiKeysControllerTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
        {
            this._factory = factory;
            this._factory.OutputHelper = output;
            this._output = output;
            this._client = this._factory.CreateClient();
        }

        [Fact]
        public async Task CreateApiKey_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var request = new { TeamId = Guid.NewGuid(), Name = "Test Key" };

            var response = await this._client.PostAsJsonAsync($"/api/v1/organizations/{orgId}/api-keys", request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateApiKey_ValidRequest_ReturnsPlainKeyOnce()
        {
            var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();

            var request = new
            {
                TeamId = team.Id,
                Name = "Production API Key",
                Description = "Test key",
                MaxBudget = 100.00m,
                RpmLimit = 1000,
                TpmLimit = 100000,
                AllowedModels = new[] { "gpt-4", "gpt-3.5-turbo" },
                Tags = new[] { "production", "critical" }
            };

            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/api-keys", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                this._output.WriteLine($"Error response: {errorContent}");
            }

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("id").GetGuid().Should().NotBe(Guid.Empty);
            content.GetProperty("name").GetString().Should().Be("Production API Key");
            var plainKey = content.GetProperty("key").GetString();
            plainKey.Should().NotBeNullOrEmpty();
            plainKey.Should().StartWith("sk_");

            var keyId = content.GetProperty("id").GetGuid();

            using var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var storedKey = await dbContext.VirtualKeys.FindAsync(keyId);

            storedKey.Should().NotBeNull();
            storedKey.KeyHash.Should().NotBeNullOrEmpty();
            storedKey.Name.Should().Be("Production API Key");
            storedKey.Description.Should().Be("Test key");
            storedKey.MaxBudget.Should().Be(100.00m);
            storedKey.RpmLimit.Should().Be(1000);
            storedKey.TpmLimit.Should().Be(100000);
            storedKey.AllowedModels.Should().BeEquivalentTo("gpt-4", "gpt-3.5-turbo");
            storedKey.Tags.Should().BeEquivalentTo("production", "critical");
            storedKey.IsActive.Should().BeTrue();
            storedKey.IsRevoked.Should().BeFalse();

            var expectedHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(plainKey)));
            storedKey.KeyHash.Should().Be(expectedHash);
        }

        [Fact]
        public async Task CreateApiKey_NonTeamAdmin_ReturnsForbidden()
        {
            var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync("member@test.com", "Member");

            var request = new { TeamId = team.Id, Name = "Test Key" };

            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/api-keys", request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateApiKey_InvalidTeam_ReturnsNotFound()
        {
            var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();

            var request = new { TeamId = Guid.NewGuid(), Name = "Test Key" };

            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/api-keys", request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ListApiKeys_ReturnsKeysWithoutKeyHash()
        {
            var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();

            await this.CreateTestVirtualKeyAsync(org.Id, team.Id, user.Id, "Key 1");
            await this.CreateTestVirtualKeyAsync(org.Id, team.Id, user.Id, "Key 2");

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/api-keys");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var items = content.GetProperty("items");
            items.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);

            foreach (var item in items.EnumerateArray())
            {
                item.TryGetProperty("keyHash", out _).Should().BeFalse();
                item.TryGetProperty("key", out _).Should().BeFalse();
                item.TryGetProperty("id", out _).Should().BeTrue();
                item.TryGetProperty("name", out _).Should().BeTrue();
            }
        }

        [Fact]
        public async Task ListApiKeys_WithTeamFilter_ReturnsFilteredKeys()
        {
            var (client, user, org, team1) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();
            var team2 = await this.CreateTestTeamAsync(org.Id, "Team 2", user.Id);

            await this.CreateTestVirtualKeyAsync(org.Id, team1.Id, user.Id, "Team 1 Key");
            await this.CreateTestVirtualKeyAsync(org.Id, team2.Id, user.Id, "Team 2 Key");

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/api-keys?teamId={team1.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var items = content.GetProperty("items").EnumerateArray().ToList();
            items.Should().ContainSingle();
            items[0].GetProperty("name").GetString().Should().Be("Team 1 Key");
        }

        [Fact]
        public async Task ListApiKeys_PaginationWorks()
        {
            var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();

            for (int i = 0; i < 15; i++)
            {
                await this.CreateTestVirtualKeyAsync(org.Id, team.Id, user.Id, $"Key {i}");
            }

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/api-keys?page=1&pageSize=10");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("items").GetArrayLength().Should().Be(10);
            content.GetProperty("page").GetInt32().Should().Be(1);
            content.GetProperty("pageSize").GetInt32().Should().Be(10);
            content.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(15);
        }

        [Fact]
        public async Task GetApiKey_ReturnsKeyDetails()
        {
            var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();
            var key = await this.CreateTestVirtualKeyAsync(org.Id, team.Id, user.Id, "Test Key");

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/api-keys/{key.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("id").GetGuid().Should().Be(key.Id);
            content.GetProperty("name").GetString().Should().Be("Test Key");
            content.TryGetProperty("keyHash", out _).Should().BeFalse();
            content.TryGetProperty("key", out _).Should().BeFalse();
        }

        [Fact]
        public async Task GetApiKey_NonMember_ReturnsForbidden()
        {
            var (client1, user1, org1, team1) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync("user1@test.com");
            var (client2, user2, org2, team2) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync("user2@test.com");

            var key = await this.CreateTestVirtualKeyAsync(org1.Id, team1.Id, user1.Id, "Test Key");

            var response = await client2.GetAsync($"/api/v1/organizations/{org1.Id}/api-keys/{key.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UpdateApiKey_ValidRequest_UpdatesKey()
        {
            var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();
            var key = await this.CreateTestVirtualKeyAsync(org.Id, team.Id, user.Id, "Old Name");

            var updateRequest = new
            {
                Name = "Updated Name",
                Description = "Updated description",
                MaxBudget = 200.00m,
                RpmLimit = 2000,
                IsActive = false
            };

            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/api-keys/{key.Id}", updateRequest);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("name").GetString().Should().Be("Updated Name");
            content.GetProperty("description").GetString().Should().Be("Updated description");
            content.GetProperty("maxBudget").GetDecimal().Should().Be(200.00m);
            content.GetProperty("rpmLimit").GetInt32().Should().Be(2000);
            content.GetProperty("isActive").GetBoolean().Should().BeFalse();
        }

        [Fact]
        public async Task UpdateApiKey_NonAdmin_ReturnsForbidden()
        {
            var (adminClient, adminUser, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync("admin@test.com");
            var (memberClient, memberUser) = await this.CreateAuthenticatedClientAsync("member@test.com");

            await this.AddUserToTeamAsync(memberUser.Id, team.Id, org.Id, "Member");

            var key = await this.CreateTestVirtualKeyAsync(org.Id, team.Id, adminUser.Id, "Test Key");

            var updateRequest = new { Name = "Updated Name" };

            var response = await memberClient.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/api-keys/{key.Id}", updateRequest);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task RevokeApiKey_ValidRequest_RevokesKey()
        {
            var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();
            var key = await this.CreateTestVirtualKeyAsync(org.Id, team.Id, user.Id, "Test Key");

            var response = await client.DeleteAsync($"/api/v1/organizations/{org.Id}/api-keys/{key.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var revokedKey = await dbContext.VirtualKeys.FindAsync(key.Id);

            revokedKey.Should().NotBeNull();
            revokedKey.IsRevoked.Should().BeTrue();
            revokedKey.RevokedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task RevokeApiKey_NonAdmin_ReturnsForbidden()
        {
            var (adminClient, adminUser, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync("admin@test.com");
            var (memberClient, memberUser) = await this.CreateAuthenticatedClientAsync("member@test.com");

            await this.AddUserToTeamAsync(memberUser.Id, team.Id, org.Id, "Member");

            var key = await this.CreateTestVirtualKeyAsync(org.Id, team.Id, adminUser.Id, "Test Key");

            var response = await memberClient.DeleteAsync($"/api/v1/organizations/{org.Id}/api-keys/{key.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task RotateApiKey_ValidRequest_GeneratesNewKey()
        {
            var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();
            var key = await this.CreateTestVirtualKeyAsync(org.Id, team.Id, user.Id, "Test Key");
            var oldKeyHash = key.KeyHash;

            var response = await client.PostAsync($"/api/v1/organizations/{org.Id}/api-keys/{key.Id}/rotate", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var newPlainKey = content.GetProperty("key").GetString();
            newPlainKey.Should().NotBeNullOrEmpty();
            newPlainKey.Should().StartWith("sk_");

            using var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var rotatedKey = await dbContext.VirtualKeys.FindAsync(key.Id);

            rotatedKey.Should().NotBeNull();
            rotatedKey.KeyHash.Should().NotBe(oldKeyHash);

            var expectedNewHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(newPlainKey)));
            rotatedKey.KeyHash.Should().Be(expectedNewHash);
        }

        [Fact]
        public async Task RotateApiKey_NonAdmin_ReturnsForbidden()
        {
            var (adminClient, adminUser, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync("admin@test.com");
            var (memberClient, memberUser) = await this.CreateAuthenticatedClientAsync("member@test.com");

            await this.AddUserToTeamAsync(memberUser.Id, team.Id, org.Id, "Member");

            var key = await this.CreateTestVirtualKeyAsync(org.Id, team.Id, adminUser.Id, "Test Key");

            var response = await memberClient.PostAsync($"/api/v1/organizations/{org.Id}/api-keys/{key.Id}/rotate", null);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetUsage_ReturnsUsageStats()
        {
            var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();
            var key = await this.CreateTestVirtualKeyAsync(org.Id, team.Id, user.Id, "Test Key", maxBudget: 100.00m, currentSpend: 25.50m);

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/api-keys/{key.Id}/usage");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("currentSpend").GetDecimal().Should().Be(25.50m);
            content.GetProperty("remainingBudget").GetDecimal().Should().Be(74.50m);
            content.GetProperty("requestCount").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task GetUsage_NoBudget_ReturnsNullRemainingBudget()
        {
            var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();
            var key = await this.CreateTestVirtualKeyAsync(org.Id, team.Id, user.Id, "Test Key", maxBudget: null);

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/api-keys/{key.Id}/usage");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("remainingBudget").ValueKind.Should().Be(JsonValueKind.Null);
        }

        [Fact]
        public async Task CreateApiKey_NegativeBudget_ReturnsBadRequest()
        {
            var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();

            var request = new { TeamId = team.Id, Name = "Test Key", MaxBudget = -100.00m };

            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/api-keys", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateApiKey_NegativeRpmLimit_ReturnsBadRequest()
        {
            var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();

            var request = new { TeamId = team.Id, Name = "Test Key", RpmLimit = -100 };

            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/api-keys", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateApiKey_SetsUserRegion()
        {
            var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();

            var request = new { TeamId = team.Id, Name = "Test Key" };

            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/api-keys", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var keyId = content.GetProperty("id").GetGuid();

            using var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var key = await dbContext.VirtualKeys.FindAsync(keyId);

            key.Should().NotBeNull();
            key.UserRegion.Should().NotBeNullOrEmpty();
        }

#pragma warning disable SA1124
        #region Helper Methods
#pragma warning restore SA1124

        private async Task<(HttpClient Client, User User)> CreateAuthenticatedClientAsync(string email = "test@example.com")
        {
            var loginRequest = new { Email = email };
            var response = await this._client.PostAsJsonAsync("/auth/dev-login", loginRequest).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
            var token = content.GetProperty("token").GetString();
            Assert.NotNull(token);

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var userId = Guid.Parse(jwtToken.Claims.First(c => string.Equals(c.Type, JwtRegisteredClaimNames.Sub, StringComparison.Ordinal)).Value);

            var scope = this._factory.Services.CreateScope();
            var synaxisDbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var user = await synaxisDbContext.Users.FindAsync(userId).ConfigureAwait(false);
            Assert.NotNull(user);

            var authenticatedClient = this._factory.CreateClient();
            authenticatedClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return (authenticatedClient, user);
        }

        private async Task<(HttpClient Client, User User, Organization Org, Team Team)> CreateAuthenticatedClientWithOrgAndTeamAsync(string email = "test@example.com", string role = "TeamAdmin")
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync(email).ConfigureAwait(false);

            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

            var org = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == user.OrganizationId).ConfigureAwait(false);
            if (org == null)
            {
                org = new Organization
                {
                    Id = Guid.NewGuid(),
                    Name = $"Test Org {email}",
                    Slug = $"test-org-{Guid.NewGuid():N}",
                    PrimaryRegion = "us-east-1",
                    Tier = "free",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.Organizations.Add(org);

                user.OrganizationId = org.Id;
            }

            var trackedUser = await dbContext.Users.FindAsync(user.Id).ConfigureAwait(false);
            if (trackedUser != null)
            {
                if (string.Equals(role, "TeamAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    trackedUser.Role = "owner";
                }
                else
                {
                    trackedUser.Role = "user";
                }

                dbContext.Entry(trackedUser).State = EntityState.Modified;
            }

            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                Name = "Test Team",
                Slug = $"test-team-{Guid.NewGuid():N}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.Teams.Add(team);

            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TeamId = team.Id,
                OrganizationId = org.Id,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };
            dbContext.TeamMemberships.Add(membership);

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return (client, user, org, team);
        }

        private async Task<VirtualKey> CreateTestVirtualKeyAsync(Guid organizationId, Guid teamId, Guid userId, string name, decimal? maxBudget = null, decimal currentSpend = 0.00m)
        {
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

            var user = await dbContext.Users.FindAsync(userId).ConfigureAwait(false);
            var org = await dbContext.Organizations.FindAsync(organizationId).ConfigureAwait(false);

            var rawKey = "sk_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var keyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));

            var virtualKey = new VirtualKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                TeamId = teamId,
                CreatedBy = userId,
                Name = name,
                KeyHash = keyHash,
                MaxBudget = maxBudget,
                CurrentSpend = currentSpend,
                UserRegion = user?.DataResidencyRegion ?? org?.PrimaryRegion ?? "us-east-1",
                IsActive = true,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.VirtualKeys.Add(virtualKey);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return virtualKey;
        }

        private async Task<Team> CreateTestTeamAsync(Guid organizationId, string name, Guid userId)
        {
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = name,
                Slug = $"{name.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal)}-{Guid.NewGuid():N}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Teams.Add(team);

            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TeamId = team.Id,
                OrganizationId = organizationId,
                Role = "TeamAdmin",
                JoinedAt = DateTime.UtcNow
            };

            dbContext.TeamMemberships.Add(membership);

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return team;
        }

#pragma warning disable AsyncFixer01 // Method only awaits a single expression - has setup code before await
        private async Task AddUserToTeamAsync(Guid userId, Guid teamId, Guid organizationId, string role)
        {
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TeamId = teamId,
                OrganizationId = organizationId,
                Role = role,
                JoinedAt = DateTime.UtcNow,
            };

            dbContext.TeamMemberships.Add(membership);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
#pragma warning restore AsyncFixer01

#pragma warning disable SA1124
        #endregion
#pragma warning restore SA1124
    }
}
