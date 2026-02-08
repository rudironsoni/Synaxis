// <copyright file="IdentityService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Services
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Tokens;
    using Synaxis.InferenceGateway.Application.Identity;
    using Synaxis.InferenceGateway.Application.Identity.Models;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;

    /// <summary>
    /// Implementation of the identity service for user management and authentication.
    /// </summary>
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<SynaxisUser> _userManager;
        private readonly SignInManager<SynaxisUser> _signInManager;
        private readonly SynaxisDbContext _context;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityService"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="signInManager">The sign in manager.</param>
        /// <param name="context">The database context.</param>
        /// <param name="configuration">The configuration.</param>
        public IdentityService(
            UserManager<SynaxisUser> userManager,
            SignInManager<SynaxisUser> signInManager,
            SynaxisDbContext context,
            IConfiguration configuration)
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._context = context;
            this._configuration = configuration;
        }

        /// <inheritdoc />
        public async Task<RegistrationResult> RegisterUserAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new RegistrationResult();

            // Check if user already exists
            var existingUser = await this._userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
            if (existingUser != null)
            {
                result.Errors.Add("User with this email already exists.");
                return result;
            }

            // Create user
            var user = new SynaxisUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Status = "PendingVerification",
                EmailConfirmed = false,
            };

            var createResult = await this._userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
            if (!createResult.Succeeded)
            {
                result.Errors = createResult.Errors.Select(e => e.Description).ToList();
                return result;
            }

            result.Success = true;
            result.UserId = user.Id;
            return result;
        }

        /// <inheritdoc />
        public async Task<RegistrationResult> RegisterOrganizationAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new RegistrationResult();

            if (string.IsNullOrWhiteSpace(request.OrganizationName))
            {
                result.Errors.Add("Organization name is required.");
                return result;
            }

            // Check if user already exists BEFORE starting transaction
            var existingUser = await this._userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
            if (existingUser != null)
            {
                result.Errors.Add("User with this email already exists.");
                return result;
            }

            // Begin transaction
            using var transaction = await this._context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Register user first
                var user = await this.RegisterUserForOrganizationAsync(request, result, cancellationToken).ConfigureAwait(false);
                if (user == null)
                {
                    return result;
                }

                // Create organization with unique slug
                var organization = await this.CreateOrganizationWithUniqueSlugAsync(request, user, cancellationToken).ConfigureAwait(false);

                // Create organization settings
                await this.CreateOrganizationSettingsAsync(organization.Id, cancellationToken).ConfigureAwait(false);

                // Create default group
                var defaultGroup = await this.CreateDefaultGroupAsync(organization.Id, user.Id, cancellationToken).ConfigureAwait(false);

                // Create memberships
                await this.CreateUserMembershipsAsync(user.Id, organization.Id, defaultGroup.Id, cancellationToken).ConfigureAwait(false);

                // Activate user
                await this.ActivateUserAsync(user).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                result.Success = true;
                result.UserId = user.Id;
                result.OrganizationId = organization.Id;
                return result;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<AuthenticationResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            var response = new AuthenticationResponse();

            var user = await this._userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
            if (user == null)
            {
                response.ErrorMessage = "Invalid email or password.";
                return response;
            }

            var signInResult = await this._signInManager.CheckPasswordSignInAsync(user, request.Password, false).ConfigureAwait(false);
            if (!signInResult.Succeeded)
            {
                response.ErrorMessage = "Invalid email or password.";
                return response;
            }

            // Get user's organizations
            var memberships = await this._context.UserOrganizationMemberships
                .Where(m => m.UserId == user.Id && m.Status == "Active")
                .Include(m => m.Organization)
                .Where(m => m.Organization.DeletedAt == null)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            if (!memberships.Any())
            {
                response.ErrorMessage = "User is not a member of any organization.";
                return response;
            }

            // Determine which organization to use
            var membership = request.OrganizationId.HasValue
                ? memberships.FirstOrDefault(m => m.OrganizationId == request.OrganizationId.Value)
                : memberships[0];

            if (membership == null)
            {
                response.ErrorMessage = "User is not a member of the specified organization.";
                return response;
            }

            // Generate tokens
            var accessToken = await this.GenerateAccessToken(user, membership).ConfigureAwait(false);
            var refreshToken = GenerateRefreshToken();

            response.Success = true;
            response.AccessToken = accessToken;
            response.RefreshToken = refreshToken;
            response.ExpiresAt = DateTime.UtcNow.AddMinutes(60);
            response.User = await this.MapToUserInfo(user, memberships).ConfigureAwait(false);

            return response;
        }

        /// <inheritdoc />
        public async Task<AuthenticationResponse> RefreshTokenAsync(
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            // NOTE: Implement refresh token validation and storage
            // For now, return an error
            return new AuthenticationResponse
            {
                Success = false,
                ErrorMessage = "Refresh token functionality not yet implemented.",
            };
        }

        /// <inheritdoc />
        public async Task<UserInfo?> GetUserInfoAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = await this._userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
            if (user == null)
            {
                return null;
            }

            var memberships = await this._context.UserOrganizationMemberships
                .Where(m => m.UserId == userId && m.Status == "Active")
                .Include(m => m.Organization)
                .Where(m => m.Organization.DeletedAt == null)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            return await this.MapToUserInfo(user, memberships).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> AssignUserToOrganizationAsync(
            Guid userId,
            Guid organizationId,
            string role,
            CancellationToken cancellationToken = default)
        {
            // Validate role
            var validRoles = new[] { "Owner", "Admin", "Member", "Guest" };
            if (!validRoles.Contains(role))
            {
                return false;
            }

            // Check if membership already exists
            var existingMembership = await this._context.UserOrganizationMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == organizationId, cancellationToken).ConfigureAwait(false);

            if (existingMembership != null)
            {
                return false;
            }

            // Get default group for organization
            var defaultGroup = await this._context.Groups
                .FirstOrDefaultAsync(g => g.OrganizationId == organizationId && g.IsDefaultGroup, cancellationToken).ConfigureAwait(false);

            var membership = new UserOrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrganizationId = organizationId,
                OrganizationRole = role,
                PrimaryGroupId = defaultGroup?.Id,
                Status = "Active",
            };

            this._context.UserOrganizationMemberships.Add(membership);

            // If default group exists, add user to it
            if (defaultGroup != null)
            {
                var groupMembership = new UserGroupMembership
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    GroupId = defaultGroup.Id,
                    GroupRole = "Member",
                    IsPrimary = true,
                    JoinedAt = DateTime.UtcNow,
                };

                this._context.UserGroupMemberships.Add(groupMembership);
            }

            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> AssignUserToGroupAsync(
            Guid userId,
            Guid groupId,
            string groupRole,
            CancellationToken cancellationToken = default)
        {
            // Validate role
            var validRoles = new[] { "Admin", "Member", "Viewer" };
            if (!validRoles.Contains(groupRole))
            {
                return false;
            }

            // Check if membership already exists
            var existingMembership = await this._context.UserGroupMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.GroupId == groupId, cancellationToken).ConfigureAwait(false);

            if (existingMembership != null)
            {
                return false;
            }

            var membership = new UserGroupMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GroupId = groupId,
                GroupRole = groupRole,
                IsPrimary = false,
                JoinedAt = DateTime.UtcNow,
            };

            this._context.UserGroupMemberships.Add(membership);
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Registers a user for organization registration.
        /// </summary>
        /// <param name="request">The registration request.</param>
        /// <param name="result">The registration result to populate on error.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created user or null if registration failed.</returns>
        private async Task<SynaxisUser?> RegisterUserForOrganizationAsync(
            RegisterRequest request,
            RegistrationResult result,
            CancellationToken cancellationToken)
        {
            var userResult = await this.RegisterUserAsync(request, cancellationToken).ConfigureAwait(false);
            if (!userResult.Success)
            {
                result.Errors = userResult.Errors;
                return null;
            }

            if (userResult.UserId == null)
            {
                result.Errors.Add("User registration succeeded but user ID was not returned.");
                return null;
            }

            var user = await this._userManager.FindByIdAsync(userResult.UserId.Value.ToString()).ConfigureAwait(false);
            if (user == null)
            {
                result.Errors.Add("User was created but could not be retrieved.");
                return null;
            }

            return user;
        }

        /// <summary>
        /// Creates an organization with a unique slug.
        /// </summary>
        /// <param name="request">The registration request.</param>
        /// <param name="user">The user creating the organization.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created organization.</returns>
        private async Task<Organization> CreateOrganizationWithUniqueSlugAsync(
            RegisterRequest request,
            SynaxisUser user,
            CancellationToken cancellationToken)
        {
            var slug = request.OrganizationSlug ?? GenerateSlug(request.OrganizationName!);
            slug = await this.EnsureUniqueSlugAsync(slug, cancellationToken).ConfigureAwait(false);

            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                LegalName = request.OrganizationName!,
                DisplayName = request.OrganizationName!,
                Slug = slug,
                Status = "Active",
                PlanTier = "Free",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Id,
            };

            this._context.Organizations.Add(organization);
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return organization;
        }

        /// <summary>
        /// Ensures a slug is unique by appending a counter if necessary.
        /// </summary>
        /// <param name="slug">The proposed slug.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A unique slug.</returns>
        private async Task<string> EnsureUniqueSlugAsync(string slug, CancellationToken cancellationToken)
        {
            var originalSlug = slug;
            var counter = 1;
            while (await this._context.Organizations.AnyAsync(o => o.Slug == slug, cancellationToken).ConfigureAwait(false))
            {
                slug = $"{originalSlug}-{counter}";
                counter++;
            }

            return slug;
        }

        /// <summary>
        /// Creates organization settings for a new organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task CreateOrganizationSettingsAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            var settings = new OrganizationSettings
            {
                OrganizationId = organizationId,
            };
            this._context.OrganizationSettings.Add(settings);
            return this._context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Creates the default group for a new organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="userId">The user ID creating the group.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created default group.</returns>
        private async Task<Group> CreateDefaultGroupAsync(
            Guid organizationId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var defaultGroup = new Group
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = "Default",
                Slug = "default",
                Status = "Active",
                IsDefaultGroup = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
            };

            this._context.Groups.Add(defaultGroup);
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return defaultGroup;
        }

        /// <summary>
        /// Creates user memberships for both organization and default group.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="defaultGroupId">The default group ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task CreateUserMembershipsAsync(
            Guid userId,
            Guid organizationId,
            Guid defaultGroupId,
            CancellationToken cancellationToken)
        {
            var membership = new UserOrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrganizationId = organizationId,
                OrganizationRole = "Owner",
                PrimaryGroupId = defaultGroupId,
                Status = "Active",
            };

            this._context.UserOrganizationMemberships.Add(membership);

            var groupMembership = new UserGroupMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GroupId = defaultGroupId,
                GroupRole = "Admin",
                IsPrimary = true,
                JoinedAt = DateTime.UtcNow,
            };

            this._context.UserGroupMemberships.Add(groupMembership);
            return this._context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Activates a user by setting status to Active and confirming email.
        /// </summary>
        /// <param name="user">The user to activate.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task ActivateUserAsync(SynaxisUser user)
        {
            user.Status = "Active";
            user.EmailConfirmed = true;
            return this._userManager.UpdateAsync(user);
        }

        /// <summary>
        /// Generates a JWT access token for the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="membership">The organization membership.</param>
        /// <returns>The JWT token string.</returns>
        private Task<string> GenerateAccessToken(
            SynaxisUser user,
            UserOrganizationMembership membership)
        {
            var claims = new List<Claim>
            {
                new (ClaimTypes.NameIdentifier, user.Id.ToString()),
                new (ClaimTypes.Email, user.Email!),
                new ("organizationId", membership.OrganizationId.ToString()),
                new ("organizationRole", membership.OrganizationRole),
            };

            var jwtSecret = this._configuration["Jwt:Secret"] ?? "your-secret-key-here-min-32-chars-long!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: this._configuration["Jwt:Issuer"] ?? "Synaxis",
                audience: this._configuration["Jwt:Audience"] ?? "Synaxis",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: credentials);

            return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
        }

        /// <summary>
        /// Generates a cryptographically secure refresh token.
        /// </summary>
        /// <returns>A base64-encoded refresh token.</returns>
        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// Maps a SynaxisUser and memberships to a UserInfo DTO.
        /// </summary>
        /// <param name="user">The user entity.</param>
        /// <param name="memberships">The user's organization memberships.</param>
        /// <returns>The user information DTO.</returns>
        private Task<UserInfo> MapToUserInfo(
            SynaxisUser user,
            List<UserOrganizationMembership> memberships)
        {
            var organizations = memberships.Select(m => new OrganizationInfo
            {
                Id = m.OrganizationId,
                DisplayName = m.Organization.DisplayName,
                Slug = m.Organization.Slug,
                Role = m.OrganizationRole,
            }).ToList();

            return Task.FromResult(new UserInfo
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CurrentOrganization = organizations.FirstOrDefault(),
                Organizations = organizations,
            });
        }

        /// <summary>
        /// Generates a URL-friendly slug from a name.
        /// </summary>
        /// <param name="name">The name to convert to a slug.</param>
        /// <returns>A lowercase, hyphenated slug.</returns>
        private static string GenerateSlug(string name)
        {
            return name.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-")
                .Trim('-');
        }
    }
}
