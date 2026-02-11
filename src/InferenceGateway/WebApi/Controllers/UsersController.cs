// <copyright file="UsersController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.Formats;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.InferenceGateway.Application.Interfaces;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Controller for managing user profiles and account settings.
    /// </summary>
    [ApiController]
    [Route("api/v1/users")]
    [Authorize]
    [EnableCors("WebApp")]
    public class UsersController : ControllerBase
    {
        private readonly SynaxisDbContext _synaxisDbContext;
        private readonly IOrganizationUserContext _userContext;
        private readonly IPasswordService _passwordService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersController"/> class.
        /// </summary>
        /// <param name="synaxisDbContext">The Synaxis database context.</param>
        /// <param name="userContext">The organization user context.</param>
        /// <param name="passwordService">The password service.</param>
        public UsersController(SynaxisDbContext synaxisDbContext, IOrganizationUserContext userContext, IPasswordService passwordService)
        {
            this._synaxisDbContext = synaxisDbContext;
            this._userContext = userContext;
            this._passwordService = passwordService;
        }

        /// <summary>
        /// Gets the current user's profile.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current user's profile.</returns>
        [HttpGet("me")]
        public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
        {
            // Check if user is authenticated
            if (!this._userContext.IsAuthenticated)
            {
                return this.Unauthorized();
            }

            // Check if JWT token is blacklisted
            var authHeader = this.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.Ordinal))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                if (await this.IsTokenBlacklistedAsync(token, cancellationToken))
                {
                    return this.Unauthorized();
                }
            }

            var userId = this._userContext.UserId;

            var user = await this._synaxisDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound();
            }

            var response = new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                avatarUrl = user.AvatarUrl,
                timezone = user.Timezone,
                locale = user.Locale,
                role = user.Role,
                organizationId = user.OrganizationId,
                dataResidencyRegion = user.DataResidencyRegion,
                crossBorderConsentGiven = user.CrossBorderConsentGiven,
                crossBorderConsentDate = user.CrossBorderConsentDate,
                mfaEnabled = user.MfaEnabled,
                isActive = user.IsActive,
                createdAt = user.CreatedAt,
                updatedAt = user.UpdatedAt,
            };

            return this.Ok(response);
        }

        /// <summary>
        /// Updates the current user's profile.
        /// </summary>
        /// <param name="request">The update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated user profile.</returns>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            // Check authentication
            var authResult = await this.CheckAuthenticationAsync(cancellationToken);
            if (authResult != null)
            {
                return authResult;
            }

            // Validate request
            var validationResult = this.ValidateUpdateUserRequest(request);
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = this._userContext.UserId;
            var user = await this._synaxisDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound();
            }

            UsersController.UpdateUserFields(user, request);
            user.UpdatedAt = DateTime.UtcNow;
            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.Ok(this.CreateUpdateUserResponse(user));
        }

        private async Task<IActionResult?> CheckAuthenticationAsync(CancellationToken cancellationToken)
        {
            if (!this._userContext.IsAuthenticated)
            {
                return this.Unauthorized();
            }

            var authHeader = this.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.Ordinal))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                if (await this.IsTokenBlacklistedAsync(token, cancellationToken))
                {
                    return this.Unauthorized();
                }
            }

            return null;
        }

        private static void UpdateUserFields(User user, UpdateUserRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.FirstName))
            {
                user.FirstName = request.FirstName;
            }

            if (!string.IsNullOrWhiteSpace(request.LastName))
            {
                user.LastName = request.LastName;
            }

            if (!string.IsNullOrWhiteSpace(request.Timezone))
            {
                user.Timezone = request.Timezone;
            }

            if (!string.IsNullOrWhiteSpace(request.Locale))
            {
                user.Locale = request.Locale;
            }
        }

        private object CreateUpdateUserResponse(User user)
        {
            return new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                avatarUrl = user.AvatarUrl,
                timezone = user.Timezone,
                locale = user.Locale,
                role = user.Role,
                updatedAt = user.UpdatedAt,
            };
        }

        /// <summary>
        /// Deletes the current user's account (soft delete).
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content.</returns>
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken)
        {
            // Check authentication
            var authResult = await this.CheckAuthenticationAsync(cancellationToken);
            if (authResult != null)
            {
                return authResult;
            }

            var userId = this._userContext.UserId;

            var user = await this._synaxisDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound();
            }

            // Soft delete: set IsActive to false and DeletedAt timestamp
            user.IsActive = false;
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            // Revoke all refresh tokens for the user
            var refreshTokens = await this._synaxisDbContext.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        /// <summary>
        /// Uploads an avatar for the current user.
        /// </summary>
        /// <param name="file">The avatar file to upload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Avatar upload response.</returns>
        [HttpPost("me/avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile? file, CancellationToken cancellationToken)
        {
            // Check authentication
            var authResult = await this.CheckAuthenticationAsync(cancellationToken);
            if (authResult != null)
            {
                return authResult;
            }

            // Validate file
            var validationResult = this.ValidateAvatarFile(file);
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = this._userContext.UserId;
            var user = await this._synaxisDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound();
            }

            // Save file and update user (file is guaranteed non-null after validation)
            var avatarUrl = await SaveAvatarFileAsync(file!, userId, cancellationToken);

            user.AvatarUrl = avatarUrl;
            user.UpdatedAt = DateTime.UtcNow;
            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var response = new
            {
                avatarUrl = avatarUrl,
            };

            return this.Ok(response);
        }

        private IActionResult? ValidateAvatarFile(IFormFile? file)
        {
            // Validate file is present
            if (file == null || file.Length == 0)
            {
                return this.BadRequest("Avatar file is required");
            }

            // Validate file type
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedContentTypes.Contains(file.ContentType))
            {
                return this.BadRequest("Invalid file type. Only JPEG, PNG, GIF, and WebP are allowed");
            }

            // Validate file size (max 5MB)
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return this.BadRequest("File size exceeds maximum limit of 5MB");
            }

            return null;
        }

        private static (bool IsValid, string? ErrorMessage) ValidateImageDimensions(IFormFile file)
        {
            using var imageStream = file.OpenReadStream();
            using var image = Image.Load(imageStream);
            const int minDimension = 64;
            const int maxDimension = 1024;

            if (image.Width < minDimension || image.Height < minDimension)
            {
                return (false, "Image dimensions must be at least 64x64 pixels");
            }

            if (image.Width > maxDimension || image.Height > maxDimension)
            {
                return (false, "Image dimensions must not exceed 1024x1024 pixels");
            }

            return (true, null);
        }

        private static async Task<string> SaveAvatarFileAsync(IFormFile file, Guid userId, CancellationToken cancellationToken)
        {
            // Validate image dimensions
            var dimensionValidation = ValidateImageDimensions(file);
            if (!dimensionValidation.IsValid)
            {
                throw new InvalidOperationException(dimensionValidation.ErrorMessage);
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

            // Create upload directory if it doesn't exist
            var uploadPath = Path.Combine("uploads", "avatars", userId.ToString());
            Directory.CreateDirectory(uploadPath);

            // Save file to disk
            var filePath = Path.Combine(uploadPath, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }

            // Generate avatar URL
            return $"/uploads/avatars/{userId}/{uniqueFileName}";
        }

        /// <summary>
        /// Lists all organizations the current user is a member of.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated list of organizations.</returns>
        [HttpGet("me/organizations")]
        public async Task<IActionResult> GetMyOrganizations([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var userId = this._userContext.UserId;

            var query = this._synaxisDbContext.Organizations
                .Where(o => o.Teams.Any(t => t.TeamMemberships.Any(tm => tm.UserId == userId)))
                .OrderBy(o => o.Name);

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    id = o.Id,
                    name = o.Name,
                    slug = o.Slug,
                    description = o.Description,
                    tier = o.Tier,
                    isActive = o.IsActive,
                    createdAt = o.CreatedAt,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(new
            {
                items,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            });
        }

        /// <summary>
        /// Lists all teams the current user is a member of.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated list of teams.</returns>
        [HttpGet("me/teams")]
        public async Task<IActionResult> GetMyTeams([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var userId = this._userContext.UserId;

            var query = this._synaxisDbContext.Teams
                .Where(t => t.TeamMemberships.Any(tm => tm.UserId == userId))
                .OrderBy(t => t.Name);

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    id = t.Id,
                    organizationId = t.OrganizationId,
                    name = t.Name,
                    slug = t.Slug,
                    description = t.Description,
                    isActive = t.IsActive,
                    createdAt = t.CreatedAt,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(new
            {
                items,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            });
        }

        /// <summary>
        /// Requests a GDPR data export for the current user.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Data export request response.</returns>
        [HttpPost("me/data-export")]
        public async Task<IActionResult> RequestDataExport(CancellationToken cancellationToken)
        {
            var userId = this._userContext.UserId;

            var user = await this._synaxisDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound();
            }

            var response = new
            {
                message = "Data export request received - actual export processing to be implemented",
                userId = user.Id,
                requestedAt = DateTime.UtcNow,
            };

            return this.Accepted(response);
        }

        /// <summary>
        /// Updates cross-border data transfer consent for the current user.
        /// </summary>
        /// <param name="request">The consent update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Updated consent information.</returns>
        [HttpPut("me/cross-border-consent")]
        public async Task<IActionResult> UpdateCrossBorderConsent([FromBody] UpdateCrossBorderConsentRequest request, CancellationToken cancellationToken)
        {
            var userId = this._userContext.UserId;

            var validationResult = this.ValidateCrossBorderConsentRequest(request);
            if (validationResult != null)
            {
                return validationResult;
            }

            var user = await this._synaxisDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound();
            }

            // Validation ensures ConsentGiven is not null
            user.CrossBorderConsentGiven = request.ConsentGiven!.Value;
            user.CrossBorderConsentDate = DateTime.UtcNow;
            user.CrossBorderConsentVersion = request.ConsentVersion ?? "v1.0";
            user.UpdatedAt = DateTime.UtcNow;

            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var response = new
            {
                crossBorderConsentGiven = user.CrossBorderConsentGiven,
                crossBorderConsentDate = user.CrossBorderConsentDate,
                crossBorderConsentVersion = user.CrossBorderConsentVersion,
                updatedAt = user.UpdatedAt,
            };

            return this.Ok(response);
        }

        private IActionResult? ValidateCrossBorderConsentRequest(UpdateCrossBorderConsentRequest request)
        {
            if (!request.ConsentGiven.HasValue)
            {
                return this.BadRequest("ConsentGiven is required");
            }

            return null;
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

                var blacklistedToken = await this._synaxisDbContext.JwtBlacklists
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

        /// <summary>
        /// Changes the current user's password.
        /// </summary>
        /// <param name="request">The password change request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The password change result.</returns>
        [HttpPost("me/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            var userId = this._userContext.UserId;

            var validationResult = this.ValidateChangePasswordRequest(request);
            if (validationResult != null)
            {
                return validationResult;
            }

            var result = await this._passwordService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

            if (!result.Success)
            {
                return this.BadRequest(new { success = false, errorMessage = result.ErrorMessage });
            }

            return this.Ok(new
            {
                success = true,
                passwordExpiresAt = result.PasswordExpiresAt,
            });
        }

        /// <summary>
        /// Validates a password against the organization's password policy.
        /// </summary>
        /// <param name="request">The password validation request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The password validation result.</returns>
        [HttpPost("me/password/validate")]
        public async Task<IActionResult> ValidatePassword([FromBody] ValidatePasswordRequest request, CancellationToken cancellationToken)
        {
            var userId = this._userContext.UserId;

            var result = await this._passwordService.ValidatePasswordAsync(userId, request.Password);

            return this.Ok(result);
        }

        private IActionResult? ValidateChangePasswordRequest(ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                return this.BadRequest("CurrentPassword is required");
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return this.BadRequest("NewPassword is required");
            }

            if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
            {
                return this.BadRequest("New password must be different from current password");
            }

            return null;
        }

        private IActionResult? ValidateUpdateUserRequest(UpdateUserRequest request)
        {
            // Validate FirstName
            if (request.FirstName != null && request.FirstName.Length > 100)
            {
                return this.BadRequest("FirstName must not exceed 100 characters");
            }

            // Validate LastName
            if (request.LastName != null && request.LastName.Length > 100)
            {
                return this.BadRequest("LastName must not exceed 100 characters");
            }

            // Validate Timezone
            if (!string.IsNullOrWhiteSpace(request.Timezone) && !IsValidTimezone(request.Timezone))
            {
                return this.BadRequest("Invalid timezone format");
            }

            return null;
        }

        private static bool IsValidTimezone(string timezone)
        {
            // Basic timezone validation - check if it matches common IANA timezone patterns
            // This is a simplified validation. In production, you might want to use
            // TimeZoneInfo.TryConvertToSystemTimeZoneId or a more comprehensive library
            if (string.IsNullOrWhiteSpace(timezone))
            {
                return false;
            }

            // Check for common IANA timezone patterns like "America/New_York", "Europe/London", etc.
            var parts = timezone.Split('/');
            if (parts.Length < 2)
            {
                return false;
            }

            // Each part should be alphanumeric with underscores or hyphens
            return parts.All(part => !string.IsNullOrWhiteSpace(part) && part.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));
        }
    }

    /// <summary>
    /// Request to update cross-border consent.
    /// </summary>
    public class UpdateCrossBorderConsentRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether consent is given.
        /// </summary>
        public bool? ConsentGiven { get; set; }

        /// <summary>
        /// Gets or sets the consent version.
        /// </summary>
        public string? ConsentVersion { get; set; }
    }

    /// <summary>
    /// Request to change password.
    /// </summary>
    public class ChangePasswordRequest
    {
        /// <summary>
        /// Gets or sets the current password.
        /// </summary>
        public required string CurrentPassword { get; set; }

        /// <summary>
        /// Gets or sets the new password.
        /// </summary>
        public required string NewPassword { get; set; }
    }

    /// <summary>
    /// Request to validate password.
    /// </summary>
    public class ValidatePasswordRequest
    {
        /// <summary>
        /// Gets or sets the password to validate.
        /// </summary>
        public required string Password { get; set; }
    }
}
