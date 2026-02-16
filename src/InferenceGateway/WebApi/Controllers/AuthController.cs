// <copyright file="AuthController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Models;
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Controller for authentication operations.
    /// </summary>
    [ApiController]
    [Route("auth")]
    [EnableCors("WebApp")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtService jwtService;
        private readonly IPasswordHasher passwordHasher;
        private readonly SynaxisDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="jwtService">The JWT service.</param>
        /// <param name="passwordHasher">The password hasher.</param>
        /// <param name="dbContext">The database context.</param>
        public AuthController(IJwtService jwtService, IPasswordHasher passwordHasher, SynaxisDbContext dbContext)
        {
            this.jwtService = jwtService;
            this.passwordHasher = passwordHasher;
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="request">The registration request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return this.BadRequest(new { success = false, message = "Email and password are required" });
            }

            var existingUser = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
                .ConfigureAwait(false);

            if (existingUser != null)
            {
                return this.BadRequest(new { success = false, message = "User already exists" });
            }

            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = $"{request.Email} Organization",
                Slug = $"org-{Guid.NewGuid().ToString()[..8]}",
                PrimaryRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            this.dbContext.Organizations.Add(organization);

            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Email = request.Email,
                PasswordHash = this.passwordHasher.HashPassword(request.Password),
                Role = "owner",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            this.dbContext.Users.Add(user);

            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.Ok(new { success = true, userId = user.Id.ToString() });
        }

        /// <summary>
        /// Logs in a user.
        /// </summary>
        /// <param name="request">The login request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The login result with JWT token.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return this.BadRequest(new { success = false, message = "Email and password are required" });
            }

            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
                .ConfigureAwait(false);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                return this.Unauthorized(new { success = false, message = "Invalid credentials" });
            }

            if (!this.passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return this.Unauthorized(new { success = false, message = "Invalid credentials" });
            }

            var token = this.jwtService.GenerateToken(user);
            return this.Ok(
                new
                {
                    token,
                    user = new
                    {
                        id = user.Id.ToString(),
                        email = user.Email,
                    },
                });
        }

        /// <summary>
        /// Development login endpoint for testing.
        /// </summary>
        /// <param name="request">The dev login request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The login result with JWT token.</returns>
        [HttpPost("dev-login")]
        public async Task<IActionResult> DevLogin([FromBody] DevLoginRequest request, CancellationToken cancellationToken)
        {
            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                var organization = new Organization
                {
                    Id = Guid.NewGuid(),
                    Name = "Dev Organization",
                    Slug = $"dev-org-{Guid.NewGuid().ToString()[..8]}",
                    PrimaryRegion = "us-east-1",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };
                this.dbContext.Organizations.Add(organization);

                user = new User
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organization.Id,
                    Email = request.Email,
                    PasswordHash = "N/A",
                    Role = "owner",
                    DataResidencyRegion = "us-east-1",
                    CreatedInRegion = "us-east-1",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };
                this.dbContext.Users.Add(user);
                await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            var token = this.jwtService.GenerateToken(user);
            return this.Ok(new { token });
        }

        /// <summary>
        /// Logs out a user by invalidating their token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The logout result.</returns>
        [HttpPost("logout")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            return Task.FromResult<IActionResult>(this.Ok(new { success = true, message = "Logged out successfully" }));
        }

        /// <summary>
        /// Refreshes an authentication token.
        /// </summary>
        /// <param name="request">The refresh request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A new authentication token.</returns>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return this.BadRequest(new { success = false, message = "Token is required" });
            }

            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(request.Token);
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => string.Equals(c.Type, System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, StringComparison.Ordinal));

                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return this.BadRequest(new { success = false, message = "Invalid token" });
                }

                var user = await this.dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                    .ConfigureAwait(false);

                if (user == null || !user.IsActive)
                {
                    return this.BadRequest(new { success = false, message = "Invalid token or user not active" });
                }

                var newToken = this.jwtService.GenerateToken(user);
                return this.Ok(new { token = newToken });
            }
            catch (Exception)
            {
                return this.BadRequest(new { success = false, message = "Invalid token format" });
            }
        }

        /// <summary>
        /// Initiates password reset process by sending a reset token.
        /// </summary>
        /// <param name="request">The forgot password request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Success message (does not reveal if email exists).</returns>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return this.BadRequest(new { success = false, message = "Email is required" });
            }

            _ = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(new { success = true, message = "If the email exists, a password reset link has been sent" });
        }

        /// <summary>
        /// Resets user password using a valid reset token.
        /// </summary>
        /// <param name="request">The reset password request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The reset result.</returns>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Password))
            {
                return this.BadRequest(new { success = false, message = "Token and password are required" });
            }

            if (!request.Token.StartsWith("reset_", StringComparison.Ordinal))
            {
                return this.BadRequest(new { success = false, message = "Invalid reset token" });
            }

            var parts = request.Token.Split('_');
            if (parts.Length < 2 || !Guid.TryParse(parts[1], out var userId))
            {
                return this.BadRequest(new { success = false, message = "Invalid reset token format" });
            }

            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.BadRequest(new { success = false, message = "Invalid reset token" });
            }

            user.PasswordHash = this.passwordHasher.HashPassword(request.Password);
            user.UpdatedAt = DateTime.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.Ok(new { success = true, message = "Password reset successfully" });
        }

        /// <summary>
        /// Verifies user email address using a verification token.
        /// </summary>
        /// <param name="request">The verify email request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The verification result.</returns>
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return this.BadRequest(new { success = false, message = "Verification token is required" });
            }

            if (!request.Token.StartsWith("verify_", StringComparison.Ordinal))
            {
                return this.BadRequest(new { success = false, message = "Invalid verification token" });
            }

            var parts = request.Token.Split('_');
            if (parts.Length < 2 || !Guid.TryParse(parts[1], out var userId))
            {
                return this.BadRequest(new { success = false, message = "Invalid verification token format" });
            }

            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.BadRequest(new { success = false, message = "Invalid verification token" });
            }

            if (user.EmailVerifiedAt == null)
            {
                user.EmailVerifiedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return this.Ok(new { success = true, message = "Email verified successfully" });
        }
    }

    /// <summary>
    /// Request to register a new user.
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to log in a user.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for development login.
    /// </summary>
    public class DevLoginRequest
    {
        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to refresh an authentication token.
    /// </summary>
    public class RefreshRequest
    {
        /// <summary>
        /// Gets or sets the current token to refresh.
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to initiate password reset.
    /// </summary>
    public class ForgotPasswordRequest
    {
        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to reset password with a token.
    /// </summary>
    public class ResetPasswordRequest
    {
        /// <summary>
        /// Gets or sets the reset token.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new password.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to verify email address.
    /// </summary>
    public class VerifyEmailRequest
    {
        /// <summary>
        /// Gets or sets the verification token.
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }
}
