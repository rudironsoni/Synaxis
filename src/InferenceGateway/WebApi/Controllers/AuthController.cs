// <copyright file="AuthController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using System.Security.Claims;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AuthController> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="jwtService">The JWT service.</param>
        /// <param name="passwordHasher">The password hasher.</param>
        /// <param name="dbContext">The database context.</param>
        /// <param name="logger">The logger.</param>
        public AuthController(IJwtService jwtService, IPasswordHasher passwordHasher, SynaxisDbContext dbContext, ILogger<AuthController> logger)
        {
            this.jwtService = jwtService;
            this.passwordHasher = passwordHasher;
            this.dbContext = dbContext;
            this.logger = logger;
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
        /// Logs out a user.
        /// </summary>
        /// <returns>The logout result.</returns>
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            return this.Ok(new { success = true, message = "Logged out successfully" });
        }

        /// <summary>
        /// Refreshes a JWT token.
        /// </summary>
        /// <param name="request">The refresh request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The refresh result with new token.</returns>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return this.BadRequest(new { success = false, message = "Token is required" });
            }

            var userId = this.jwtService.ValidateToken(request.Token);
            if (userId == null)
            {
                return this.BadRequest(new { success = false, message = "Invalid or expired token" });
            }

            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.BadRequest(new { success = false, message = "User not found" });
            }

            if (!user.IsActive)
            {
                return this.Unauthorized(new { success = false, message = "User account is inactive" });
            }

            var newToken = this.jwtService.GenerateToken(user);
            return this.Ok(new { token = newToken });
        }

        /// <summary>
        /// Initiates a password reset process.
        /// </summary>
        /// <param name="request">The forgot password request.</param>
        /// <returns>The result.</returns>
        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            // Always return success to avoid leaking user existence
            return this.Ok(new { success = true, message = "If the email exists, a password reset link has been sent" });
        }

        /// <summary>
        /// Resets a user's password.
        /// </summary>
        /// <param name="request">The reset password request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result.</returns>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Password))
            {
                return this.BadRequest(new { success = false, message = "Token and password are required" });
            }

            // Validate token format
            if (!request.Token.StartsWith("reset_", StringComparison.Ordinal))
            {
                return this.BadRequest(new { success = false, message = "Invalid reset token" });
            }

            // Extract user ID from token (format: reset_{userId}_{guid})
            var tokenParts = request.Token.Split('_');
            if (tokenParts.Length >= 2 && Guid.TryParse(tokenParts[1], out var userId))
            {
                var user = await this.dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                    .ConfigureAwait(false);

                if (user != null)
                {
                    // Update password
                    user.PasswordHash = this.passwordHasher.HashPassword(request.Password);
                    await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            return this.Ok(new { success = true, message = "Password reset successful" });
        }

        /// <summary>
        /// Verifies a user's email address.
        /// </summary>
        /// <param name="request">The verify email request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result.</returns>
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return this.BadRequest(new { success = false, message = "Token is required" });
            }

            if (!request.Token.StartsWith("verify_", StringComparison.Ordinal))
            {
                return this.BadRequest(new { success = false, message = "Invalid verification token" });
            }

            // Extract user ID from token
            var parts = request.Token.Split('_');
            if (parts.Length < 2 || !Guid.TryParse(parts[1], out var userId))
            {
                return this.BadRequest(new { success = false, message = "Invalid verification token" });
            }

            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.BadRequest(new { success = false, message = "User not found" });
            }

            if (user.EmailVerifiedAt == null)
            {
                user.EmailVerifiedAt = DateTime.UtcNow;
                await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return this.Ok(new { success = true, message = "Email verified successfully" });
        }

        /// <summary>
        /// Resends email verification link.
        /// </summary>
        /// <param name="request">The resend verification request.</param>
        /// <returns>The result.</returns>
        [HttpPost("/api/v1/auth/resend-verification")]
        public IActionResult ResendVerification([FromBody] ResendVerificationRequest request)
        {
            // Always return success to avoid leaking user existence
            this.logger.LogInformation("Verification email resend requested for {Email}", request.Email);
            return this.Ok(new { success = true, message = "If the email exists, a verification link has been sent" });
        }

        /// <summary>
        /// Sets up MFA for the authenticated user.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The MFA secret and QR code.</returns>
        [HttpPost("/api/v1/auth/mfa/setup")]
        [Authorize]
        public async Task<IActionResult> MfaSetup(CancellationToken cancellationToken)
        {
            var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.Unauthorized(new { success = false, message = "Invalid user" });
            }

            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.Unauthorized(new { success = false, message = "User not found" });
            }

            // Generate a new MFA secret
            var secret = OtpNet.KeyGeneration.GenerateRandomKey(20);
            var base64Secret = Convert.ToBase64String(secret);

            // Store the secret temporarily (in a real implementation, this should be stored securely)
            user.MfaSecret = base64Secret;
            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Generate QR code URI
            var appName = "Synaxis";
            var qrCodeUri = $"otpauth://totp/{Uri.EscapeDataString(appName)}:{Uri.EscapeDataString(user.Email)}?secret={base64Secret}&issuer={Uri.EscapeDataString(appName)}";

            return this.Ok(new { secret = base64Secret, qrCodeUri });
        }

        /// <summary>
        /// Enables MFA for the authenticated user.
        /// </summary>
        /// <param name="request">The enable MFA request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result.</returns>
        [HttpPost("/api/v1/auth/mfa/enable")]
        [Authorize]
        public async Task<IActionResult> MfaEnable([FromBody] MfaEnableRequest request, CancellationToken cancellationToken)
        {
            var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.Unauthorized(new { success = false, message = "Invalid user" });
            }

            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.Unauthorized(new { success = false, message = "User not found" });
            }

            if (string.IsNullOrEmpty(user.MfaSecret))
            {
                return this.BadRequest(new { success = false, message = "MFA not set up. Call /mfa/setup first" });
            }

            // Verify the TOTP code
            var totp = new OtpNet.Totp(Convert.FromBase64String(user.MfaSecret));
            var isValid = totp.VerifyTotp(request.Code, out _, new OtpNet.VerificationWindow(2, 2));

            if (!isValid)
            {
                return this.BadRequest(new { success = false, message = "Invalid verification code" });
            }

            // Enable MFA
            user.MfaEnabled = true;
            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.Ok(new { success = true, message = "MFA enabled successfully" });
        }

        /// <summary>
        /// Disables MFA for the authenticated user.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result.</returns>
        [HttpPost("/api/v1/auth/mfa/disable")]
        [Authorize]
        public async Task<IActionResult> MfaDisable(CancellationToken cancellationToken)
        {
            var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.Unauthorized(new { success = false, message = "Invalid user" });
            }

            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.Unauthorized(new { success = false, message = "User not found" });
            }

            // Disable MFA
            user.MfaEnabled = false;
            user.MfaSecret = null;
            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.Ok(new { success = true, message = "MFA disabled successfully" });
        }

        /// <summary>
        /// Logs in a user with MFA.
        /// </summary>
        /// <param name="request">The MFA login request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The login result with JWT token.</returns>
        [HttpPost("/api/v1/auth/login/mfa")]
        public async Task<IActionResult> LoginMfa([FromBody] LoginMfaRequest request, CancellationToken cancellationToken)
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

            // Verify MFA is enabled
            if (!user.MfaEnabled || string.IsNullOrEmpty(user.MfaSecret))
            {
                return this.BadRequest(new { success = false, message = "MFA not enabled for this user" });
            }

            // Verify the TOTP code
            var totp = new OtpNet.Totp(Convert.FromBase64String(user.MfaSecret));
            var isValid = totp.VerifyTotp(request.Code, out _, new OtpNet.VerificationWindow(2, 2));

            if (!isValid)
            {
                return this.Unauthorized(new { success = false, message = "Invalid MFA code" });
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
    }

    /// <summary>
    /// Request to refresh a JWT token.
    /// </summary>
    public class RefreshRequest
    {
        /// <summary>
        /// Gets or sets the token to refresh.
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
    /// Request to reset password.
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
    /// Request to verify email.
    /// </summary>
    public class VerifyEmailRequest
    {
        /// <summary>
        /// Gets or sets the verification token.
        /// </summary>
        public string Token { get; set; } = string.Empty;
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
    /// Request to resend email verification.
    /// </summary>
    public class ResendVerificationRequest
    {
        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to enable MFA.
    /// </summary>
    public class MfaEnableRequest
    {
        /// <summary>
        /// Gets or sets the verification code.
        /// </summary>
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to login with MFA.
    /// </summary>
    public class LoginMfaRequest
    {
        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MFA code.
        /// </summary>
        public string Code { get; set; } = string.Empty;
    }
}
