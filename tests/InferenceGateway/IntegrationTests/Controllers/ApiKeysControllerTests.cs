// <copyright file="ApiKeysControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.IntegrationTests.Controllers
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Net;
    using System.Net.Http.Json;
    using System.Security.Claims;
    using System.Text.Json;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
    using Synaxis.Infrastructure.Data;
    using Xunit.Abstractions;
    using User = Synaxis.Core.Models.User;

    [Collection("Integration")]
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

        [Fact(Skip = "ApiKeysController uses old Tenant/Project architecture - needs migration to Organization/Team")]
        public async Task CreateKey_WithoutAuth_ReturnsUnauthorized()
        {
            var request = new { Name = "Test Key" };
            var projectId = Guid.NewGuid();

            var response = await this._client.PostAsJsonAsync($"/projects/{projectId}/keys", request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(Skip = "ApiKeysController uses old Tenant/Project architecture - needs migration to Organization/Team")]
        public async Task CreateKey_WithAuth_InvalidProject_ReturnsNotFound()
        {
            var (client, _) = await this.CreateAuthenticatedClientAsync();
            var invalidProjectId = Guid.NewGuid();

            var request = new { Name = "Test Key" };
            var response = await client.PostAsJsonAsync($"/projects/{invalidProjectId}/keys", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact(Skip = "ApiKeysController uses old Tenant/Project architecture - needs migration to Organization/Team")]
        public async Task CreateKey_WithAuth_ValidProject_ReturnsCreatedKey()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();
            var project = await this.CreateTestProjectAsync(user.OrganizationId);

            var request = new { Name = "Production API Key" };
            var response = await client.PostAsJsonAsync($"/projects/{project.Id}/keys", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                this._output.WriteLine($"Error response: {errorContent}");
            }

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(content.ValueKind == JsonValueKind.Object);
            Assert.NotEqual(Guid.Empty, content.GetProperty("id").GetGuid());
            var name = content.GetProperty("name").GetString();
            Assert.NotNull(name);
            Assert.Equal("Production API Key", name);
            var key = content.GetProperty("key").GetString();
            Assert.NotNull(key);
            Assert.StartsWith("sk-synaxis-", key, StringComparison.Ordinal);
        }

        [Fact(Skip = "ApiKeysController uses old Tenant/Project architecture - needs migration to Organization/Team")]
        public async Task CreateKey_WithAuth_WrongTenant_ReturnsNotFound()
        {
            // Create first user and their project
            var (client1, _) = await this.CreateAuthenticatedClientAsync("user1@example.com");

            // Create second user with their own tenant
            var (_, user2) = await this.CreateAuthenticatedClientAsync("user2@example.com");
            var projectForUser2 = await this.CreateTestProjectAsync(user2.OrganizationId);

            // Try to create key in user2's project using user1's token
            var request = new { Name = "Test Key" };
            var response = await client1.PostAsJsonAsync($"/projects/{projectForUser2.Id}/keys", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact(Skip = "ApiKeysController uses old Tenant/Project architecture - needs migration to Organization/Team")]
        public async Task RevokeKey_WithoutAuth_ReturnsUnauthorized()
        {
            var projectId = Guid.NewGuid();
            var keyId = Guid.NewGuid();

            var response = await this._client.DeleteAsync($"/projects/{projectId}/keys/{keyId}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(Skip = "ApiKeysController uses old Tenant/Project architecture - needs migration to Organization/Team")]
        public async Task RevokeKey_WithAuth_InvalidKey_ReturnsNotFound()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();
            var project = await this.CreateTestProjectAsync(user.OrganizationId);
            var invalidKeyId = Guid.NewGuid();

            var response = await client.DeleteAsync($"/projects/{project.Id}/keys/{invalidKeyId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact(Skip = "ApiKeysController uses old Tenant/Project architecture - needs migration to Organization/Team")]
        public async Task RevokeKey_WithAuth_InvalidProject_ReturnsNotFound()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();
            var project = await this.CreateTestProjectAsync(user.OrganizationId);
            var apiKey = await this.CreateTestApiKeyAsync(project.Id);
            var invalidProjectId = Guid.NewGuid();

            var response = await client.DeleteAsync($"/projects/{invalidProjectId}/keys/{apiKey.Id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact(Skip = "ApiKeysController uses old Tenant/Project architecture - needs migration to Organization/Team")]
        public async Task RevokeKey_WithAuth_ValidKey_ReturnsNoContent()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();
            var project = await this.CreateTestProjectAsync(user.OrganizationId);
            var apiKey = await this.CreateTestApiKeyAsync(project.Id);

            var response = await client.DeleteAsync($"/projects/{project.Id}/keys/{apiKey.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the key is revoked in the database
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var revokedKey = await dbContext.ApiKeys.FindAsync(apiKey.Id);
            Assert.NotNull(revokedKey);
            Assert.Equal(ApiKeyStatus.Revoked, revokedKey.Status);
        }

        [Fact(Skip = "ApiKeysController uses old Tenant/Project architecture - needs migration to Organization/Team")]
        public async Task RevokeKey_WithAuth_WrongTenant_ReturnsNotFound()
        {
            // Create first user
            var (client1, _) = await this.CreateAuthenticatedClientAsync("user1@example.com");

            // Create second user with their own tenant and project
            var (_, user2) = await this.CreateAuthenticatedClientAsync("user2@example.com");
            var projectForUser2 = await this.CreateTestProjectAsync(user2.OrganizationId);
            var apiKey = await this.CreateTestApiKeyAsync(projectForUser2.Id);

            // Try to revoke key in user2's project using user1's token
            var response = await client1.DeleteAsync($"/projects/{projectForUser2.Id}/keys/{apiKey.Id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact(Skip = "ApiKeysController uses old Tenant/Project architecture - needs migration to Organization/Team")]
        public async Task CreateKey_StoresCorrectDataInDatabase()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();
            var project = await this.CreateTestProjectAsync(user.OrganizationId);

            var request = new { Name = "Database Test Key" };
            var response = await client.PostAsJsonAsync($"/projects/{project.Id}/keys", request);

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var keyId = content.GetProperty("id").GetGuid();

            // Verify in database
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var storedKey = await dbContext.ApiKeys.FindAsync(keyId);

            Assert.NotNull(storedKey);
            Assert.Equal(project.Id, storedKey.ProjectId);
            Assert.Equal("Database Test Key", storedKey.Name);
            Assert.Equal(ApiKeyStatus.Active, storedKey.Status);
            Assert.NotNull(storedKey.KeyHash);
            Assert.NotEmpty(storedKey.KeyHash);
        }

        [Fact(Skip = "ApiKeysController uses old Tenant/Project architecture - needs migration to Organization/Team")]
        public async Task RevokeKey_CreatesAuditLog()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();
            var project = await this.CreateTestProjectAsync(user.OrganizationId);
            var apiKey = await this.CreateTestApiKeyAsync(project.Id);

            var response = await client.DeleteAsync($"/projects/{project.Id}/keys/{apiKey.Id}");

            response.EnsureSuccessStatusCode();

            // Verify audit log was created
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var auditLog = await dbContext.AuditLogs
                .Where(a => a.Action == "RevokeApiKey" && a.UserId == user.Id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            Assert.NotNull(auditLog);
            Assert.NotNull(auditLog.NewValues);
            Assert.Contains(apiKey.Id.ToString(), auditLog.NewValues, StringComparison.Ordinal);
        }

        [Fact(Skip = "ApiKeysController uses old Tenant/Project architecture - needs migration to Organization/Team")]
        public async Task CreateKey_CreatesAuditLog()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();
            var project = await this.CreateTestProjectAsync(user.OrganizationId);

            var request = new { Name = "Audit Test Key" };
            var response = await client.PostAsJsonAsync($"/projects/{project.Id}/keys", request);

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var keyId = content.GetProperty("id").GetGuid();

            // Verify audit log was created
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var auditLog = await dbContext.AuditLogs
                .Where(a => a.Action == "CreateApiKey" && a.UserId == user.Id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            Assert.NotNull(auditLog);
            Assert.NotNull(auditLog.NewValues);
            Assert.Contains(keyId.ToString(), auditLog.NewValues, StringComparison.Ordinal);
            Assert.Contains("Audit Test Key", auditLog.NewValues, StringComparison.Ordinal);
        }

        [Fact(Skip = "ApiKeysController uses old Tenant/Project architecture - needs migration to Organization/Team")]
        public async Task CreateKey_KeyHashIsValid()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();
            var project = await this.CreateTestProjectAsync(user.OrganizationId);

            var request = new { Name = "Hash Test Key" };
            var response = await client.PostAsJsonAsync($"/projects/{project.Id}/keys", request);

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var rawKey = content.GetProperty("key").GetString();
            var keyId = content.GetProperty("id").GetGuid();
            Assert.NotNull(rawKey);

            // Get the stored hash
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var storedKey = await dbContext.ApiKeys.FindAsync(keyId);

            Assert.NotNull(storedKey);

            // Verify the hash is valid using the ApiKeyService
            var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
            Assert.True(apiKeyService.ValidateKey(rawKey, storedKey.KeyHash));
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
            _ = Guid.Parse(jwtToken.Claims.First(c => string.Equals(c.Type, "organizationId", StringComparison.Ordinal)).Value);

            // Get the user from database
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Synaxis.Infrastructure.Data.SynaxisDbContext>();
            var user = await dbContext.Users.FindAsync(userId).ConfigureAwait(false);
            Assert.NotNull(user);

            // Create a new client with the authorization header
            var authenticatedClient = this._factory.CreateClient();
            authenticatedClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return (authenticatedClient, user);
        }

        private async Task<Project> CreateTestProjectAsync(Guid tenantId, string name = "Test Project")
        {
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

            // Ensure tenant exists in ControlPlaneDbContext
            var tenant = await dbContext.Tenants.FindAsync(tenantId).ConfigureAwait(false);
            if (tenant == null)
            {
                tenant = new Tenant
                {
                    Id = tenantId,
                    Name = "Test Tenant",
                    Region = TenantRegion.Us,
                    Status = TenantStatus.Active,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                dbContext.Tenants.Add(tenant);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            var project = new Project
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = name,
                Status = ProjectStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return project;
        }

        private async Task<ApiKey> CreateTestApiKeyAsync(Guid projectId, string name = "Test API Key")
        {
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

            var rawKey = apiKeyService.GenerateKey();
            var hash = apiKeyService.HashKey(rawKey);

            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = name,
                KeyHash = hash,
                Status = ApiKeyStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            dbContext.ApiKeys.Add(apiKey);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return apiKey;
        }

#pragma warning disable SA1124 // Do not use regions
        #endregion
#pragma warning restore SA1124 // Do not use regions
    }
}
