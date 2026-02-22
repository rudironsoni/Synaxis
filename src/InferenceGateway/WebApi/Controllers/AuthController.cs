// <copyright file="AuthController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using OtpNet;
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

#pragma warning disable S4487 // Unused field kept for future use
        private readonly ILogger<AuthController> logger;
#pragma warning restore S4487

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
            this.logger.LogInformation("Registering user with email {Email}", request.Email);

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return this.BadRequest(new { success = false, message = "Email and password are required" });
            }

            var existingUser = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
                .ConfigureAwait(false);

            if (existingUser != null)
            {
                this.logger.LogWarning("User with email {Email} already exists", request.Email);
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

            // Create refresh token for the user
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = HashToken(Guid.NewGuid().ToString()),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
            };
            this.dbContext.RefreshTokens.Add(refreshToken);
            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.Ok(new { token, refreshToken = refreshToken.TokenHash });
        }

        private static string HashToken(string token)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(token);
            var hash = sha256.ComputeHash(bytes);
            return System.Convert.ToBase64String(hash);
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
                    return this.Unauthorized(new { success = false, message = "Invalid token or user not active" });
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

            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
                .ConfigureAwait(false);

            // Only create token if user exists and is active
            // For security, return same message regardless (don't reveal if email exists or user is inactive)
            if (user != null && user.IsActive)
            {
                var token = new PasswordResetToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    TokenHash = HashToken(Guid.NewGuid().ToString()),
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow,
                };
                this.dbContext.PasswordResetTokens.Add(token);
                await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

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

        /// <summary>
        /// Sets up MFA for the authenticated user.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The MFA setup result with secret and QR code URI.</returns>
        [HttpPost("mfa/setup")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> SetupMfa(CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();
            if (userId == null)
            {
                return this.Unauthorized(new { success = false, message = "Invalid user" });
            }

            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound(new { success = false, message = "User not found" });
            }

            // Generate a new TOTP secret
            var secret = GenerateTotpSecret();
            user.MfaSecret = secret;
            user.UpdatedAt = DateTime.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Generate QR code URI with URL-encoded email
            var encodedEmail = Uri.EscapeDataString(user.Email);
            var qrCodeUri = $"otpauth://totp/Synaxis:{encodedEmail}?secret={secret}&issuer=Synaxis";

            return this.Ok(new
            {
                success = true,
                secret,
                qrCodeUri,
            });
        }

        /// <summary>
        /// Enables MFA for the authenticated user.
        /// </summary>
        /// <param name="request">The MFA enable request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The MFA enable result with backup codes.</returns>
        [HttpPost("mfa/enable")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> EnableMfa([FromBody] MfaEnableRequest request, CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();
            if (userId == null)
            {
                return this.Unauthorized(new { success = false, message = "Invalid user" });
            }

            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound(new { success = false, message = "User not found" });
            }

            if (string.IsNullOrEmpty(user.MfaSecret))
            {
                return this.BadRequest(new { success = false, message = "MFA not set up. Please call setup first." });
            }

            if (user.MfaEnabled)
            {
                return this.BadRequest(new { success = false, message = "MFA is already enabled" });
            }

            // Validate the TOTP code
            if (!ValidateTotpCode(user.MfaSecret, request.Code))
            {
                return this.BadRequest(new { success = false, message = "Invalid TOTP code" });
            }

            // Generate backup codes
            var backupCodes = GenerateBackupCodes();
            user.MfaBackupCodes = string.Join(",", backupCodes.Select(HashBackupCode));
            user.MfaEnabled = true;
            user.UpdatedAt = DateTime.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.Ok(new
            {
                success = true,
                backupCodes,
            });
        }

        /// <summary>
        /// Disables MFA for the authenticated user.
        /// </summary>
        /// <param name="request">The MFA disable request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content on success.</returns>
        [HttpPost("mfa/disable")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> DisableMfa([FromBody] MfaDisableRequest request, CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();
            if (userId == null)
            {
                return this.Unauthorized(new { success = false, message = "Invalid user" });
            }

            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound(new { success = false, message = "User not found" });
            }

            if (!user.MfaEnabled)
            {
                return this.BadRequest(new { success = false, message = "MFA is not enabled" });
            }

            // Validate TOTP code or backup code
            if (!ValidateTotpCode(user.MfaSecret!, request.Code) &&
                !ValidateBackupCode(user.MfaBackupCodes, request.Code))
            {
                return this.BadRequest(new { success = false, message = "Invalid TOTP code or backup code" });
            }

            user.MfaEnabled = false;
            user.MfaSecret = null;
            user.MfaBackupCodes = null;
            user.UpdatedAt = DateTime.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        /// <summary>
        /// Logs in with MFA code.
        /// </summary>
        /// <param name="request">The MFA login request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The login result with JWT token.</returns>
        [HttpPost("login/mfa")]
        public async Task<IActionResult> LoginWithMfa([FromBody] MfaLoginRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Code))
            {
                return this.BadRequest(new { success = false, message = "Email, password, and code are required" });
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

            if (!user.MfaEnabled)
            {
                return this.BadRequest(new { success = false, message = "MFA is not enabled for this user" });
            }

            // Validate TOTP code or backup code
            if (!ValidateTotpCode(user.MfaSecret!, request.Code) &&
                !ValidateBackupCode(user.MfaBackupCodes, request.Code))
            {
                return this.Unauthorized(new { success = false, message = "Invalid MFA code" });
            }

            // If backup code was used, remove it
            if (ValidateBackupCode(user.MfaBackupCodes, request.Code))
            {
                var remainingCodes = user.MfaBackupCodes!.Split(',')
                    .Where(hc => !VerifyBackupCodeHash(request.Code, hc))
                    .ToList();
                user.MfaBackupCodes = string.Join(",", remainingCodes);
                user.UpdatedAt = DateTime.UtcNow;
                await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            var token = this.jwtService.GenerateToken(user);
            return this.Ok(new
            {
                success = true,
                token,
                user = new
                {
                    id = user.Id.ToString(),
                    email = user.Email,
                },
            });
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }

            return userId;
        }

        private static string GenerateTotpSecret()
        {
            var bytes = new byte[20];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return Base32Encode(bytes);
        }

        private static bool ValidateTotpCode(string secret, string code)
        {
            try
            {
                var key = OtpNet.Base32Encoding.ToBytes(secret);
                var totp = new OtpNet.Totp(key);
                return totp.VerifyTotp(code, out _);
            }
            catch
            {
                return false;
            }
        }

        private static string[] GenerateBackupCodes()
        {
            var codes = new string[10];
            for (int i = 0; i < codes.Length; i++)
            {
                var value = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 100000000);
                codes[i] = value.ToString("D8", System.Globalization.CultureInfo.InvariantCulture);
            }

            return codes;
        }

        private static string HashBackupCode(string code)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(code);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static bool ValidateBackupCode(string? storedHashes, string code)
        {
            if (string.IsNullOrEmpty(storedHashes))
            {
                return false;
            }

            var hashes = storedHashes.Split(',');
            return hashes.Any(h => VerifyBackupCodeHash(code, h));
        }

        private static bool VerifyBackupCodeHash(string code, string hash)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(code);
            var computedHash = sha256.ComputeHash(bytes);

            byte[] storedHashBytes;
            try
            {
                storedHashBytes = Convert.FromBase64String(hash);
            }
            catch (FormatException)
            {
                return false;
            }

            // Use constant-time comparison to avoid timing attacks
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(computedHash, storedHashBytes);
        }

        private static string Base32Encode(byte[] data)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var output = new System.Text.StringBuilder();
            int bits = 0;
            int value = 0;

            foreach (var b in data)
            {
                value = (value << 8) | b;
                bits += 8;

                while (bits >= 5)
                {
                    output.Append(alphabet[(value >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }

            if (bits > 0)
            {
                output.Append(alphabet[(value << (5 - bits)) & 31]);
            }

            return output.ToString();
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

    /// <summary>
    /// Request to enable MFA.
    /// </summary>
    public class MfaEnableRequest
    {
        /// <summary>
        /// Gets or sets the TOTP verification code.
        /// </summary>
        public string Code { get; set; } = string.Empty;
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
    /// Request to log in with MFA.
    /// </summary>
    public class MfaLoginRequest
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
