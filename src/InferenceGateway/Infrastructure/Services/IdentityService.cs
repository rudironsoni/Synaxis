using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Synaxis.InferenceGateway.Application.Identity;
using Synaxis.InferenceGateway.Application.Identity.Models;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;

namespace Synaxis.InferenceGateway.Infrastructure.Services;

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
    public IdentityService(
        UserManager<SynaxisUser> userManager,
        SignInManager<SynaxisUser> signInManager,
        SynaxisDbContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<RegistrationResult> RegisterUserAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new RegistrationResult();

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
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
            EmailConfirmed = false
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
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

        // Begin transaction
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Register user first
            var userResult = await RegisterUserAsync(request, cancellationToken);
            if (!userResult.Success)
            {
                return userResult;
            }

            if (userResult.UserId == null)
            {
                result.Errors.Add("User registration succeeded but user ID was not returned.");
                return result;
            }

            var user = await _userManager.FindByIdAsync(userResult.UserId.Value.ToString());
            if (user == null)
            {
                result.Errors.Add("User was created but could not be retrieved.");
                return result;
            }

            // Generate unique slug
            var slug = request.OrganizationSlug ?? 
                       GenerateSlug(request.OrganizationName);
            
            // Ensure slug is unique by appending a number if needed
            var originalSlug = slug;
            var counter = 1;
            while (await _context.Organizations.AnyAsync(o => o.Slug == slug, cancellationToken))
            {
                slug = $"{originalSlug}-{counter}";
                counter++;
            }

            // Create organization
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                LegalName = request.OrganizationName,
                DisplayName = request.OrganizationName,
                Slug = slug,
                Status = "Active",
                PlanTier = "Free",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Id
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync(cancellationToken);

            // Create organization settings
            var settings = new OrganizationSettings
            {
                OrganizationId = organization.Id
            };
            _context.OrganizationSettings.Add(settings);

            // Create default group
            var defaultGroup = new Group
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Name = "Default",
                Slug = "default",
                Status = "Active",
                IsDefaultGroup = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Id
            };

            _context.Groups.Add(defaultGroup);
            await _context.SaveChangesAsync(cancellationToken);

            // Create user-organization membership
            var membership = new UserOrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationId = organization.Id,
                OrganizationRole = "Owner",
                PrimaryGroupId = defaultGroup.Id,
                Status = "Active"
            };

            _context.UserOrganizationMemberships.Add(membership);

            // Create user-group membership
            var groupMembership = new UserGroupMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                GroupId = defaultGroup.Id,
                GroupRole = "Admin",
                IsPrimary = true,
                JoinedAt = DateTime.UtcNow
            };

            _context.UserGroupMemberships.Add(groupMembership);
            await _context.SaveChangesAsync(cancellationToken);

            // Activate user
            user.Status = "Active";
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            await transaction.CommitAsync(cancellationToken);

            result.Success = true;
            result.UserId = user.Id;
            result.OrganizationId = organization.Id;
            return result;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AuthenticationResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = new AuthenticationResponse();

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            response.ErrorMessage = "Invalid email or password.";
            return response;
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!signInResult.Succeeded)
        {
            response.ErrorMessage = "Invalid email or password.";
            return response;
        }

        // Get user's organizations
        var memberships = await _context.UserOrganizationMemberships
            .Where(m => m.UserId == user.Id && m.Status == "Active")
            .Include(m => m.Organization)
            .Where(m => m.Organization.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (!memberships.Any())
        {
            response.ErrorMessage = "User is not a member of any organization.";
            return response;
        }

        // Determine which organization to use
        var membership = request.OrganizationId.HasValue
            ? memberships.FirstOrDefault(m => m.OrganizationId == request.OrganizationId.Value)
            : memberships.First();

        if (membership == null)
        {
            response.ErrorMessage = "User is not a member of the specified organization.";
            return response;
        }

        // Generate tokens
        var accessToken = await GenerateAccessToken(user, membership, cancellationToken);
        var refreshToken = GenerateRefreshToken();

        response.Success = true;
        response.AccessToken = accessToken;
        response.RefreshToken = refreshToken;
        response.ExpiresAt = DateTime.UtcNow.AddMinutes(60);
        response.User = await MapToUserInfo(user, memberships, cancellationToken);

        return response;
    }

    /// <inheritdoc />
    public async Task<AuthenticationResponse> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement refresh token validation and storage
        // For now, return an error
        return new AuthenticationResponse
        {
            Success = false,
            ErrorMessage = "Refresh token functionality not yet implemented."
        };
    }

    /// <inheritdoc />
    public async Task<UserInfo?> GetUserInfoAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return null;
        }

        var memberships = await _context.UserOrganizationMemberships
            .Where(m => m.UserId == userId && m.Status == "Active")
            .Include(m => m.Organization)
            .Where(m => m.Organization.DeletedAt == null)
            .ToListAsync(cancellationToken);

        return await MapToUserInfo(user, memberships, cancellationToken);
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
        var existingMembership = await _context.UserOrganizationMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == organizationId, cancellationToken);

        if (existingMembership != null)
        {
            return false;
        }

        // Get default group for organization
        var defaultGroup = await _context.Groups
            .FirstOrDefaultAsync(g => g.OrganizationId == organizationId && g.IsDefaultGroup, cancellationToken);

        var membership = new UserOrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = organizationId,
            OrganizationRole = role,
            PrimaryGroupId = defaultGroup?.Id,
            Status = "Active"
        };

        _context.UserOrganizationMemberships.Add(membership);

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
                JoinedAt = DateTime.UtcNow
            };

            _context.UserGroupMemberships.Add(groupMembership);
        }

        await _context.SaveChangesAsync(cancellationToken);
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
        var existingMembership = await _context.UserGroupMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.GroupId == groupId, cancellationToken);

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
            JoinedAt = DateTime.UtcNow
        };

        _context.UserGroupMemberships.Add(membership);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<string> GenerateAccessToken(
        SynaxisUser user,
        UserOrganizationMembership membership,
        CancellationToken cancellationToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new("organizationId", membership.OrganizationId.ToString()),
            new("organizationRole", membership.OrganizationRole)
        };

        var jwtSecret = _configuration["Jwt:Secret"] ?? "your-secret-key-here-min-32-chars-long!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "Synaxis",
            audience: _configuration["Jwt:Audience"] ?? "Synaxis",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task<UserInfo> MapToUserInfo(
        SynaxisUser user,
        List<UserOrganizationMembership> memberships,
        CancellationToken cancellationToken)
    {
        var organizations = memberships.Select(m => new OrganizationInfo
        {
            Id = m.OrganizationId,
            DisplayName = m.Organization.DisplayName,
            Slug = m.Organization.Slug,
            Role = m.OrganizationRole
        }).ToList();

        return new UserInfo
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CurrentOrganization = organizations.FirstOrDefault(),
            Organizations = organizations
        };
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Trim('-');
    }
}
