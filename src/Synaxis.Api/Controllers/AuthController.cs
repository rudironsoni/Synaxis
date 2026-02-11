// <copyright file="AuthController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Controllers
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.Api.DTOs.Authentication;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using MfaEnableResult = Synaxis.Core.Contracts.MfaEnableResult;

    /// <summary>
    /// Controller for authentication endpoints.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly Synaxis.Infrastructure.Data.SynaxisDbContext _dbContext;

        public AuthController(
            IAuthenticationService authenticationService,
            IUserService userService,
            IEmailService emailService,
            ILogger<AuthController> logger,
            Synaxis.Infrastructure.Data.SynaxisDbContext dbContext)
        {
            this._authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            this._userService = userService ?? throw new ArgumentNullException(nameof(userService));
            this._emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Authenticate a user with email and password.
        /// </summary>
        /// <param name="request">The login request.</param>
        /// <returns>The authentication result.</returns>
        [HttpPost("login")]
        public async Task<ActionResult<DTOs.Authentication.AuthenticationResult>> Login([FromBody] LoginRequest request)
        {
            try
            {
                this._logger.LogInformation("Login attempt for email: {Email}", request.Email);

                var result = await this._authenticationService.AuthenticateAsync(request.Email, request.Password);

                if (!result.Success)
                {
                    if (result.RequiresMfa)
                    {
                        return this.Ok(new { requiresMfa = true, message = result.ErrorMessage });
                    }

                    return this.Unauthorized(new { message = result.ErrorMessage });
                }

                return this.Ok(result);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return this.StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Login with MFA code.
        /// </summary>
        /// <param name="request">The MFA login request.</param>
        /// <returns>The authentication result.</returns>
        [HttpPost("login/mfa")]
        public async Task<ActionResult<DTOs.Authentication.AuthenticationResult>> LoginWithMfa([FromBody] MfaLoginRequest request)
        {
            try
            {
                this._logger.LogInformation("MFA login attempt for email: {Email}", request.Email);

                // First authenticate with email and password
                var authResult = await this._authenticationService.AuthenticateAsync(request.Email, request.Password);

                if (!authResult.Success || !authResult.RequiresMfa)
                {
                    return this.Unauthorized(new { message = "Invalid credentials or MFA not required" });
                }

                // Verify MFA code
                var mfaValid = await this._userService.VerifyMfaCodeAsync(authResult.User.Id, request.Code);

                if (!mfaValid)
                {
                    this._logger.LogWarning("Invalid MFA code for user: {UserId}", authResult.User.Id);
                    return this.Unauthorized(new { message = "Invalid MFA code" });
                }

                // For now, return success with user info
                // In a real implementation, you'd call a method to generate tokens after MFA verification
                return this.Ok(new DTOs.Authentication.AuthenticationResult
                {
                    Success = true,
                    User = this.MapToUserDto(authResult.User),
                    RequiresMfa = false,
                    Message = "MFA verification successful",
                });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during MFA login for email: {Email}", request.Email);
                return this.StatusCode(500, new { message = "An error occurred during MFA login" });
            }
        }

        /// <summary>
        /// Register a new user.
        /// </summary>
        /// <param name="request">The registration request.</param>
        /// <returns>The authentication result.</returns>
        [HttpPost("register")]
        public async Task<ActionResult<DTOs.Authentication.AuthenticationResult>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                this._logger.LogInformation("Registration attempt for email: {Email}", request.Email);

                var user = await this._userService.CreateUserAsync(new CreateUserRequest
                {
                    OrganizationId = request.OrganizationId ?? Guid.Empty,
                    Email = request.Email,
                    Password = request.Password,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    DataResidencyRegion = request.DataResidencyRegion ?? "us-east-1",
                    CreatedInRegion = request.CreatedInRegion ?? "us-east-1",
                    Role = "member",
                });

                // Generate and store verification token
                var verificationToken = Guid.NewGuid().ToString();
                var verificationUrl = $"{this.Request.Scheme}://{this.Request.Host}/api/auth/verify-email?token={verificationToken}";

                var tokenEntity = new Core.Models.EmailVerificationToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    TokenHash = HashToken(verificationToken),
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow,
                };

                this._dbContext.EmailVerificationTokens.Add(tokenEntity);
                await this._dbContext.SaveChangesAsync();

                // Send verification email
                await this._emailService.SendVerificationEmailAsync(user.Email, verificationUrl);

                this._logger.LogInformation("Registration successful for email: {Email}", request.Email);

                return this.Ok(new DTOs.Authentication.AuthenticationResult
                {
                    Success = true,
                    User = this.MapToUserDto(user),
                    Message = "Registration successful. Please check your email to verify your account.",
                });
            }
            catch (InvalidOperationException ex)
            {
                this._logger.LogWarning(ex, "Registration failed for email: {Email}", request.Email);
                return this.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
                return this.StatusCode(500, new { message = "An error occurred during registration" });
            }
        }

        /// <summary>
        /// Verify email address.
        /// </summary>
        /// <param name="request">The verification request.</param>
        /// <returns>The verification result.</returns>
        [HttpPost("verify-email")]
        public async Task<ActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            try
            {
                this._logger.LogInformation("Email verification attempt for token: {Token}", request.Token);

                // Validate request
                if (string.IsNullOrEmpty(request.Token))
                {
                    return this.BadRequest(new { message = "Token is required" });
                }

                var validationResult = await this.ValidateAndVerifyEmailTokenAsync(request.Token);
                if (validationResult != null)
                {
                    return validationResult;
                }

                return this.Ok(new { message = "Email verified successfully" });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during email verification");
                return this.StatusCode(500, new { message = "An error occurred during email verification" });
            }
        }

        /// <summary>
        /// Resend verification email.
        /// </summary>
        /// <param name="request">The resend verification request.</param>
        /// <returns>The resend result.</returns>
        [HttpPost("resend-verification")]
        public async Task<ActionResult> ResendVerificationEmail([FromBody] ResendVerificationRequest request)
        {
            try
            {
                this._logger.LogInformation("Resend verification email request for: {Email}", request.Email);

                // Validate request
                if (string.IsNullOrEmpty(request.Email))
                {
                    return this.BadRequest(new { message = "Email is required" });
                }

                var user = await this._dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    // Don't reveal that user doesn't exist
                    this._logger.LogInformation("Resend verification email for non-existent email: {Email}", request.Email);
                    return this.Ok(new { message = "If the email exists, a verification link has been sent" });
                }

                // Check if email is already verified
                if (user.EmailVerifiedAt.HasValue)
                {
                    this._logger.LogInformation("Email already verified for user: {UserId}", user.Id);
                    return this.Ok(new { message = "Email is already verified" });
                }

                await this.InvalidateExistingTokensAsync(user.Id);
                await this.SendNewVerificationEmailAsync(user);

                this._logger.LogInformation("Verification email resent for user: {UserId}", user.Id);

                return this.Ok(new { message = "Verification email sent" });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during resend verification email for: {Email}", request.Email);
                return this.StatusCode(500, new { message = "An error occurred while resending verification email" });
            }
        }

        /// <summary>
        /// Get email verification status for the current user.
        /// </summary>
        /// <returns>The email verification status.</returns>
        [HttpGet("me/verification-status")]
        [Authorize]
        public async Task<ActionResult<EmailVerificationStatusDto>> GetVerificationStatus()
        {
            try
            {
                var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return this.Unauthorized(new { message = "Invalid user" });
                }

                var user = await this._userService.GetUserAsync(userId);

                return this.Ok(new EmailVerificationStatusDto
                {
                    IsVerified = user.EmailVerifiedAt.HasValue,
                    Email = user.Email,
                    VerifiedAt = user.EmailVerifiedAt,
                });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error getting verification status");
                return this.StatusCode(500, new { message = "An error occurred while getting verification status" });
            }
        }

        /// <summary>
        /// Refresh access token.
        /// </summary>
        /// <param name="request">The refresh token request.</param>
        /// <returns>The authentication result.</returns>
        [HttpPost("refresh")]
        public async Task<ActionResult<DTOs.Authentication.AuthenticationResult>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                this._logger.LogInformation("Token refresh attempt");

                var result = await this._authenticationService.RefreshTokenAsync(request.RefreshToken);

                if (!result.Success)
                {
                    return this.Unauthorized(new { message = result.ErrorMessage });
                }

                return this.Ok(result);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during token refresh");
                return this.StatusCode(500, new { message = "An error occurred during token refresh" });
            }
        }

        /// <summary>
        /// Logout user.
        /// </summary>
        /// <param name="request">The logout request.</param>
        /// <returns>204 No Content on success.</returns>
        [HttpPost("logout")]
        public async Task<ActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                this._logger.LogInformation("Logout attempt");

                await this._authenticationService.LogoutAsync(request.RefreshToken, request.AccessToken);

                return this.NoContent();
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during logout");
                return this.StatusCode(500, new { message = "An error occurred during logout" });
            }
        }

        /// <summary>
        /// Request password reset.
        /// </summary>
        /// <param name="request">The forgot password request.</param>
        /// <returns>The forgot password result.</returns>
        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                this._logger.LogInformation("Password reset request for email: {Email}", request.Email);

                // In a real implementation, you'd:
                // 1. Find the user by email
                // 2. Generate a password reset token
                // 3. Save the token to the database
                // 4. Send an email with the reset link
                var resetToken = Guid.NewGuid().ToString();
                var resetUrl = $"{this.Request.Scheme}://{this.Request.Host}/api/auth/reset-password?token={resetToken}";

                await this._emailService.SendPasswordResetEmailAsync(request.Email, resetUrl);

                return this.Ok(new { message = "Password reset email sent" });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during password reset request for email: {Email}", request.Email);
                return this.StatusCode(500, new { message = "An error occurred during password reset request" });
            }
        }

        /// <summary>
        /// Reset password with token.
        /// </summary>
        /// <param name="request">The reset password request.</param>
        /// <returns>The reset password result.</returns>
        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                this._logger.LogInformation("Password reset attempt for token: {Token}", request.Token);

                // Validate request
                if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NewPassword))
                {
                    return this.BadRequest(new { success = false, message = "Token and password are required" });
                }

                // Find the reset token
                var resetToken = await this._dbContext.PasswordResetTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.TokenHash == this._userService.HashPassword(request.Token));

                if (resetToken == null)
                {
                    return this.BadRequest(new { success = false, message = "Invalid or expired token" });
                }

                // Check if token is expired
                if (resetToken.ExpiresAt < DateTime.UtcNow)
                {
                    return this.BadRequest(new { success = false, message = "Token has expired" });
                }

                // Check if token has already been used
                if (resetToken.IsUsed)
                {
                    return this.BadRequest(new { success = false, message = "Token has already been used" });
                }

                // Update user's password
                resetToken.User.PasswordHash = this._userService.HashPassword(request.NewPassword);
                resetToken.User.UpdatedAt = DateTime.UtcNow;

                // Mark token as used
                resetToken.IsUsed = true;

                await this._dbContext.SaveChangesAsync();

                this._logger.LogInformation("Password reset successful for user: {UserId}", resetToken.UserId);

                return this.Ok(new { success = true, message = "Password reset successful" });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during password reset");
                return this.StatusCode(500, new { message = "An error occurred during password reset" });
            }
        }

        /// <summary>
        /// Setup MFA for the current user.
        /// </summary>
        /// <returns>The MFA setup result.</returns>
        [HttpPost("mfa/setup")]
        [Authorize]
        public async Task<ActionResult<MfaSetupResult>> SetupMfa()
        {
            try
            {
                var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return this.Unauthorized(new { message = "Invalid user" });
                }

                this._logger.LogInformation("MFA setup attempt for user: {UserId}", userId);

                var result = await this._userService.SetupMfaAsync(userId);

                return this.Ok(result);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during MFA setup");
                return this.StatusCode(500, new { message = "An error occurred during MFA setup" });
            }
        }

        /// <summary>
        /// Enable MFA for the current user.
        /// </summary>
        /// <param name="request">The MFA enable request.</param>
        /// <returns>The MFA enable result with backup codes.</returns>
        [HttpPost("mfa/enable")]
        [Authorize]
        public async Task<ActionResult<MfaEnableResult>> EnableMfa([FromBody] MfaEnableRequest request)
        {
            try
            {
                var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return this.Unauthorized(new { message = "Invalid user" });
                }

                this._logger.LogInformation("MFA enable attempt for user: {UserId}", userId);

                var result = await this._userService.EnableMfaAsync(userId, request.Code);

                return this.Ok(result);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during MFA enable");
                return this.StatusCode(500, new { message = "An error occurred during MFA enable" });
            }
        }

        /// <summary>
        /// Disable MFA for the current user.
        /// </summary>
        /// <param name="request">The MFA disable request containing TOTP or backup code.</param>
        /// <returns>204 No Content on success.</returns>
        [HttpPost("mfa/disable")]
        [Authorize]
        public async Task<ActionResult> DisableMfa([FromBody] MfaDisableRequest request)
        {
            Guid? userId = null;

            try
            {
                var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var parsedUserId))
                {
                    return this.Unauthorized(new { message = "Invalid user" });
                }

                userId = parsedUserId;
                this._logger.LogInformation("MFA disable attempt for user: {UserId}", userId);

                var result = await this._userService.DisableMfaAsync(userId.Value, request.Code);

                if (!result)
                {
                    return this.BadRequest(new { message = "Invalid TOTP code or backup code" });
                }

                return this.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                this._logger.LogWarning(ex, "MFA disable failed for user: {UserId}", userId ?? Guid.Empty);
                return this.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during MFA disable");
                return this.StatusCode(500, new { message = "An error occurred during MFA disable" });
            }
        }

        /// <summary>
        /// Get the current user profile.
        /// </summary>
        /// <returns>The current user profile.</returns>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return this.Unauthorized(new { message = "Invalid user" });
                }

                var user = await this._userService.GetUserAsync(userId);

                return this.Ok(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    AvatarUrl = user.AvatarUrl,
                    Role = user.Role,
                    MfaEnabled = user.MfaEnabled,
                    EmailVerifiedAt = user.EmailVerifiedAt,
                });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error getting current user");
                return this.StatusCode(500, new { message = "An error occurred while getting user profile" });
            }
        }

        /// <summary>
        /// Update the current user profile.
        /// </summary>
        /// <param name="request">The update user request.</param>
        /// <returns>The updated user profile.</returns>
        [HttpPut("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> UpdateCurrentUser([FromBody] DTOs.Authentication.UpdateUserRequest request)
        {
            try
            {
                var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return this.Unauthorized(new { message = "Invalid user" });
                }

                var updateUserRequest = new Core.Contracts.UpdateUserRequest
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    AvatarUrl = request.AvatarUrl,
                    Timezone = request.Timezone,
                    Locale = request.Locale,
                };

                var user = await this._userService.UpdateUserAsync(userId, updateUserRequest);

                return this.Ok(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    AvatarUrl = user.AvatarUrl,
                    Role = user.Role,
                    MfaEnabled = user.MfaEnabled,
                    EmailVerifiedAt = user.EmailVerifiedAt,
                });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error updating current user");
                return this.StatusCode(500, new { message = "An error occurred while updating user profile" });
            }
        }

        private UserDto MapToUserDto(Core.Models.User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role,
                MfaEnabled = user.MfaEnabled,
                EmailVerifiedAt = user.EmailVerifiedAt,
            };
        }

        private async Task<ActionResult> ValidateAndVerifyEmailTokenAsync(string token)
        {
            // Find the verification token
            var verificationToken = await this._dbContext.EmailVerificationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == HashToken(token));

            if (verificationToken == null)
            {
                this._logger.LogWarning("Invalid email verification token");
                return this.BadRequest(new { message = "Invalid or expired token" });
            }

            // Check if token is expired
            if (verificationToken.ExpiresAt < DateTime.UtcNow)
            {
                this._logger.LogWarning("Expired email verification token for user: {UserId}", verificationToken.UserId);
                return this.BadRequest(new { message = "Invalid or expired token" });
            }

            // Check if token has already been used
            if (verificationToken.IsUsed)
            {
                this._logger.LogWarning("Already used email verification token for user: {UserId}", verificationToken.UserId);
                return this.BadRequest(new { message = "Token has already been used" });
            }

            // Check if email is already verified
            if (verificationToken.User.EmailVerifiedAt.HasValue)
            {
                this._logger.LogWarning("Email already verified for user: {UserId}", verificationToken.UserId);
                return this.BadRequest(new { message = "Email is already verified" });
            }

            // Update user's email verification status
            verificationToken.User.EmailVerifiedAt = DateTime.UtcNow;
            verificationToken.User.UpdatedAt = DateTime.UtcNow;

            // Mark token as used
            verificationToken.IsUsed = true;

            await this._dbContext.SaveChangesAsync();

            this._logger.LogInformation("Email verified successfully for user: {UserId}", verificationToken.UserId);

            return null;
        }

        private async Task InvalidateExistingTokensAsync(Guid userId)
        {
            var existingTokens = await this._dbContext.EmailVerificationTokens
                .Where(t => t.UserId == userId && !t.IsUsed)
                .ToListAsync();

            foreach (var token in existingTokens)
            {
                token.IsUsed = true;
            }

            await this._dbContext.SaveChangesAsync();
        }

        private async Task SendNewVerificationEmailAsync(Core.Models.User user)
        {
            var verificationToken = Guid.NewGuid().ToString();
            var verificationUrl = $"{this.Request.Scheme}://{this.Request.Host}/api/auth/verify-email?token={verificationToken}";

            var tokenEntity = new Core.Models.EmailVerificationToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = HashToken(verificationToken),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow,
            };

            this._dbContext.EmailVerificationTokens.Add(tokenEntity);
            await this._dbContext.SaveChangesAsync();

            await this._emailService.SendVerificationEmailAsync(user.Email, verificationUrl);
        }

        private static string HashToken(string token)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(token);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
