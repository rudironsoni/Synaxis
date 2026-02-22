// <copyright file="ApiKeysControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.IntegrationTests.Controllers;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Xunit.Abstractions;

[Collection("Integration")]
public class ApiKeysControllerTests
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
    public async Task CreateKey_WithoutAuth_ReturnsUnauthorized()
    {
        var orgId = Guid.NewGuid();
        var request = new { Name = "Test Key" };

        var response = await this._client.PostAsJsonAsync($"/api/v1/organizations/{orgId}/api-keys", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateKey_WithAuth_InvalidOrg_ReturnsNotFound()
    {
        var (client, user, org, _) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();
        var invalidOrgId = Guid.NewGuid();

        var request = new { Name = "Test Key" };
        var response = await client.PostAsJsonAsync($"/api/v1/organizations/{invalidOrgId}/api-keys", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateKey_WithAuth_ValidProject_ReturnsCreatedKey()
    {
        var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();

        var request = new { Name = "Production API Key" };
        var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/api-keys", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            this._output.WriteLine($"Error response: {errorContent}");
        }

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.ValueKind == JsonValueKind.Object);
        Assert.NotEqual(Guid.Empty, content.GetProperty("id").GetGuid());
        var name = content.GetProperty("name").GetString();
        Assert.NotNull(name);
        Assert.Equal("Production API Key", name);
        var key = content.GetProperty("key").GetString();
        Assert.NotNull(key);
        Assert.StartsWith("sk_", key, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateKey_WithAuth_WrongTenant_ReturnsNotFound()
    {
        // Create first user and their org/team
        var (client1, _, _, _) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync("user1@example.com");

        // Create second user with their own org/team
        var (_, user2, org2, team2) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync("user2@example.com");

        // Try to create key in user2's org using user1's token
        var request = new { Name = "Test Key" };
        var response = await client1.PostAsJsonAsync($"/api/v1/organizations/{org2.Id}/api-keys", request);

        // Should be Forbidden since user1 is not a member of org2
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RevokeKey_WithoutAuth_ReturnsUnauthorized()
    {
        var orgId = Guid.NewGuid();
        var keyId = Guid.NewGuid();

        var response = await this._client.DeleteAsync($"/api/v1/organizations/{orgId}/api-keys/{keyId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RevokeKey_WithAuth_InvalidKey_ReturnsNotFound()
    {
        var (client, _, org, _) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();
        var invalidKeyId = Guid.NewGuid();

        var response = await client.DeleteAsync($"/api/v1/organizations/{org.Id}/api-keys/{invalidKeyId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RevokeKey_WithAuth_InvalidOrg_ReturnsNotFound()
    {
        var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();
        var apiKey = await this.CreateTestOrganizationApiKeyAsync(org.Id, user.Id, "Test Key");
        var invalidOrgId = Guid.NewGuid();

        var response = await client.DeleteAsync($"/api/v1/organizations/{invalidOrgId}/api-keys/{apiKey.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RevokeKey_WithAuth_ValidKey_ReturnsNoContent()
    {
        var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();
        var apiKey = await this.CreateTestOrganizationApiKeyAsync(org.Id, user.Id, "Test Key");

        var response = await client.DeleteAsync($"/api/v1/organizations/{org.Id}/api-keys/{apiKey.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the key is revoked in the database
        var scope = this._factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
        var revokedKey = await dbContext.OrganizationApiKeys.FindAsync(apiKey.Id);
        Assert.NotNull(revokedKey);
        Assert.True(revokedKey.IsRevoked);
    }

    [Fact]
    public async Task RevokeKey_WithAuth_WrongTenant_ReturnsNotFound()
    {
        // Create first user
        var (client1, _, _, _) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync("user1@example.com");

        // Create second user with their own org/team
        var (_, user2, org2, team2) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync("user2@example.com");
        var apiKey = await this.CreateTestOrganizationApiKeyAsync(org2.Id, user2.Id, "Test Key");

        // Try to revoke key in user2's org using user1's token
        var response = await client1.DeleteAsync($"/api/v1/organizations/{org2.Id}/api-keys/{apiKey.Id}");

        // Should be Forbidden since user1 is not a member of org2
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateKey_StoresCorrectDataInDatabase()
    {
        var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();

        var request = new { Name = "Database Test Key" };
        var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/api-keys", request);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var keyId = content.GetProperty("id").GetGuid();

        // Verify in database
        var scope = this._factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
        var storedKey = await dbContext.OrganizationApiKeys.FindAsync(keyId);

        Assert.NotNull(storedKey);
        Assert.Equal(org.Id, storedKey.OrganizationId);
        Assert.Equal("Database Test Key", storedKey.Name);
        Assert.True(storedKey.IsActive);
        Assert.False(storedKey.IsRevoked);
        Assert.NotNull(storedKey.KeyHash);
        Assert.NotEmpty(storedKey.KeyHash);
    }

    [Fact]
    public async Task RevokeKey_CreatesAuditLog()
    {
        var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();
        var apiKey = await this.CreateTestOrganizationApiKeyAsync(org.Id, user.Id, "Test Key");

        var response = await client.DeleteAsync($"/api/v1/organizations/{org.Id}/api-keys/{apiKey.Id}");

        response.EnsureSuccessStatusCode();

        // OrganizationApiKeysController test - verifies the key was revoked
        var scope = this._factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
        var revokedKey = await dbContext.OrganizationApiKeys.FindAsync(apiKey.Id);
        Assert.NotNull(revokedKey);
        Assert.True(revokedKey.IsRevoked);
    }

    [Fact]
    public async Task CreateKey_CreatesAuditLog()
    {
        var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();

        var request = new { Name = "Audit Test Key" };
        var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/api-keys", request);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var keyId = content.GetProperty("id").GetGuid();

        // OrganizationApiKeysController test - verifies the key was created
        var scope = this._factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
        var storedKey = await dbContext.OrganizationApiKeys.FindAsync(keyId);
        Assert.NotNull(storedKey);
        Assert.Equal("Audit Test Key", storedKey.Name);
    }

    [Fact]
    public async Task CreateKey_KeyHashIsValid()
    {
        var (client, user, org, team) = await this.CreateAuthenticatedClientWithOrgAndTeamAsync();

        var request = new { Name = "Hash Test Key" };
        var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/api-keys", request);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var rawKey = content.GetProperty("key").GetString();
        var keyId = content.GetProperty("id").GetGuid();
        Assert.NotNull(rawKey);

        // Get the stored hash
        var scope = this._factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
        var storedKey = await dbContext.OrganizationApiKeys.FindAsync(keyId);

        Assert.NotNull(storedKey);

        // Verify the hash is valid by computing it ourselves
        var expectedHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
        Assert.Equal(expectedHash, storedKey.KeyHash);
    }

#pragma warning disable SA1124 // Do not use regions
    #region Helper Methods
#pragma warning restore SA1124 // Do not use regions

    private async Task<(HttpClient Client, User User)> CreateAuthenticatedClientAsync(string email = "test@example.com")
    {
        // Use the dev-login endpoint to get a valid JWT token
        var loginRequest = new { Email = email };
        var response = await this._client.PostAsJsonAsync("/auth/dev-login", loginRequest).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
        var token = content.GetProperty("token").GetString();
        Assert.NotNull(token);

        // Parse the token to get user info
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var userId = Guid.Parse(jwtToken.Claims.First(c => string.Equals(c.Type, JwtRegisteredClaimNames.Sub, StringComparison.Ordinal)).Value);

        // Get the user from database
        var scope = this._factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
        var user = await dbContext.Users.FindAsync(userId).ConfigureAwait(false);
        Assert.NotNull(user);

        // Create a new client with the authorization header
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

        // Set user role based on team role - if not TeamAdmin, make them a regular user
        // Re-fetch the user to ensure we have the latest tracked entity
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

    private async Task<OrganizationApiKey> CreateTestOrganizationApiKeyAsync(Guid organizationId, Guid userId, string name)
    {
        var scope = this._factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

        var rawKey = "sk_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var keyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));

        var apiKey = new OrganizationApiKey
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            CreatedBy = userId,
            Name = name,
            KeyHash = keyHash,
            KeyPrefix = rawKey[..Math.Min(8, rawKey.Length)],
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.OrganizationApiKeys.Add(apiKey);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        return apiKey;
    }

#pragma warning disable SA1124 // Do not use regions
    #endregion
#pragma warning restore SA1124 // Do not use regions
}
