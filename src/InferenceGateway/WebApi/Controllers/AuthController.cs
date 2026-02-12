// <copyright file="AuthController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using System.IdentityModel.Tokens.Jwt;
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

            // Create a refresh token
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = this.passwordHasher.HashPassword(Guid.NewGuid().ToString()),
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
            };
            this.dbContext.RefreshTokens.Add(refreshToken);
            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.Ok(new { token, refreshToken = refreshToken.TokenHash });
        }

        /// <summary>
        /// Logs out a user.
        /// </summary>
        /// <param name="request">The logout request (optional).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The logout result.</returns>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
        {
            var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.Unauthorized(new { success = false, message = "Invalid user" });
            }

            // Revoke refresh token if provided
            if (request != null && !string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                var refreshToken = await this.dbContext.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.TokenHash == request.RefreshToken && rt.UserId == userId, cancellationToken)
                    .ConfigureAwait(false);

                if (refreshToken != null)
                {
                    refreshToken.IsRevoked = true;
                    refreshToken.RevokedAt = DateTime.UtcNow;
                }
            }

            // Blacklist JWT token if provided
            if (request != null && !string.IsNullOrWhiteSpace(request.AccessToken))
            {
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(request.AccessToken);
                    var jtiClaim = jwtToken.Claims.FirstOrDefault(c => string.Equals(c.Type, JwtRegisteredClaimNames.Jti, StringComparison.Ordinal))?.Value;

                    if (!string.IsNullOrEmpty(jtiClaim))
                    {
                        var blacklistedToken = new JwtBlacklist
                        {
                            Id = Guid.NewGuid(),
                            TokenId = jtiClaim,
                            UserId = userId,
                            ExpiresAt = jwtToken.ValidTo,
                            CreatedAt = DateTime.UtcNow,
                        };
                        this.dbContext.JwtBlacklists.Add(blacklistedToken);
                    }
                }
                catch
                {
                    // Ignore token parsing errors
                }
            }

            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Return 200 OK for backward compatibility when no request body is provided
            // Return 204 No Content when request body is provided
            return request == null ? this.Ok(new { success = true, message = "Logged out successfully" }) : this.NoContent();
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

            // Check if JWT token is blacklisted
            if (await this.IsTokenBlacklistedAsync(request.Token, cancellationToken))
            {
                return this.Unauthorized(new { success = false, message = "Token has been revoked" });
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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result.</returns>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            // Validate email
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return this.BadRequest(new { success = false, message = "Email is required" });
            }

            // Find user by email
            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
                .ConfigureAwait(false);

            // Only proceed if user exists (security: don't reveal if email exists)
            if (user != null)
            {
                // Generate a secure random token
                var token = GenerateSecureToken();

                // Hash the token for storage
                var tokenHash = this.passwordHasher.HashPassword(token);

                // Create password reset token record
                var resetToken = new PasswordResetToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    TokenHash = tokenHash,
                    ExpiresAt = DateTime.UtcNow.AddHours(1), // Token expires in 1 hour
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow,
                };

                this.dbContext.PasswordResetTokens.Add(resetToken);
                await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                // Generate reset link
                var resetLink = $"{this.Request.Scheme}://{this.Request.Host}/auth/reset-password?token={token}";

                // Log the reset link (in production, this would send an email)
                this.logger.LogInformation("Password reset link generated for user {UserId}: {ResetLink}", user.Id, resetLink);
            }

            // Always return success to avoid leaking user existence
            return this.Ok(new { success = true, message = "If the email exists, a password reset link has been sent" });
        }

        /// <summary>
        /// Generates a secure random token for password reset.
        /// </summary>
        /// <returns>A secure random token.</returns>
        private static string GenerateSecureToken()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var tokenBytes = new byte[32];
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", string.Empty);
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

            // Find the reset token by hashing the provided token and comparing with stored hashes
            var allResetTokens = await this.dbContext.PasswordResetTokens
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var resetToken = allResetTokens.FirstOrDefault(token => this.passwordHasher.VerifyPassword(request.Token, token.TokenHash));

            if (resetToken == null)
            {
                return this.BadRequest(new { success = false, message = "Invalid or expired reset token" });
            }

            // Check if token has expired
            if (resetToken.ExpiresAt < DateTime.UtcNow)
            {
                return this.BadRequest(new { success = false, message = "Reset token has expired" });
            }

            // Check if token has already been used
            if (resetToken.IsUsed)
            {
                return this.BadRequest(new { success = false, message = "Reset token has already been used" });
            }

            // Get the user
            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == resetToken.UserId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.BadRequest(new { success = false, message = "User not found" });
            }

            // Update the user's password
            user.PasswordHash = this.passwordHasher.HashPassword(request.Password);
            user.UpdatedAt = DateTime.UtcNow;

            // Mark the token as used
            resetToken.IsUsed = true;

            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Return 200 OK with success response
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
        [HttpPost("/api/auth/mfa/setup")]
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
            var base32Secret = OtpNet.Base32Encoding.ToString(secret);

            // Store the secret temporarily (in a real implementation, this should be stored securely)
            user.MfaSecret = base32Secret;
            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Generate QR code URI
            var appName = "Synaxis";
            var qrCodeUri = $"otpauth://totp/{Uri.EscapeDataString(appName)}:{Uri.EscapeDataString(user.Email)}?secret={base32Secret}&issuer={Uri.EscapeDataString(appName)}";

            return this.Ok(new { secret = base32Secret, qrCodeUri });
        }

        /// <summary>
        /// Enables MFA for the authenticated user.
        /// </summary>
        /// <param name="request">The enable MFA request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result.</returns>
        [HttpPost("/api/v1/auth/mfa/enable")]
        [HttpPost("/api/auth/mfa/enable")]
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

            if (!TryCreateTotp(user.MfaSecret, out var totp))
            {
                return this.BadRequest(new { success = false, message = "Invalid MFA secret" });
            }

            // Verify the TOTP code
            var isValid = totp.VerifyTotp(request.Code, out _, new OtpNet.VerificationWindow(2, 2));

            if (!isValid)
            {
                return this.BadRequest(new { success = false, message = "Invalid verification code" });
            }

            // Generate 10 backup codes
            var backupCodes = new List<string>();
            var hashedBackupCodes = new List<string>();
            var random = new Random();

            for (int i = 0; i < 10; i++)
            {
                // Generate 8-character alphanumeric code
                var code = new string(Enumerable.Range(0, 8)
                    .Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[random.Next(36)])
                    .ToArray());
                backupCodes.Add(code);
                hashedBackupCodes.Add(this.passwordHasher.HashPassword(code));
            }

            // Store hashed backup codes in database
            user.MfaBackupCodes = string.Join(",", hashedBackupCodes);

            // Enable MFA
            user.MfaEnabled = true;
            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.Ok(new { success = true, message = "MFA enabled successfully", backupCodes });
        }

        /// <summary>
        /// Disables MFA for the authenticated user.
        /// </summary>
        /// <param name="request">The MFA disable request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result.</returns>
        [HttpPost("/api/v1/auth/mfa/disable")]
        [HttpPost("/api/auth/mfa/disable")]
        [Authorize]
        public async Task<IActionResult> MfaDisable([FromBody] MfaDisableRequest request, CancellationToken cancellationToken)
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

            // Check if MFA is enabled
            if (!user.MfaEnabled)
            {
                return this.BadRequest(new { success = false, message = "MFA is not enabled" });
            }

            // Validate code
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return this.BadRequest(new { success = false, message = "Code is required" });
            }

            // Check if it's a backup code
            var isBackupCodeValid = this.IsBackupCodeValid(user, request.Code);

            // Check if it's a valid TOTP code
            var isTotpValid = AuthController.IsTotpValid(user, request.Code);

            if (!isBackupCodeValid && !isTotpValid)
            {
                return this.BadRequest(new { success = false, message = "Invalid code" });
            }

            // Disable MFA
            user.MfaEnabled = false;
            user.MfaSecret = null;
            user.MfaBackupCodes = null;
            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        /// <summary>
        /// Checks if the provided code is a valid backup code.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="code">The code to check.</param>
        /// <returns>True if the code is a valid backup code; otherwise, false.</returns>
        private bool IsBackupCodeValid(User user, string code)
        {
            if (string.IsNullOrEmpty(user.MfaBackupCodes))
            {
                return false;
            }

            var hashedBackupCodes = user.MfaBackupCodes.Split(',');
            return hashedBackupCodes.Any(hashedCode => this.passwordHasher.VerifyPassword(code, hashedCode));
        }

        /// <summary>
        /// Checks if the provided code is a valid TOTP code.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="code">The code to check.</param>
        /// <returns>True if the code is a valid TOTP code; otherwise, false.</returns>
        private static bool IsTotpValid(User user, string code)
        {
            if (string.IsNullOrEmpty(user.MfaSecret) || !TryCreateTotp(user.MfaSecret, out var totp))
            {
                return false;
            }

            return totp.VerifyTotp(code, out _, new OtpNet.VerificationWindow(2, 2));
        }

        private static bool TryCreateTotp(string secret, out OtpNet.Totp totp)
        {
            try
            {
                var bytes = OtpNet.Base32Encoding.ToBytes(secret);
                totp = new OtpNet.Totp(bytes);
                return true;
            }
            catch
            {
                try
                {
                    var bytes = Convert.FromBase64String(secret);
                    totp = new OtpNet.Totp(bytes);
                    return true;
                }
                catch
                {
                    totp = null!;
                    return false;
                }
            }
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

            if (!TryCreateTotp(user.MfaSecret, out var totp))
            {
                return this.BadRequest(new { success = false, message = "Invalid MFA secret" });
            }

            // Verify the TOTP code
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

        /// <summary>
        /// Checks if a JWT token is blacklisted.
        /// </summary>
        /// <param name="token">The JWT token to check.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the token is blacklisted; otherwise, false.</returns>
        private async Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var jtiClaim = jwtToken.Claims.FirstOrDefault(c => string.Equals(c.Type, JwtRegisteredClaimNames.Jti, StringComparison.Ordinal))?.Value;

                if (string.IsNullOrEmpty(jtiClaim))
                {
                    return false;
                }

                var blacklistedToken = await this.dbContext.JwtBlacklists
                    .FirstOrDefaultAsync(jb => jb.TokenId == jtiClaim, cancellationToken)
                    .ConfigureAwait(false);

                return blacklistedToken != null;
            }
            catch
            {
                // If token parsing fails, consider it not blacklisted
                return false;
            }
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
    /// Request to logout.
    /// </summary>
    public class LogoutRequest
    {
        /// <summary>
        /// Gets or sets the refresh token.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to disable MFA.
    /// </summary>
    public class MfaDisableRequest
    {
        /// <summary>
        /// Gets or sets the TOTP code or backup code.
        /// </summary>
        public string Code { get; set; } = string.Empty;
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
