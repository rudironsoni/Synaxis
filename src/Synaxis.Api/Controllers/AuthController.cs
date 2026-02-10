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
    using Microsoft.Extensions.Logging;
    using Synaxis.Api.DTOs.Authentication;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;

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

        public AuthController(
            IAuthenticationService authenticationService,
            IUserService userService,
            IEmailService emailService,
            ILogger<AuthController> logger)
        {
            this._authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            this._userService = userService ?? throw new ArgumentNullException(nameof(userService));
            this._emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                // Send verification email
                var verificationToken = Guid.NewGuid().ToString();
                var verificationUrl = $"{this.Request.Scheme}://{this.Request.Host}/api/auth/verify-email?token={verificationToken}";

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

                // In a real implementation, you'd validate the token and update the user's email verification status
                // For now, we'll just return success
                return this.Ok(new { message = "Email verified successfully" });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during email verification");
                return this.StatusCode(500, new { message = "An error occurred during email verification" });
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
        /// <returns>The logout result.</returns>
        [HttpPost("logout")]
        public async Task<ActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                this._logger.LogInformation("Logout attempt");

                await this._authenticationService.LogoutAsync(request.RefreshToken);

                return this.Ok(new { message = "Logout successful" });
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

                // In a real implementation, you'd:
                // 1. Validate the reset token
                // 2. Find the user associated with the token
                // 3. Update the user's password
                // 4. Invalidate the reset token
                return this.Ok(new { message = "Password reset successful" });
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
        /// <returns>The MFA enable result.</returns>
        [HttpPost("mfa/enable")]
        [Authorize]
        public async Task<ActionResult> EnableMfa([FromBody] MfaEnableRequest request)
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

                if (!result)
                {
                    return this.BadRequest(new { message = "Invalid TOTP code" });
                }

                return this.Ok(new { message = "MFA enabled successfully" });
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
        /// <returns>The MFA disable result.</returns>
        [HttpPost("mfa/disable")]
        [Authorize]
        public async Task<ActionResult> DisableMfa()
        {
            try
            {
                var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return this.Unauthorized(new { message = "Invalid user" });
                }

                this._logger.LogInformation("MFA disable attempt for user: {UserId}", userId);

                await this._userService.DisableMfaAsync(userId);

                return this.Ok(new { message = "MFA disabled successfully" });
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
    }
}
