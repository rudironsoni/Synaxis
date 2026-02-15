// <copyright file="UsersControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.IntegrationTests.Controllers;

using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Xunit;
using Xunit.Abstractions;

[Trait("Category", "Integration")]
[Collection("Integration")]
public class UsersControllerTests : IClassFixture<SynaxisWebApplicationFactory>
{
    private readonly SynaxisWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _client;

    public UsersControllerTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _factory.OutputHelper = output;
        _output = output;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetMe_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithAuth_ReturnsUserProfile()
    {
        var (client, user) = await CreateAuthenticatedUserAsync();

        var response = await client.GetAsync("/api/v1/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("id").GetGuid().Should().Be(user.Id);
        content.GetProperty("email").GetString().Should().Be(user.Email);
        content.GetProperty("firstName").GetString().Should().Be(user.FirstName);
        content.GetProperty("lastName").GetString().Should().Be(user.LastName);
        content.GetProperty("timezone").GetString().Should().Be(user.Timezone);
        content.GetProperty("locale").GetString().Should().Be(user.Locale);
    }

    [Fact]
    public async Task UpdateMe_WithoutAuth_ReturnsUnauthorized()
    {
        var request = new { FirstName = "Updated" };
        var response = await _client.PutAsJsonAsync("/api/v1/users/me", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateMe_ValidRequest_UpdatesUserProfile()
    {
        var (client, user) = await CreateAuthenticatedUserAsync();

        var request = new
        {
            FirstName = "UpdatedFirst",
            LastName = "UpdatedLast",
            Timezone = "America/New_York",
            Locale = "en-GB"
        };

        var response = await client.PutAsJsonAsync("/api/v1/users/me", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("firstName").GetString().Should().Be("UpdatedFirst");
        content.GetProperty("lastName").GetString().Should().Be("UpdatedLast");
        content.GetProperty("timezone").GetString().Should().Be("America/New_York");
        content.GetProperty("locale").GetString().Should().Be("en-GB");

        var updatedUser = await GetUserByIdAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be("UpdatedFirst");
        updatedUser.LastName.Should().Be("UpdatedLast");
    }

    [Fact]
    public async Task DeleteMe_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("/api/v1/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteMe_ValidRequest_SoftDeletesUser()
    {
        var (client, user) = await CreateAuthenticatedUserAsync();

        var response = await client.DeleteAsync("/api/v1/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deletedUser = await GetUserByIdAsync(user.Id);
        deletedUser.Should().NotBeNull();
        deletedUser!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UploadAvatar_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/v1/users/me/avatar", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadAvatar_ValidRequest_ReturnsPlaceholder()
    {
        var (client, user) = await CreateAuthenticatedUserAsync();

        var response = await client.PostAsync("/api/v1/users/me/avatar", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("message").GetString().Should().Contain("placeholder");
    }

    [Fact]
    public async Task GetMyOrganizations_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/users/me/organizations");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyOrganizations_ValidRequest_ReturnsUserOrganizations()
    {
        var (client, user) = await CreateAuthenticatedUserAsync();
        var org1 = await CreateTestOrganizationAsync("Org 1");
        var org2 = await CreateTestOrganizationAsync("Org 2");
        await AddUserToOrganizationAsync(user.Id, org1.Id);
        await AddUserToOrganizationAsync(user.Id, org2.Id);

        var response = await client.GetAsync("/api/v1/users/me/organizations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var orgs = content.GetProperty("items").EnumerateArray();
        orgs.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetMyTeams_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/users/me/teams");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyTeams_ValidRequest_ReturnsUserTeams()
    {
        var (client, user) = await CreateAuthenticatedUserAsync();
        var org = await CreateTestOrganizationAsync("Test Org");
        var team1 = await CreateTestTeamAsync(org.Id, "Team 1");
        var team2 = await CreateTestTeamAsync(org.Id, "Team 2");
        await AddUserToTeamAsync(user.Id, team1.Id, org.Id);
        await AddUserToTeamAsync(user.Id, team2.Id, org.Id);

        var response = await client.GetAsync("/api/v1/users/me/teams");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var teams = content.GetProperty("items").EnumerateArray();
        teams.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task RequestDataExport_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/v1/users/me/data-export", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequestDataExport_ValidRequest_ReturnsAccepted()
    {
        var (client, user) = await CreateAuthenticatedUserAsync();

        var response = await client.PostAsync("/api/v1/users/me/data-export", null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("message").GetString().Should().Contain("export request received");
    }

    [Fact]
    public async Task UpdateCrossBorderConsent_WithoutAuth_ReturnsUnauthorized()
    {
        var request = new { ConsentGiven = true };
        var response = await _client.PutAsJsonAsync("/api/v1/users/me/cross-border-consent", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateCrossBorderConsent_ValidRequest_UpdatesConsent()
    {
        var (client, user) = await CreateAuthenticatedUserAsync();

        var request = new
        {
            ConsentGiven = true,
            ConsentVersion = "v1.0"
        };

        var response = await client.PutAsJsonAsync("/api/v1/users/me/cross-border-consent", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await GetUserByIdAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.CrossBorderConsentGiven.Should().BeTrue();
        updatedUser.CrossBorderConsentVersion.Should().Be("v1.0");
        updatedUser.CrossBorderConsentDate.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateCrossBorderConsent_MissingConsentGiven_ReturnsBadRequest()
    {
        var (client, user) = await CreateAuthenticatedUserAsync();

        var request = new { ConsentVersion = "v1.0" };

        var response = await client.PutAsJsonAsync("/api/v1/users/me/cross-border-consent", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<(HttpClient Client, User User)> CreateAuthenticatedUserAsync()
    {
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Slug = $"test-org-{Guid.NewGuid():N}"[..20],
            Name = "Test Organization",
            Description = "Test Description",
            PrimaryRegion = "us-east-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            Email = $"testuser-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hashed_password",
            DataResidencyRegion = "us-east-1",
            CreatedInRegion = "us-east-1",
            FirstName = "Test",
            LastName = "User",
            AvatarUrl = "https://example.com/avatar.png",
            Timezone = "UTC",
            Locale = "en-US",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Organizations.Add(org);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        var authenticatedClient = _factory.CreateClient();
        var token = GenerateTestToken(user.Id);
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return (authenticatedClient, user);
    }

    private Task<User?> GetUserByIdAsync(Guid userId)
    {
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
        return dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    private Task<Organization> CreateTestOrganizationAsync(string name)
    {
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Slug = $"{name.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal)}-{Guid.NewGuid():N}"[..20],
            Name = name,
            Description = "Test Organization",
            PrimaryRegion = "us-east-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Organizations.Add(org);
        return dbContext.SaveChangesAsync().ContinueWith(_ => org, TaskScheduler.Default);
    }

    private Task AddUserToOrganizationAsync(Guid userId, Guid organizationId)
    {
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

        var team = new Team
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Slug = $"team-{Guid.NewGuid():N}"[..20],
            Name = "Default Team",
            Description = "Default team",
            AllowedModels = new List<string> { "gpt-4", "gpt-3.5-turbo" },
            BlockedModels = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var membership = new TeamMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TeamId = team.Id,
            OrganizationId = organizationId,
            Role = "Member",
            JoinedAt = DateTime.UtcNow
        };

        dbContext.Teams.Add(team);
        dbContext.TeamMemberships.Add(membership);
        return dbContext.SaveChangesAsync();
    }

    private async Task<Team> CreateTestTeamAsync(Guid organizationId, string name)
    {
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

        var team = new Team
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Slug = $"{name.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal)}-{Guid.NewGuid():N}"[..20],
            Name = name,
            Description = "Test Team",
            AllowedModels = new List<string> { "gpt-4", "gpt-3.5-turbo" },
            BlockedModels = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
        return team;
    }

    private Task AddUserToTeamAsync(Guid userId, Guid teamId, Guid organizationId)
    {
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

        var membership = new TeamMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TeamId = teamId,
            OrganizationId = organizationId,
            Role = "Member",
            JoinedAt = DateTime.UtcNow
        };

        dbContext.TeamMemberships.Add(membership);
        return dbContext.SaveChangesAsync();
    }

    private string GenerateTestToken(Guid userId)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("TestJwtSecretKeyThatIsAtLeast32BytesLongForHmacSha256Algorithm"));

        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
                new System.Security.Claims.Claim("sub", userId.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key,
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };

        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }
}
