// <copyright file="InvitationsControllerTests.cs" company="PlaceholderCompany">
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
    public class InvitationsControllerTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;

        public InvitationsControllerTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
        {
            _factory = factory;
            _factory.OutputHelper = output;
            _output = output;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CreateInvitation_WithoutAuth_ReturnsUnauthorized()
        {
            var request = new
            {
                OrganizationId = Guid.NewGuid(),
                TeamId = Guid.NewGuid(),
                Email = "test@example.com",
                Role = "member"
            };

            var response = await _client.PostAsJsonAsync("/api/v1/invitations", request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateInvitation_NotTeamAdmin_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org1 = await CreateTestOrganizationAsync(user1.Id);
            var team1 = await CreateTestGroupAsync(org1.Id, "Team Alpha");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org1.Id, "Member");
            await AddUserToGroupAsync(user2.Id, team1.Id, "Member");

            var request = new
            {
                OrganizationId = org1.Id,
                TeamId = team1.Id,
                Email = "newmember@example.com",
                Role = "member"
            };

            var response = await client2.PostAsJsonAsync("/api/v1/invitations", request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateInvitation_ValidRequest_CreatesInvitation()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Engineering Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var request = new
            {
                OrganizationId = org.Id,
                TeamId = team.Id,
                Email = "newmember@example.com",
                Role = "member"
            };

            var response = await client.PostAsJsonAsync("/api/v1/invitations", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("id").GetGuid().Should().NotBeEmpty();
            content.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
            content.GetProperty("email").GetString().Should().Be("newmember@example.com");
            content.GetProperty("role").GetString().Should().Be("Member");
            content.GetProperty("status").GetString().Should().Be("pending");

            var token = content.GetProperty("token").GetString();
            token!.Length.Should().BeGreaterThanOrEqualTo(64);
        }

        [Fact]
        public async Task CreateInvitation_DuplicateEmail_ReturnsBadRequest()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Engineering Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var request = new
            {
                OrganizationId = org.Id,
                TeamId = team.Id,
                Email = "duplicate@example.com",
                Role = "member"
            };

            await client.PostAsJsonAsync("/api/v1/invitations", request);
            var response = await client.PostAsJsonAsync("/api/v1/invitations", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateInvitation_InvalidEmail_ReturnsBadRequest()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Engineering Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var request = new
            {
                OrganizationId = org.Id,
                TeamId = team.Id,
                Email = "not-an-email",
                Role = "member"
            };

            var response = await client.PostAsJsonAsync("/api/v1/invitations", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetInvitationByToken_ValidToken_ReturnsInvitationDetails()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id, "Test Org");
            var team = await CreateTestGroupAsync(org.Id, "Test Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var createResponse = await client.PostAsJsonAsync("/api/v1/invitations", new
            {
                OrganizationId = org.Id,
                TeamId = team.Id,
                Email = "invited@example.com",
                Role = "member"
            });
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var token = createContent.GetProperty("token").GetString();

            var response = await _client.GetAsync($"/api/v1/invitations/{token}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("email").GetString().Should().Be("invited@example.com");
            content.GetProperty("organizationName").GetString().Should().Be("Test Org");
            content.GetProperty("teamName").GetString().Should().Be("Test Team");
            content.GetProperty("role").GetString().Should().Be("Member");
            content.GetProperty("status").GetString().Should().Be("pending");
        }

        [Fact]
        public async Task GetInvitationByToken_InvalidToken_ReturnsNotFound()
        {
            var response = await _client.GetAsync($"/api/v1/invitations/invalid-token-12345678");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetInvitationByToken_ExpiredToken_ReturnsNotFound()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Test Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var invitation = await CreateExpiredInvitationAsync(org.Id, team.Id, user.Id, "expired@example.com");

            var response = await _client.GetAsync($"/api/v1/invitations/{invitation.Token}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AcceptInvitation_ValidToken_CreatesTeamMembership()
        {
            var (adminClient, adminUser) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(adminUser.Id);
            var team = await CreateTestGroupAsync(org.Id, "Test Team");
            await AddUserToGroupAsync(adminUser.Id, team.Id, "TeamAdmin");

            var createResponse = await adminClient.PostAsJsonAsync("/api/v1/invitations", new
            {
                OrganizationId = org.Id,
                TeamId = team.Id,
                Email = "newuser@example.com",
                Role = "member"
            });
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var token = createContent.GetProperty("token").GetString();

            var (userClient, newUser) = await CreateAuthenticatedClientAsync("newuser@example.com");

            var response = await userClient.PostAsync($"/api/v1/invitations/{token}/accept", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("status").GetString().Should().Be("accepted");

            var membership = await GetUserGroupMembershipAsync(newUser.Id, team.Id);
            membership.Should().NotBeNull();
            membership!.Role.Should().Be("Member");
        }

        [Fact]
        public async Task AcceptInvitation_WithoutAuth_ReturnsUnauthorized()
        {
            var response = await _client.PostAsync("/api/v1/invitations/some-token/accept", null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AcceptInvitation_ExpiredToken_ReturnsBadRequest()
        {
            var (adminClient, adminUser) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(adminUser.Id);
            var team = await CreateTestGroupAsync(org.Id, "Test Team");

            var invitation = await CreateExpiredInvitationAsync(org.Id, team.Id, adminUser.Id, "expired@example.com");

            var (userClient, _) = await CreateAuthenticatedClientAsync("expired@example.com");

            var response = await userClient.PostAsync($"/api/v1/invitations/{invitation.Token}/accept", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AcceptInvitation_AlreadyAccepted_ReturnsBadRequest()
        {
            var (adminClient, adminUser) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(adminUser.Id);
            var team = await CreateTestGroupAsync(org.Id, "Test Team");
            await AddUserToGroupAsync(adminUser.Id, team.Id, "TeamAdmin");

            var createResponse = await adminClient.PostAsJsonAsync("/api/v1/invitations", new
            {
                OrganizationId = org.Id,
                TeamId = team.Id,
                Email = "user@example.com",
                Role = "member"
            });
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var token = createContent.GetProperty("token").GetString();

            var (userClient, _) = await CreateAuthenticatedClientAsync("user@example.com");

            await userClient.PostAsync($"/api/v1/invitations/{token}/accept", null);
            var response = await userClient.PostAsync($"/api/v1/invitations/{token}/accept", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task DeclineInvitation_ValidToken_UpdatesStatus()
        {
            var (adminClient, adminUser) = await CreateAuthenticatedClientAsync("admin@example.com");
            var org = await CreateTestOrganizationAsync(adminUser.Id);
            var team = await CreateTestGroupAsync(org.Id, "Test Team");
            await AddUserToGroupAsync(adminUser.Id, team.Id, "TeamAdmin");

            var createResponse = await adminClient.PostAsJsonAsync("/api/v1/invitations", new
            {
                OrganizationId = org.Id,
                TeamId = team.Id,
                Email = "decline@example.com",
                Role = "member"
            });
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var token = createContent.GetProperty("token").GetString();

            var (userClient, _) = await CreateAuthenticatedClientAsync("decline@example.com");

            var response = await userClient.PostAsync($"/api/v1/invitations/{token}/decline", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("status").GetString().Should().Be("declined");
        }

        [Fact]
        public async Task ListInvitations_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var response = await _client.GetAsync($"/api/v1/organizations/{orgId}/invitations");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ListInvitations_NotOrgMember_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org1 = await CreateTestOrganizationAsync(user1.Id);

            var (client2, _) = await CreateAuthenticatedClientAsync("user2@example.com");
            var response = await client2.GetAsync($"/api/v1/organizations/{org1.Id}/invitations");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ListInvitations_ValidRequest_ReturnsInvitationsList()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Test Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            await client.PostAsJsonAsync("/api/v1/invitations", new
            {
                OrganizationId = org.Id,
                TeamId = team.Id,
                Email = "user1@example.com",
                Role = "member"
            });
            await client.PostAsJsonAsync("/api/v1/invitations", new
            {
                OrganizationId = org.Id,
                TeamId = team.Id,
                Email = "user2@example.com",
                Role = "TeamAdmin"
            });

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/invitations");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var invitations = content.GetProperty("items").EnumerateArray().ToList();
            invitations.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task ListInvitations_SupportsPagination()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Test Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            for (var i = 1; i <= 5; i++)
            {
                await client.PostAsJsonAsync("/api/v1/invitations", new
                {
                    OrganizationId = org.Id,
                    TeamId = team.Id,
                    Email = $"user{i}@example.com",
                    Role = "member"
                });
            }

            var response = await client.GetAsync($"/api/v1/organizations/{org.Id}/invitations?pageSize=2&page=1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var invitations = content.GetProperty("items").EnumerateArray().ToList();
            invitations.Should().HaveCount(2);
        }

        [Fact]
        public async Task CancelInvitation_WithoutAuth_ReturnsUnauthorized()
        {
            var invitationId = Guid.NewGuid();
            var response = await _client.DeleteAsync($"/api/v1/invitations/{invitationId}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CancelInvitation_NotTeamAdmin_ReturnsForbidden()
        {
            var (_, user1) = await CreateAuthenticatedClientAsync("user1@example.com");
            var org1 = await CreateTestOrganizationAsync(user1.Id);
            var team1 = await CreateTestGroupAsync(org1.Id, "Team Alpha");
            await AddUserToGroupAsync(user1.Id, team1.Id, "TeamAdmin");

            var invitation = await CreateTestInvitationAsync(org1.Id, team1.Id, user1.Id, "invitee@example.com");

            var (client2, user2) = await CreateAuthenticatedClientAsync("user2@example.com");
            await AddUserToOrganizationAsync(user2.Id, org1.Id, "Member");
            await AddUserToGroupAsync(user2.Id, team1.Id, "Member");

            var response = await client2.DeleteAsync($"/api/v1/invitations/{invitation.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CancelInvitation_ValidRequest_CancelsInvitation()
        {
            var (client, user) = await CreateAuthenticatedClientAsync();
            var org = await CreateTestOrganizationAsync(user.Id);
            var team = await CreateTestGroupAsync(org.Id, "Test Team");
            await AddUserToGroupAsync(user.Id, team.Id, "TeamAdmin");

            var invitation = await CreateTestInvitationAsync(org.Id, team.Id, user.Id, "cancel@example.com");

            var response = await client.DeleteAsync($"/api/v1/invitations/{invitation.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var cancelledInvitation = await dbContext.Invitations.FindAsync(invitation.Id);
            cancelledInvitation.Should().NotBeNull();
            cancelledInvitation!.Status.Should().Be("cancelled");
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

            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TeamId = teamId,
                OrganizationId = team.OrganizationId,
                Role = role,
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

        private async Task<Invitation> CreateTestInvitationAsync(Guid organizationId, Guid teamId, Guid invitedBy, string email)
        {
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

            var invitation = new Invitation
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                TeamId = teamId,
                Email = email,
                Role = "member",
                Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                InvitedBy = invitedBy,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            dbContext.Invitations.Add(invitation);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return invitation;
        }

        private async Task<Invitation> CreateExpiredInvitationAsync(Guid organizationId, Guid teamId, Guid invitedBy, string email)
        {
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

            var invitation = new Invitation
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                TeamId = teamId,
                Email = email,
                Role = "member",
                Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                InvitedBy = invitedBy,
                Status = "pending",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ExpiresAt = DateTime.UtcNow.AddDays(-3)
            };

            dbContext.Invitations.Add(invitation);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return invitation;
        }
    }
}
