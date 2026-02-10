// <copyright file="TeamsControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.IntegrationTests.Controllers
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Xunit.Abstractions;

    [Trait("Category", "Integration")]
    [Collection("Integration")]
    public class TeamsControllerTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;

        public TeamsControllerTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
        {
            _factory = factory;
            _factory.OutputHelper = output;
            _output = output;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CreateTeam_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var request = new { Name = "Engineering Team" };

            var response = await _client.PostAsJsonAsync($"/api/v1/organizations/{orgId}/teams", request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateTeam_InvalidOrganization_ReturnsNotFound()
        {
            var (client, _) = await CreateAuthenticatedClientAsync();
            var invalidOrgId = Guid.NewGuid();
            var request = new { Name = "Engineering Team" };

            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{invalidOrgId}/teams", request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateTeam_NotOrganizationMember_ReturnsForbidden()
        {
            // User 1 creates their organization
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org1 = await CreateTestOrganizationAsync(user1.Id);

            // User 2 tries to create team in User 1's organization
            var (client2, _) = await CreateAuthenticatedClientAsync("user2@example.com");
            var request = new { Name = "Engineering Team" };

            var response = await client2.PostAsJsonAsync($"/api/v1/organizations/{org1.Id}/teams", request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateTeam_ValidRequest_CreatesTeamAndReturnsDetails()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);

            var request = new { Name = "Engineering Team", Description = "Core engineering team" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("id").GetGuid().Should().NotBeEmpty();
            content.GetProperty("name").GetString().Should().Be("Engineering Team");
            content.GetProperty("description").GetString().Should().Be("Core engineering team");

            // Verify creator is added as TeamAdmin
            var teamId = content.GetProperty("id").GetGuid();
            var membership = await GetUserGroupMembershipAsync(user.Id, teamId);
            membership.Should().NotBeNull();
            membership!.Role.Should().Be("TeamAdmin");
        }

        [Fact]
        public async Task CreateTeam_DuplicateName_ReturnsBadRequest()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            await CreateTestGroupAsync(org.Id, "Engineering Team");

            var request = new { Name = "Engineering Team" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateTeam_CreatesAuditLog()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);

            var request = new { Name = "Engineering Team" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams", request);

            response.EnsureSuccessStatusCode();

            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var auditLog = await dbContext.AuditLogs
                .Where(a => a.Action == "CreateTeam" && a.UserId == user.Id)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            auditLog.Should().NotBeNull();
        }

        [Fact]
        public async Task ListTeams_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var response = await _client.GetAsync($"/api/v1/organizations/{orgId}/teams");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ListTeams_NotOrganizationMember_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org1 = await CreateTestOrganizationAsync(user1.Id);

            var (client2, _) = await CreateAuthenticatedClientAsync("user2@example.com");
            var response = await client2.GetAsync($"/api/v1/organizations/{org1.Id}/teams");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ListTeams_ValidRequest_ReturnsTeamsList()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            await CreateTestGroupAsync(org.Id, "Team Alpha");
            await CreateTestGroupAsync(org.Id, "Team Beta");

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/teams");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var teams = content.GetProperty("teams").EnumerateArray().ToList();
            teams.Count.Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task ListTeams_SupportsPagination()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            for (var i = 1; i <= 5; i++)
            {
                await CreateTestGroupAsync(org.Id, $"Team {i}");
            }

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/teams?pageSize=2&page=1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var teams = content.GetProperty("teams").EnumerateArray().ToList();
            teams.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTeam_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var response = await _client.GetAsync($"/api/v1/organizations/{orgId}/teams/{teamId}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetTeam_NotTeamMember_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org1 = await CreateTestOrganizationAsync(user1.Id);
            var team1 = await CreateTestGroupAsync(org1.Id, "Team Alpha");

            var (client2, _) = await CreateAuthenticatedClientAsync("user2@example.com");
            var response = await client2.GetAsync($"/api/v1/organizations/{org1.Id}/teams/{team1.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetTeam_ValidRequest_ReturnsTeamDetails()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Engineering Team");
            await AddUserToGroupAsync(user.Id, team.Id, "Member");

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("id").GetGuid().Should().Be(team.Id);
            content.GetProperty("name").GetString().Should().Be("Engineering Team");
            content.GetProperty("memberCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task UpdateTeam_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var request = new { Name = "Updated Team" };

            var response = await _client.PutAsJsonAsync($"/api/v1/organizations/{orgId}/teams/{teamId}", request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateTeam_NotTeamAdmin_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org1 = await CreateTestOrganizationAsync(user1.Id);
            var team1 = await CreateTestGroupAsync(org1.Id, "Team Alpha");

            // User2 is a regular member, not admin
            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org1.Id, "Member");
            await AddUserToGroupAsync(user2.Id, team1.Id, "Member");

            var request = new { Name = "Updated Team" };
            var response = await client2.PutAsJsonAsync($"/api/v1/organizations/{org1.Id}/teams/{team1.Id}", request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UpdateTeam_ValidRequest_UpdatesTeam()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Old Name");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var request = new { Name = "New Name", Description = "Updated description" };
            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("name").GetString().Should().Be("New Name");
            content.GetProperty("description").GetString().Should().Be("Updated description");
        }

        [Fact]
        public async Task UpdateTeam_CreatesAuditLog()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Old Name");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var request = new { Name = "New Name" };
            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}", request);

            response.EnsureSuccessStatusCode();

            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var auditLog = await dbContext.AuditLogs
                .Where(a => a.Action == "UpdateTeam" && a.UserId == user.Id)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            auditLog.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteTeam_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            var response = await _client.DeleteAsync($"/api/v1/organizations/{orgId}/teams/{teamId}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task DeleteTeam_NotTeamAdmin_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org1 = await CreateTestOrganizationAsync(user1.Id);
            var team1 = await CreateTestGroupAsync(org1.Id, "Team Alpha");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org1.Id, "Member");
            await AddUserToGroupAsync(user2.Id, team1.Id, "Member");

            var response = await client2.DeleteAsync($"/api/v1/organizations/{org1.Id}/teams/{team1.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task DeleteTeam_ValidRequest_SoftDeletesTeam()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var response = await client.DeleteAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify soft delete
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var deletedTeam = await dbContext.Teams.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == team.Id);
            deletedTeam.Should().NotBeNull();
            deletedTeam!.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteTeam_CreatesAuditLog()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var response = await client.DeleteAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}");

            response.EnsureSuccessStatusCode();

            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var auditLog = await dbContext.AuditLogs
                .Where(a => a.Action == "DeleteTeam" && a.UserId == user.Id)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            auditLog.Should().NotBeNull();
        }

        [Fact]
        public async Task AddMember_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var request = new { UserId = Guid.NewGuid(), Role = "member" };

            var response = await _client.PostAsJsonAsync($"/api/v1/organizations/{orgId}/teams/{teamId}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AddMember_NotTeamAdmin_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org1 = await CreateTestOrganizationAsync(user1.Id);
            var team1 = await CreateTestGroupAsync(org1.Id, "Team Alpha");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org1.Id, "Member");
            await AddUserToGroupAsync(user2.Id, team1.Id, "Member");

            var (_, user3) = await CreateAuthenticatedClientAsync("user3@example.com");
            await AddUserToOrganizationAsync(user3.Id, org1.Id, "Member");

            var request = new { UserId = user3.Id, Role = "member" };
            var response = await client2.PostAsJsonAsync($"/api/v1/organizations/{org1.Id}/teams/{team1.Id}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AddMember_ValidRequest_ReturnsCreated()
        {
            var (client, user) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Engineering Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var (_, newUser) = await CreateAuthenticatedClientAsync("newmember@example.com");
            await AddUserToOrganizationAsync(newUser.Id, org.Id, "Member");

            var request = new { UserId = newUser.Id, Role = "member" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var membership = await GetUserGroupMembershipAsync(newUser.Id, team.Id);
            membership.Should().NotBeNull();
            membership!.Role.Should().Be("Member");
        }

        [Fact]
        public async Task AddMember_AsOrgAdmin_ReturnsCreated()
        {
            var (client, user) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Engineering Team");

            var (_, newUser) = await CreateAuthenticatedClientAsync("newmember@example.com");
            await AddUserToOrganizationAsync(newUser.Id, org.Id, "Member");

            var request = new { UserId = newUser.Id, Role = "admin" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var membership = await GetUserGroupMembershipAsync(newUser.Id, team.Id);
            membership.Should().NotBeNull();
            membership!.Role.Should().Be("TeamAdmin");
        }

        [Fact]
        public async Task AddMember_DuplicateMember_ReturnsBadRequest()
        {
            var (client, user) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Engineering Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var (_, existingMember) = await CreateAuthenticatedClientAsync("existing@example.com");
            await AddUserToOrganizationAsync(existingMember.Id, org.Id, "Member");
            await AddUserToGroupAsync(existingMember.Id, team.Id, "Member");

            var request = new { UserId = existingMember.Id, Role = "member" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AddMember_InvalidRole_ReturnsBadRequest()
        {
            var (client, user) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Engineering Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var (_, newUser) = await CreateAuthenticatedClientAsync("newmember@example.com");
            await AddUserToOrganizationAsync(newUser.Id, org.Id, "Member");

            var request = new { UserId = newUser.Id, Role = "invalidrole" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AddMember_UserNotInOrganization_ReturnsBadRequest()
        {
            var (client, user) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Engineering Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var (_, otherOrgUser) = await CreateAuthenticatedClientAsync("other@example.com");
            var otherOrg = await CreateTestOrganizationAsync(otherOrgUser.Id);

            var request = new { UserId = otherOrgUser.Id, Role = "member" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AddMember_TeamNotFound_ReturnsNotFound()
        {
            var (client, user) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(user.Id);

            var (_, newUser) = await CreateAuthenticatedClientAsync("newmember@example.com");
            await AddUserToOrganizationAsync(newUser.Id, org.Id, "Member");

            var invalidTeamId = Guid.NewGuid();
            var request = new { UserId = newUser.Id, Role = "member" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{invalidTeamId}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AddMember_CreatesAuditLog()
        {
            var (client, user) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Engineering Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var (_, newUser) = await CreateAuthenticatedClientAsync("newmember@example.com");
            await AddUserToOrganizationAsync(newUser.Id, org.Id, "Member");

            var request = new { UserId = newUser.Id, Role = "member" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members", request);

            response.EnsureSuccessStatusCode();

            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var auditLog = await dbContext.AuditLogs
                .Where(a => a.Action == "AddTeamMember" && a.UserId == user.Id)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            auditLog.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateMemberRole_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var request = new { Role = "admin" };

            var response = await _client.PutAsJsonAsync($"/api/v1/organizations/{orgId}/teams/{teamId}/members/{memberId}/role", request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateMemberRole_NotTeamAdmin_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org1 = await CreateTestOrganizationAsync(user1.Id);
            var team1 = await CreateTestGroupAsync(org1.Id, "Team Alpha");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org1.Id, "Member");
            await AddUserToGroupAsync(user2.Id, team1.Id, "Member");

            var (_, user3) = await CreateAuthenticatedClientAsync("user3@example.com");
            await AddUserToOrganizationAsync(user3.Id, org1.Id, "Member");
            await AddUserToGroupAsync(user3.Id, team1.Id, "Member");

            var request = new { Role = "admin" };
            var response = await client2.PutAsJsonAsync($"/api/v1/organizations/{org1.Id}/teams/{team1.Id}/members/{user3.Id}/role", request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UpdateMemberRole_ValidRequest_ReturnsOk()
        {
            var (client, user) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Engineering Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var (_, member) = await CreateAuthenticatedClientAsync("member@example.com");
            await AddUserToOrganizationAsync(member.Id, org.Id, "Member");
            await AddUserToGroupAsync(member.Id, team.Id, "Member");

            var request = new { Role = "admin" };
            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members/{member.Id}/role", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("role").GetString().Should().Be("TeamAdmin");

            var membership = await GetUserGroupMembershipAsync(member.Id, team.Id);
            membership!.Role.Should().Be("TeamAdmin");
        }

        [Fact]
        public async Task UpdateMemberRole_InvalidRole_ReturnsBadRequest()
        {
            var (client, user) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Engineering Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var (_, member) = await CreateAuthenticatedClientAsync("member@example.com");
            await AddUserToOrganizationAsync(member.Id, org.Id, "Member");
            await AddUserToGroupAsync(member.Id, team.Id, "Member");

            var request = new { Role = "invalidrole" };
            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members/{member.Id}/role", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateMemberRole_MemberNotFound_ReturnsNotFound()
        {
            var (client, user) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Engineering Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var nonExistentMemberId = Guid.NewGuid();
            var request = new { Role = "admin" };
            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members/{nonExistentMemberId}/role", request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateMemberRole_TeamNotFound_ReturnsNotFound()
        {
            var (client, user) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(user.Id);

            var invalidTeamId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var request = new { Role = "admin" };
            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{invalidTeamId}/members/{memberId}/role", request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateMemberRole_CreatesAuditLog()
        {
            var (client, user) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Engineering Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var (_, member) = await CreateAuthenticatedClientAsync("member@example.com");
            await AddUserToOrganizationAsync(member.Id, org.Id, "Member");
            await AddUserToGroupAsync(member.Id, team.Id, "Member");

            var request = new { Role = "admin" };
            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members/{member.Id}/role", request);

            response.EnsureSuccessStatusCode();

            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var auditLog = await dbContext.AuditLogs
                .Where(a => a.Action == "UpdateTeamMemberRole" && a.UserId == user.Id)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            auditLog.Should().NotBeNull();
        }

        private async Task<(HttpClient Client, User User)> CreateAuthenticatedClientAsync(string email = "test@example.com")
        {
            var loginRequest = new { Email = email };
            var response = await _client.PostAsJsonAsync("/auth/dev-login", loginRequest).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
            var token = content.GetProperty("token").GetString();
            token.Should().NotBeNull();

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var userId = Guid.Parse(jwtToken.Claims.First(c => string.Equals(c.Type, JwtRegisteredClaimNames.Sub, StringComparison.Ordinal)).Value);

            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var user = await dbContext.Users.FindAsync(userId).ConfigureAwait(false);
            user.Should().NotBeNull();

            var authenticatedClient = _factory.CreateClient();
            authenticatedClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return (authenticatedClient, user!);
        }

        private async Task<Organization> CreateTestOrganizationAsync(Guid createdBy, string name = "Test Organization")
        {
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = Guid.NewGuid().ToString("N")[..8],
                Tier = "free",
                PrimaryRegion = "us-east-1",
                CreatedAt = DateTime.UtcNow,
            };

            dbContext.Organizations.Add(org);

            var user = await dbContext.Users.FindAsync(createdBy).ConfigureAwait(false);
            if (user != null)
            {
                user.OrganizationId = org.Id;
                user.Role = "owner";
            }

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return org;
        }

        private async Task<Team> CreateTestGroupAsync(Guid organizationId, string name = "Test Team")
        {
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = name,
                Slug = Guid.NewGuid().ToString("N")[..8],
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            dbContext.Teams.Add(team);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return team;
        }

        private async Task AddUserToOrganizationAsync(Guid userId, Guid organizationId, string role = "Member")
        {
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

            var user = await dbContext.Users.FindAsync(userId).ConfigureAwait(false);
            if (user != null)
            {
                user.OrganizationId = organizationId;
                user.Role = role.ToLowerInvariant();
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        private async Task AddUserToGroupAsync(Guid userId, Guid teamId, string role = "Member")
        {
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

            var team = await dbContext.Teams.FindAsync(teamId).ConfigureAwait(false);
            if (team == null)
            {
                return;
            }

            var normalizedRole = role.Trim().ToLowerInvariant();
            var validRole = normalizedRole switch
            {
                "admin" or "teamadmin" => "TeamAdmin",
                "orgadmin" => "OrgAdmin",
                "member" => "Member",
                "viewer" => "Viewer",
                _ => "Member",
            };

            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TeamId = teamId,
                OrganizationId = team.OrganizationId,
                Role = validRole,
                JoinedAt = DateTime.UtcNow,
            };

            dbContext.TeamMemberships.Add(membership);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        private Task<TeamMembership?> GetUserGroupMembershipAsync(Guid userId, Guid teamId)
        {
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

            return dbContext.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TeamId == teamId);
        }
    }
}
