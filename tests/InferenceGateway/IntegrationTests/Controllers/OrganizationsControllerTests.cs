// <copyright file="OrganizationsControllerTests.cs" company="Synaxis">
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
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Integration tests for OrganizationsController.
    /// </summary>
    [Trait("Category", "Integration")]
    public class OrganizationsControllerTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;

        public OrganizationsControllerTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
        {
            this._factory = factory;
            this._factory.OutputHelper = output;
            this._output = output;
            this._client = this._factory.CreateClient();
        }

        [Fact]
        public async Task CreateOrganization_WithoutAuth_ReturnsUnauthorized()
        {
            var request = new { Name = "Test Org", Slug = $"test-org-{Guid.NewGuid():N}" };

            var response = await this._client.PostAsJsonAsync("/api/v1/organizations", request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrganization_WithValidData_ReturnsCreatedOrganization()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var uniqueSlug = $"my-new-org-{Guid.NewGuid():N}";
            var request = new
            {
                Name = "My New Organization",
                Slug = uniqueSlug,
                Description = "Test organization",
                PrimaryRegion = "us-east-1"
            };

            var response = await client.PostAsJsonAsync("/api/v1/organizations", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                this._output.WriteLine($"Error response: {errorContent}");
            }

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotEqual(Guid.Empty, content.GetProperty("id").GetGuid());
            Assert.Equal("My New Organization", content.GetProperty("name").GetString());
            Assert.Equal(uniqueSlug, content.GetProperty("slug").GetString());
            Assert.Equal("Test organization", content.GetProperty("description").GetString());
            Assert.Equal("us-east-1", content.GetProperty("primaryRegion").GetString());
            Assert.Equal("free", content.GetProperty("tier").GetString());
        }

        [Fact]
        public async Task CreateOrganization_SetsDefaultLimits()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var request = new
            {
                Name = "Test Limits Org",
                Slug = $"test-limits-org-{Guid.NewGuid():N}"
            };

            var response = await client.PostAsJsonAsync("/api/v1/organizations", request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = content.GetProperty("id").GetGuid();

            // Verify in database
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var org = await dbContext.Organizations.FindAsync(orgId);

            Assert.NotNull(org);
            Assert.Equal(30, org.DataRetentionDays);
            Assert.Equal("active", org.SubscriptionStatus);
            Assert.True(org.IsActive);
        }

        [Fact]
        public async Task CreateOrganization_SetsCreatorAsOrgAdmin()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var request = new
            {
                Name = "Test Admin Org",
                Slug = $"test-admin-org-{Guid.NewGuid():N}"
            };

            var response = await client.PostAsJsonAsync("/api/v1/organizations", request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = content.GetProperty("id").GetGuid();

            // Verify user role in database
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var membership = await dbContext.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == user.Id && m.OrganizationId == orgId && m.Role == "OrgAdmin");

            Assert.NotNull(membership);
        }

        [Fact]
        public async Task CreateOrganization_WithDuplicateSlug_ReturnsBadRequest()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            // Create first organization
            var uniqueSlug = $"duplicate-slug-{Guid.NewGuid():N}";
            var request1 = new { Name = "First Org", Slug = uniqueSlug };
            var response1 = await client.PostAsJsonAsync("/api/v1/organizations", request1);
            response1.EnsureSuccessStatusCode();

            // Try to create second organization with same slug
            var request2 = new { Name = "Second Org", Slug = uniqueSlug };
            var response2 = await client.PostAsJsonAsync("/api/v1/organizations", request2);

            Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
        }

        [Fact]
        public async Task CreateOrganization_WithInvalidSlugFormat_ReturnsBadRequest()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var request = new { Name = "Test Org", Slug = "Invalid_Slug!" };

            var response = await client.PostAsJsonAsync("/api/v1/organizations", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrganization_WithMissingName_ReturnsBadRequest()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var request = new { Slug = $"test-slug-{Guid.NewGuid():N}" };

            var response = await client.PostAsJsonAsync("/api/v1/organizations", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrganization_WithMissingSlug_ReturnsBadRequest()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var request = new { Name = "Test Org" };

            var response = await client.PostAsJsonAsync("/api/v1/organizations", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ListOrganizations_WithoutAuth_ReturnsUnauthorized()
        {
            var response = await this._client.GetAsync("/api/v1/organizations");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ListOrganizations_ReturnsUserOrganizations()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            // Create two organizations
            var slug1 = $"org-one-{Guid.NewGuid():N}";
            var slug2 = $"org-two-{Guid.NewGuid():N}";
            await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "Org One", Slug = slug1 });
            await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "Org Two", Slug = slug2 });

            var response = await client.GetAsync("/api/v1/organizations");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var items = content.GetProperty("items").EnumerateArray().ToList();

            Assert.True(items.Count >= 2);
            Assert.Contains(items, i => string.Equals(i.GetProperty("slug").GetString(), slug1, StringComparison.Ordinal));
            Assert.Contains(items, i => string.Equals(i.GetProperty("slug").GetString(), slug2, StringComparison.Ordinal));
        }

        [Fact]
        public async Task ListOrganizations_WithPagination_ReturnsPagedResults()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            // Create three organizations
            await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "Org A", Slug = $"org-a-{Guid.NewGuid():N}" });
            await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "Org B", Slug = $"org-b-{Guid.NewGuid():N}" });
            await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "Org C", Slug = $"org-c-{Guid.NewGuid():N}" });

            var response = await client.GetAsync("/api/v1/organizations?page=1&pageSize=2");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var items = content.GetProperty("items").EnumerateArray().ToList();
            var totalCount = content.GetProperty("totalCount").GetInt32();

            Assert.True(items.Count <= 2);
            Assert.True(totalCount >= 3);
        }

        [Fact]
        public async Task GetOrganization_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var response = await this._client.GetAsync($"/api/v1/organizations/{orgId}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetOrganization_AsMember_ReturnsOrganization()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var uniqueSlug = $"test-org-{Guid.NewGuid():N}";
            var createResponse = await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "Test Org", Slug = uniqueSlug });
            if (!createResponse.IsSuccessStatusCode)
            {
                var errorContent = await createResponse.Content.ReadAsStringAsync();
                this._output.WriteLine($"Create org failed: {createResponse.StatusCode} - {errorContent}");
            }

            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = createContent.GetProperty("id").GetGuid();

            var response = await client.GetAsync($"/api/v1/organizations/{orgId}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(orgId, content.GetProperty("id").GetGuid());
            Assert.Equal("Test Org", content.GetProperty("name").GetString());
            Assert.Equal(uniqueSlug, content.GetProperty("slug").GetString());
        }

        [Fact]
        public async Task GetOrganization_AsNonMember_ReturnsForbidden()
        {
            var (client1, user1) = await this.CreateAuthenticatedClientAsync("user1@example.com");
            var (client2, user2) = await this.CreateAuthenticatedClientAsync("user2@example.com");

            var createResponse = await client1.PostAsJsonAsync("/api/v1/organizations", new { Name = "Private Org", Slug = $"private-org-{Guid.NewGuid():N}" });
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = createContent.GetProperty("id").GetGuid();

            var response = await client2.GetAsync($"/api/v1/organizations/{orgId}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrganization_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var request = new { Name = "Updated Name" };
            var response = await this._client.PutAsJsonAsync($"/api/v1/organizations/{orgId}", request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrganization_AsOrgAdmin_UpdatesOrganization()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var createResponse = await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "Original Name", Slug = $"original-slug-{Guid.NewGuid():N}" });
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = createContent.GetProperty("id").GetGuid();

            var updateRequest = new { Name = "Updated Name", Slug = $"updated-slug-{Guid.NewGuid():N}" };
            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{orgId}", updateRequest);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Updated Name", content.GetProperty("name").GetString());
            Assert.Equal(updateRequest.Slug, content.GetProperty("slug").GetString());
        }

        [Fact]
        public async Task UpdateOrganization_AsNonAdmin_ReturnsForbidden()
        {
            var (adminClient, adminUser) = await this.CreateAuthenticatedClientAsync("admin@example.com");
            var (memberClient, memberUser) = await this.CreateAuthenticatedClientAsync("member@example.com");

            var uniqueSlug = $"test-org-{Guid.NewGuid():N}";
            var createResponse = await adminClient.PostAsJsonAsync("/api/v1/organizations", new { Name = "Test Org", Slug = uniqueSlug });
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = createContent.GetProperty("id").GetGuid();

            var updateRequest = new { Name = "Hacked Name" };
            var response = await memberClient.PutAsJsonAsync($"/api/v1/organizations/{orgId}", updateRequest);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrganization_WithDuplicateSlug_ReturnsBadRequest()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var slug1 = $"org-one-{Guid.NewGuid():N}";
            await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "Org One", Slug = slug1 });
            var createResponse = await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "Org Two", Slug = $"org-two-{Guid.NewGuid():N}" });
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = createContent.GetProperty("id").GetGuid();

            var updateRequest = new { Slug = slug1 };
            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{orgId}", updateRequest);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteOrganization_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var response = await this._client.DeleteAsync($"/api/v1/organizations/{orgId}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteOrganization_AsOrgAdmin_SoftDeletesOrganization()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var createResponse = await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "To Delete", Slug = $"to-delete-{Guid.NewGuid():N}" });
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = createContent.GetProperty("id").GetGuid();

            var response = await client.DeleteAsync($"/api/v1/organizations/{orgId}");
            response.EnsureSuccessStatusCode();

            // Verify soft delete in database
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var org = await dbContext.Organizations.FindAsync(orgId);
            Assert.NotNull(org);
            Assert.False(org.IsActive);
        }

        [Fact]
        public async Task DeleteOrganization_AsNonAdmin_ReturnsForbidden()
        {
            var (adminClient, adminUser) = await this.CreateAuthenticatedClientAsync("admin@example.com");
            var (memberClient, memberUser) = await this.CreateAuthenticatedClientAsync("member@example.com");

            var createResponse = await adminClient.PostAsJsonAsync("/api/v1/organizations", new { Name = "Protected Org", Slug = $"protected-org-{Guid.NewGuid():N}" });
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = createContent.GetProperty("id").GetGuid();

            var response = await memberClient.DeleteAsync($"/api/v1/organizations/{orgId}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetOrganizationLimits_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var response = await this._client.GetAsync($"/api/v1/organizations/{orgId}/limits");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetOrganizationLimits_AsOrgAdmin_ReturnsLimits()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var uniqueSlug = $"test-org-{Guid.NewGuid():N}";
            var createResponse = await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "Test Org", Slug = uniqueSlug });
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = createContent.GetProperty("id").GetGuid();

            var response = await client.GetAsync($"/api/v1/organizations/{orgId}/limits");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(content.TryGetProperty("maxTeams", out _));
            Assert.True(content.TryGetProperty("maxConcurrentRequests", out _));
        }

        [Fact]
        public async Task UpdateOrganizationLimits_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var request = new { MaxTeams = 10 };
            var response = await this._client.PutAsJsonAsync($"/api/v1/organizations/{orgId}/limits", request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrganizationLimits_AsOrgAdmin_UpdatesLimits()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var uniqueSlug = $"test-org-{Guid.NewGuid():N}";
            var createResponse = await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "Test Org", Slug = uniqueSlug });
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = createContent.GetProperty("id").GetGuid();

            var updateRequest = new { MaxTeams = 50, MaxConcurrentRequests = 200, MonthlyRequestLimit = 1000000L };
            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{orgId}/limits", updateRequest);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(50, content.GetProperty("maxTeams").GetInt32());
            Assert.Equal(200, content.GetProperty("maxConcurrentRequests").GetInt32());
            Assert.Equal(1000000L, content.GetProperty("monthlyRequestLimit").GetInt64());
        }

        [Fact]
        public async Task UpdateOrganizationLimits_WithNegativeValues_ReturnsBadRequest()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var uniqueSlug = $"test-org-{Guid.NewGuid():N}";
            var createResponse = await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "Test Org", Slug = uniqueSlug });
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = createContent.GetProperty("id").GetGuid();

            var updateRequest = new { MaxTeams = -5 };
            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{orgId}/limits", updateRequest);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetOrganizationSettings_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var response = await this._client.GetAsync($"/api/v1/organizations/{orgId}/settings");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetOrganizationSettings_AsMember_ReturnsSettings()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var uniqueSlug = $"test-org-{Guid.NewGuid():N}";
            var createResponse = await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "Test Org", Slug = uniqueSlug });
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = createContent.GetProperty("id").GetGuid();

            var response = await client.GetAsync($"/api/v1/organizations/{orgId}/settings");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(content.TryGetProperty("tier", out _));
            Assert.True(content.TryGetProperty("dataRetentionDays", out _));
            Assert.True(content.TryGetProperty("requireSso", out _));
        }

        [Fact]
        public async Task UpdateOrganizationSettings_WithoutAuth_ReturnsUnauthorized()
        {
            var orgId = Guid.NewGuid();
            var request = new { DataRetentionDays = 60 };
            var response = await this._client.PutAsJsonAsync($"/api/v1/organizations/{orgId}/settings", request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrganizationSettings_AsOrgAdmin_UpdatesSettings()
        {
            var (client, user) = await this.CreateAuthenticatedClientAsync();

            var uniqueSlug = $"test-org-{Guid.NewGuid():N}";
            var createResponse = await client.PostAsJsonAsync("/api/v1/organizations", new { Name = "Test Org", Slug = uniqueSlug });
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = createContent.GetProperty("id").GetGuid();

            var updateRequest = new { DataRetentionDays = 90, RequireSso = true, AllowedEmailDomains = new[] { "example.com", "test.com" } };
            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{orgId}/settings", updateRequest);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(90, content.GetProperty("dataRetentionDays").GetInt32());
            Assert.True(content.GetProperty("requireSso").GetBoolean());
        }

        [Fact]
        public async Task UpdateOrganizationSettings_AsNonAdmin_ReturnsForbidden()
        {
            var (adminClient, adminUser) = await this.CreateAuthenticatedClientAsync("admin@example.com");
            var (memberClient, memberUser) = await this.CreateAuthenticatedClientAsync("member@example.com");

            var uniqueSlug = $"test-org-{Guid.NewGuid():N}";
            var createResponse = await adminClient.PostAsJsonAsync("/api/v1/organizations", new { Name = "Test Org", Slug = uniqueSlug });
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orgId = createContent.GetProperty("id").GetGuid();

            var updateRequest = new { DataRetentionDays = 90 };
            var response = await memberClient.PutAsJsonAsync($"/api/v1/organizations/{orgId}/settings", updateRequest);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

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
    }
}
