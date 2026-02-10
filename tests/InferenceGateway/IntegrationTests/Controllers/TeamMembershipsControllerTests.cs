// <copyright file="TeamMembershipsControllerTests.cs" company="PlaceholderCompany">
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
    public class TeamMembershipsControllerTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;

        public TeamMembershipsControllerTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
        {
            _factory = factory;
            _factory.OutputHelper = output;
            _output = output;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task AddMember_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var request = new { userId = Guid.NewGuid(), role = "member" };

            var response = await _client.PostAsJsonAsync($"/api/v1/organizations/{orgId}/teams/{teamId}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AddMember_NotTeamAdmin_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var (_, user3) = await CreateAuthenticatedClientAsync("user3@example.com");
            var request = new { userId = user3.Id, role = "member" };

            var response = await client2.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AddMember_ValidRequest_ReturnsCreated()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (_, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");

            var request = new { userId = user2.Id, role = "member" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var membership = await GetUserGroupMembershipAsync(user2.Id, team.Id);
            membership.Should().NotBeNull();
            membership!.Role.Should().Be("Member");
        }

        [Fact]
        public async Task AddMember_AsOrgAdmin_ReturnsCreated()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");

            var (_, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");

            var request = new { userId = user2.Id, role = "admin" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var membership = await GetUserGroupMembershipAsync(user2.Id, team.Id);
            membership.Should().NotBeNull();
            membership!.Role.Should().Be("TeamAdmin");
        }

        [Fact]
        public async Task AddMember_DuplicateMember_ReturnsBadRequest()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (_, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var request = new { userId = user2.Id, role = "member" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AddMember_InvalidRole_ReturnsBadRequest()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (_, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");

            var request = new { userId = user2.Id, role = "owner" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AddMember_UserNotInOrganization_ReturnsBadRequest()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (_, user2) = await CreateAuthenticatedClientAsync("user2@example.com");

            var request = new { userId = user2.Id, role = "member" };
            var response = await client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RemoveMember_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var response = await _client.DeleteAsync($"/api/v1/organizations/{orgId}/teams/{teamId}/members/{userId}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RemoveMember_NotTeamAdmin_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var (_, user3) = await CreateAuthenticatedClientAsync("user3@example.com");
            await AddUserToOrganizationAsync(user3.Id, org.Id, "member");
            await AddUserToGroupAsync(user3.Id, team.Id, "member");

            var response = await client2.DeleteAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members/{user3.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task RemoveMember_ValidRequest_ReturnsNoContent()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (_, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var response = await client.DeleteAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members/{user2.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var membership = await GetUserGroupMembershipAsync(user2.Id, team.Id);
            membership.Should().BeNull();
        }

        [Fact]
        public async Task RemoveMember_RemoveSelf_ReturnsNoContent()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var response = await client2.DeleteAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members/{user2.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var membership = await GetUserGroupMembershipAsync(user2.Id, team.Id);
            membership.Should().BeNull();
        }

        [Fact]
        public async Task RemoveMember_MemberNotFound_ReturnsNotFound()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var nonExistentUserId = Guid.NewGuid();

            var response = await client.DeleteAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members/{nonExistentUserId}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateMemberRole_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new { role = "admin" };

            var response = await _client.PutAsJsonAsync($"/api/v1/organizations/{orgId}/teams/{teamId}/members/{userId}/role", request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateMemberRole_NotTeamAdmin_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var (_, user3) = await CreateAuthenticatedClientAsync("user3@example.com");
            await AddUserToOrganizationAsync(user3.Id, org.Id, "member");
            await AddUserToGroupAsync(user3.Id, team.Id, "member");

            var request = new { role = "admin" };
            var response = await client2.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members/{user3.Id}/role", request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UpdateMemberRole_ValidRequest_ReturnsOk()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (_, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var request = new { role = "admin" };
            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members/{user2.Id}/role", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var membership = await GetUserGroupMembershipAsync(user2.Id, team.Id);
            membership.Should().NotBeNull();
            membership!.Role.Should().Be("TeamAdmin");
        }

        [Fact]
        public async Task UpdateMemberRole_InvalidRole_ReturnsBadRequest()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (_, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var request = new { role = "superadmin" };
            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members/{user2.Id}/role", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateMemberRole_MemberNotFound_ReturnsNotFound()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var nonExistentUserId = Guid.NewGuid();
            var request = new { role = "admin" };

            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members/{nonExistentUserId}/role", request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ListMembers_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/v1/organizations/{orgId}/teams/{teamId}/members");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ListMembers_NotTeamMember_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");

            var response = await client2.GetAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ListMembers_ValidRequest_ReturnsMembersList()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (_, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var members = content.GetProperty("members").EnumerateArray().ToList();
            members.Should().HaveCountGreaterThanOrEqualTo(2);
            members.Should().Contain(m => m.GetProperty("userId").GetGuid() == user1.Id && m.GetProperty("role").GetString() == "TeamAdmin");
            members.Should().Contain(m => m.GetProperty("userId").GetGuid() == user2.Id && m.GetProperty("role").GetString() == "Member");
        }

        [Fact]
        public async Task ListMembers_SupportsPagination()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            for (var i = 2; i <= 6; i++)
            {
                var (_, user) = await CreateAuthenticatedClientAsync($"user{i}@example.com");
                await AddUserToOrganizationAsync(user.Id, org.Id, "Member");
                await AddUserToGroupAsync(user.Id, team.Id, "member");
            }

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/members?pageSize=2&page=1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var members = content.GetProperty("members").EnumerateArray().ToList();
            members.Should().HaveCount(2);
            content.GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(6);
        }

        // New /memberships endpoints tests
        [Fact]
        public async Task ListMemberships_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/v1/organizations/{orgId}/teams/{teamId}/memberships");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ListMemberships_NotTeamMember_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");

            var response = await client2.GetAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/memberships");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ListMemberships_ValidRequest_ReturnsMembershipsList()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (_, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/memberships");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var memberships = content.GetProperty("memberships").EnumerateArray().ToList();
            memberships.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task GetMembership_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var membershipId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/v1/organizations/{orgId}/teams/{teamId}/memberships/{membershipId}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMembership_NotTeamMember_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");

            var membership = await GetUserGroupMembershipAsync(user1.Id, team.Id);

            var response = await client2.GetAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/memberships/{membership!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetMembership_ValidRequest_ReturnsMembership()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var membership = await GetUserGroupMembershipAsync(user1.Id, team.Id);

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/memberships/{membership!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("id").GetGuid().Should().Be(membership.Id);
            content.GetProperty("userId").GetGuid().Should().Be(user1.Id);
        }

        [Fact]
        public async Task GetMembership_NotFound_ReturnsNotFound()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var nonExistentId = Guid.NewGuid();

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/memberships/{nonExistentId}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateMembership_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var membershipId = Guid.NewGuid();
            var request = new { role = "admin" };

            var response = await _client.PutAsJsonAsync($"/api/v1/organizations/{orgId}/teams/{teamId}/memberships/{membershipId}", request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateMembership_NotTeamAdmin_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var (_, user3) = await CreateAuthenticatedClientAsync("user3@example.com");
            await AddUserToOrganizationAsync(user3.Id, org.Id, "member");
            await AddUserToGroupAsync(user3.Id, team.Id, "member");

            var membership = await GetUserGroupMembershipAsync(user3.Id, team.Id);
            var request = new { role = "admin" };

            var response = await client2.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/memberships/{membership!.Id}", request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UpdateMembership_ValidRequest_ReturnsOk()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (_, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var membership = await GetUserGroupMembershipAsync(user2.Id, team.Id);
            var request = new { role = "admin" };

            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/memberships/{membership!.Id}", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedMembership = await GetUserGroupMembershipAsync(user2.Id, team.Id);
            updatedMembership!.Role.Should().Be("TeamAdmin");
        }

        [Fact]
        public async Task UpdateMembership_InvalidRole_ReturnsBadRequest()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (_, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var membership = await GetUserGroupMembershipAsync(user2.Id, team.Id);
            var request = new { role = "superadmin" };

            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/memberships/{membership!.Id}", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateMembership_NotFound_ReturnsNotFound()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var nonExistentId = Guid.NewGuid();
            var request = new { role = "admin" };

            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/memberships/{nonExistentId}", request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteMembership_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var membershipId = Guid.NewGuid();

            var response = await _client.DeleteAsync($"/api/v1/organizations/{orgId}/teams/{teamId}/memberships/{membershipId}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task DeleteMembership_NotTeamAdmin_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var (_, user3) = await CreateAuthenticatedClientAsync("user3@example.com");
            await AddUserToOrganizationAsync(user3.Id, org.Id, "member");
            await AddUserToGroupAsync(user3.Id, team.Id, "member");

            var membership = await GetUserGroupMembershipAsync(user3.Id, team.Id);

            var response = await client2.DeleteAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/memberships/{membership!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task DeleteMembership_ValidRequest_ReturnsNoContent()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (_, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var membership = await GetUserGroupMembershipAsync(user2.Id, team.Id);

            var response = await client.DeleteAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/memberships/{membership!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var deletedMembership = await GetUserGroupMembershipAsync(user2.Id, team.Id);
            deletedMembership.Should().BeNull();
        }

        [Fact]
        public async Task DeleteMembership_RemoveSelf_ReturnsNoContent()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org.Id, "member");
            await AddUserToGroupAsync(user2.Id, team.Id, "member");

            var membership = await GetUserGroupMembershipAsync(user2.Id, team.Id);

            var response = await client2.DeleteAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/memberships/{membership!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var deletedMembership = await GetUserGroupMembershipAsync(user2.Id, team.Id);
            deletedMembership.Should().BeNull();
        }

        [Fact]
        public async Task DeleteMembership_NotFound_ReturnsNotFound()
        {
            var (client, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org = await CreateTestOrganizationAsync(user1.Id);
            var team = await CreateTestGroupAsync(org.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team.Id, "admin");

            var nonExistentId = Guid.NewGuid();

            var response = await client.DeleteAsync($"/api/v1/organizations/{org.Id}/teams/{team.Id}/memberships/{nonExistentId}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

            // dev-login creates user in SynaxisDbContext, retrieve it
            // Retry logic to handle transaction commit timing issues
            User? synaxisUser = null;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                using var scope = _factory.Services.CreateScope();
                var synaxisDbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

                synaxisUser = await synaxisDbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false);

                if (synaxisUser != null)
                {
                    break;
                }

                // Wait: 50ms, 100ms, 200ms, 400ms, 800ms (total 1550ms max)
                await Task.Delay(50 * (1 << attempt)).ConfigureAwait(false);
            }

            synaxisUser.Should().NotBeNull($"User with ID {userId} should exist in SynaxisDbContext after dev-login for email {email}");

            var authenticatedClient = _factory.CreateClient();
            authenticatedClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return (authenticatedClient, synaxisUser);
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
                PrimaryRegion = "us-east-1",
                IsActive = true,
                Tier = "free",
                CreatedAt = DateTime.UtcNow,
            };

            dbContext.Organizations.Add(org);

            // Update the user's OrganizationId
            var user = await dbContext.Users.FindAsync(createdBy).ConfigureAwait(false);
            if (user != null)
            {
                user.OrganizationId = org.Id;
                user.Role = "owner"; // Creator is org owner
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

            // In the new SynaxisDbContext, users belong to an organization via User.OrganizationId
            var user = await dbContext.Users.FindAsync(userId).ConfigureAwait(false);
            if (user != null)
            {
                user.OrganizationId = organizationId;

                // Map common role names to the expected format
                user.Role = role.ToLowerInvariant() switch
                {
                    "admin" or "owner" => "owner",
                    "member" => "member",
                    _ => role.ToLowerInvariant()
                };
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        private Task AddUserToGroupAsync(Guid userId, Guid groupId, string role = "Member")
        {
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

            // Map common role names to database format
            var dbRole = role.ToLowerInvariant() switch
            {
                "admin" or "teamadmin" => "TeamAdmin",
                "member" => "Member",
                _ => role
            };

            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TeamId = groupId,
                OrganizationId = dbContext.Teams.Find(groupId)!.OrganizationId,
                Role = dbRole,
                JoinedAt = DateTime.UtcNow,
            };

            dbContext.TeamMemberships.Add(membership);
            return dbContext.SaveChangesAsync();
        }

        private Task<TeamMembership?> GetUserGroupMembershipAsync(Guid userId, Guid groupId)
        {
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

            return dbContext.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TeamId == groupId);
        }
    }
}
