namespace Synaxis.InferenceGateway.Application.Identity.Models;

/// <summary>
/// Request model for user registration.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the user's password.
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the organization name (for new org registration).
    /// </summary>
    public string? OrganizationName { get; set; }

    /// <summary>
    /// Gets or sets the organization slug (for new org registration).
    /// </summary>
    public string? OrganizationSlug { get; set; }
}

/// <summary>
/// Request model for user login.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the user's password.
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// Gets or sets the optional organization ID to log into.
    /// </summary>
    public Guid? OrganizationId { get; set; }
}

/// <summary>
/// Response model for authentication operations.
/// </summary>
public class AuthenticationResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the JWT access token.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the token expiration time.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the user information.
    /// </summary>
    public UserInfo? User { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// User information response model.
/// </summary>
public class UserInfo
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user's email.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the user's current organization.
    /// </summary>
    public OrganizationInfo? CurrentOrganization { get; set; }

    /// <summary>
    /// Gets or sets the user's organizations.
    /// </summary>
    public IList<OrganizationInfo> Organizations { get; set; } = new List<OrganizationInfo>();
}

/// <summary>
/// Organization information response model.
/// </summary>
public class OrganizationInfo
{
    /// <summary>
    /// Gets or sets the organization ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the organization display name.
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the organization slug.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// Gets or sets the user's role in this organization.
    /// </summary>
    public string? Role { get; set; }
}

/// <summary>
/// Registration result.
/// </summary>
public class RegistrationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the created user ID.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the created organization ID (if applicable).
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the error messages if operation failed.
    /// </summary>
    public IList<string> Errors { get; set; } = new List<string>();
}
