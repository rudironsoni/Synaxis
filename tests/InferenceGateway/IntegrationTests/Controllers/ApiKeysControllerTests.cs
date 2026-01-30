using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Security;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests.Controllers;

public class ApiKeysControllerTests : IClassFixture<SynaxisWebApplicationFactory>
{
    private readonly SynaxisWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _client;

    public ApiKeysControllerTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _factory.OutputHelper = output;
        _output = output;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateKey_WithoutAuth_ReturnsUnauthorized()
    {
        var request = new { Name = "Test Key" };
        var projectId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/projects/{projectId}/keys", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateKey_WithAuth_InvalidProject_ReturnsNotFound()
    {
        var user = await CreateTestUserWithTenantAsync();
        var client = CreateAuthenticatedClient(user);
        var invalidProjectId = Guid.NewGuid();

        var request = new { Name = "Test Key" };
        var response = await client.PostAsJsonAsync($"/projects/{invalidProjectId}/keys", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateKey_WithAuth_ValidProject_ReturnsCreatedKey()
    {
        var user = await CreateTestUserWithTenantAsync();
        var project = await CreateTestProjectAsync(user.TenantId);
        var client = CreateAuthenticatedClient(user);

        var request = new { Name = "Production API Key" };
        var response = await client.PostAsJsonAsync($"/projects/{project.Id}/keys", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.ValueKind == JsonValueKind.Object);
        Assert.NotEqual(Guid.Empty, content.GetProperty("Id").GetGuid());
        var name = content.GetProperty("Name").GetString();
        Assert.NotNull(name);
        Assert.Equal("Production API Key", name);
        var key = content.GetProperty("Key").GetString();
        Assert.NotNull(key);
        Assert.StartsWith("sk-synaxis-", key);
    }

    [Fact]
    public async Task CreateKey_WithAuth_WrongTenant_ReturnsNotFound()
    {
        var user1 = await CreateTestUserWithTenantAsync("user1@example.com");
        var user2 = await CreateTestUserWithTenantAsync("user2@example.com");
        var projectForUser2 = await CreateTestProjectAsync(user2.TenantId);
        var client = CreateAuthenticatedClient(user1);

        var request = new { Name = "Test Key" };
        var response = await client.PostAsJsonAsync($"/projects/{projectForUser2.Id}/keys", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RevokeKey_WithoutAuth_ReturnsUnauthorized()
    {
        var projectId = Guid.NewGuid();
        var keyId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/projects/{projectId}/keys/{keyId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RevokeKey_WithAuth_InvalidKey_ReturnsNotFound()
    {
        var user = await CreateTestUserWithTenantAsync();
        var project = await CreateTestProjectAsync(user.TenantId);
        var client = CreateAuthenticatedClient(user);
        var invalidKeyId = Guid.NewGuid();

        var response = await client.DeleteAsync($"/projects/{project.Id}/keys/{invalidKeyId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RevokeKey_WithAuth_InvalidProject_ReturnsNotFound()
    {
        var user = await CreateTestUserWithTenantAsync();
        var project = await CreateTestProjectAsync(user.TenantId);
        var apiKey = await CreateTestApiKeyAsync(project.Id);
        var client = CreateAuthenticatedClient(user);
        var invalidProjectId = Guid.NewGuid();

        var response = await client.DeleteAsync($"/projects/{invalidProjectId}/keys/{apiKey.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RevokeKey_WithAuth_ValidKey_ReturnsNoContent()
    {
        var user = await CreateTestUserWithTenantAsync();
        var project = await CreateTestProjectAsync(user.TenantId);
        var apiKey = await CreateTestApiKeyAsync(project.Id);
        var client = CreateAuthenticatedClient(user);

        var response = await client.DeleteAsync($"/projects/{project.Id}/keys/{apiKey.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the key is revoked in the database
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
        var revokedKey = await dbContext.ApiKeys.FindAsync(apiKey.Id);
        Assert.NotNull(revokedKey);
        Assert.Equal(ApiKeyStatus.Revoked, revokedKey.Status);
    }

    [Fact]
    public async Task RevokeKey_WithAuth_WrongTenant_ReturnsNotFound()
    {
        var user1 = await CreateTestUserWithTenantAsync("user1@example.com");
        var user2 = await CreateTestUserWithTenantAsync("user2@example.com");
        var projectForUser2 = await CreateTestProjectAsync(user2.TenantId);
        var apiKey = await CreateTestApiKeyAsync(projectForUser2.Id);
        var client = CreateAuthenticatedClient(user1);

        var response = await client.DeleteAsync($"/projects/{projectForUser2.Id}/keys/{apiKey.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateKey_StoresCorrectDataInDatabase()
    {
        var user = await CreateTestUserWithTenantAsync();
        var project = await CreateTestProjectAsync(user.TenantId);
        var client = CreateAuthenticatedClient(user);

        var request = new { Name = "Database Test Key" };
        var response = await client.PostAsJsonAsync($"/projects/{project.Id}/keys", request);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var keyId = content.GetProperty("Id").GetGuid();

        // Verify in database
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
        var storedKey = await dbContext.ApiKeys.FindAsync(keyId);

        Assert.NotNull(storedKey);
        Assert.Equal(project.Id, storedKey.ProjectId);
        Assert.Equal("Database Test Key", storedKey.Name);
        Assert.Equal(ApiKeyStatus.Active, storedKey.Status);
        Assert.NotNull(storedKey.KeyHash);
        Assert.NotEmpty(storedKey.KeyHash);
    }

    [Fact]
    public async Task RevokeKey_CreatesAuditLog()
    {
        var user = await CreateTestUserWithTenantAsync();
        var project = await CreateTestProjectAsync(user.TenantId);
        var apiKey = await CreateTestApiKeyAsync(project.Id);
        var client = CreateAuthenticatedClient(user);

        var response = await client.DeleteAsync($"/projects/{project.Id}/keys/{apiKey.Id}");

        response.EnsureSuccessStatusCode();

        // Verify audit log was created
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
        var auditLog = await dbContext.AuditLogs
            .Where(a => a.Action == "RevokeApiKey" && a.UserId == user.Id)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        Assert.NotNull(auditLog);
        Assert.NotNull(auditLog.PayloadJson);
        Assert.Contains(apiKey.Id.ToString(), auditLog.PayloadJson);
    }

    [Fact]
    public async Task CreateKey_CreatesAuditLog()
    {
        var user = await CreateTestUserWithTenantAsync();
        var project = await CreateTestProjectAsync(user.TenantId);
        var client = CreateAuthenticatedClient(user);

        var request = new { Name = "Audit Test Key" };
        var response = await client.PostAsJsonAsync($"/projects/{project.Id}/keys", request);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var keyId = content.GetProperty("Id").GetGuid();

        // Verify audit log was created
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
        var auditLog = await dbContext.AuditLogs
            .Where(a => a.Action == "CreateApiKey" && a.UserId == user.Id)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        Assert.NotNull(auditLog);
        Assert.NotNull(auditLog.PayloadJson);
        Assert.Contains(keyId.ToString(), auditLog.PayloadJson);
        Assert.Contains(project.Id.ToString(), auditLog.PayloadJson);
    }

    [Fact]
    public async Task CreateKey_KeyHashIsValid()
    {
        var user = await CreateTestUserWithTenantAsync();
        var project = await CreateTestProjectAsync(user.TenantId);
        var client = CreateAuthenticatedClient(user);

        var request = new { Name = "Hash Test Key" };
        var response = await client.PostAsJsonAsync($"/projects/{project.Id}/keys", request);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var rawKey = content.GetProperty("Key").GetString();
        var keyId = content.GetProperty("Id").GetGuid();

        // Get the stored hash
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
        var storedKey = await dbContext.ApiKeys.FindAsync(keyId);

        Assert.NotNull(storedKey);

        // Verify the hash is valid using the ApiKeyService
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
        Assert.True(apiKeyService.ValidateKey(rawKey!, storedKey.KeyHash));
    }

    #region Helper Methods

    private async Task<User> CreateTestUserWithTenantAsync(string email = "test@example.com")
    {
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Region = TenantRegion.Us,
            Status = TenantStatus.Active
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = email,
            Role = UserRole.Developer,
            AuthProvider = "dev",
            ProviderUserId = email
        };

        dbContext.Tenants.Add(tenant);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }

    private async Task<Project> CreateTestProjectAsync(Guid tenantId, string name = "Test Project")
    {
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        return project;
    }

    private async Task<ApiKey> CreateTestApiKeyAsync(Guid projectId, string name = "Test API Key")
    {
        var scope = _factory.Services.CreateScope();
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
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.ApiKeys.Add(apiKey);
        await dbContext.SaveChangesAsync();

        return apiKey;
    }

    private HttpClient CreateAuthenticatedClient(User user)
    {
        var token = GenerateJwtToken(user);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private string GenerateJwtToken(User user)
    {
        // Get JWT secret from configuration - ensure minimum 32 bytes (256 bits) for HMAC-SHA256
        var scope = _factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var jwtSecret = configuration["Synaxis:InferenceGateway:JwtSecret"]
            ?? "SynaxisDefaultSecretKeyDoNotUseInProduction1234567890!";

        // Pad or truncate to ensure exactly 32 bytes (256 bits) minimum
        var keyBytes = Encoding.UTF8.GetBytes(jwtSecret);
        if (keyBytes.Length < 32)
        {
            // Pad with additional characters to reach 32 bytes
            var padded = jwtSecret + new string('X', 32 - keyBytes.Length);
            keyBytes = Encoding.UTF8.GetBytes(padded);
        }

        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString()),
            new Claim("tenantId", user.TenantId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "Synaxis",
            audience: "Synaxis",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion
}
