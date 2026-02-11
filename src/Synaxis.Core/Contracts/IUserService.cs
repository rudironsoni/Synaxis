// <copyright file="IUserService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Threading.Tasks;
    using Synaxis.Core.Models;

    /// <summary>
    /// Service for managing users with data residency compliance.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Create a new user.
        /// </summary>
        /// <param name="request">The user creation request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created user.</returns>
        Task<User> CreateUserAsync(CreateUserRequest request);

        /// <summary>
        /// Get user by ID.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user.</returns>
        Task<User> GetUserAsync(Guid id);

        /// <summary>
        /// Get user by email.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user.</returns>
        Task<User> GetUserByEmailAsync(string email);

        /// <summary>
        /// Update user profile.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <param name="request">The user update request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated user.</returns>
        Task<User> UpdateUserAsync(Guid id, UpdateUserRequest request);

        /// <summary>
        /// Delete user (soft delete).
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the deletion was successful.</returns>
        Task<bool> DeleteUserAsync(Guid id);

        /// <summary>
        /// Authenticate user with email and password.
        /// </summary>
        /// <param name="email">The email address.</param>
        /// <param name="password">The password.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the authenticated user.</returns>
        Task<User> AuthenticateAsync(string email, string password);

        /// <summary>
        /// Setup MFA for user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the MFA setup result.</returns>
        Task<MfaSetupResult> SetupMfaAsync(Guid userId);

        /// <summary>
        /// Enable MFA for user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="totpCode">The TOTP code to verify.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the MFA enable result with backup codes.</returns>
        Task<MfaEnableResult> EnableMfaAsync(Guid userId, string totpCode);

        /// <summary>
        /// Disable MFA for user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether MFA was disabled.</returns>
        Task<bool> DisableMfaAsync(Guid userId);

        /// <summary>
        /// Disable MFA for user with code verification.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="code">The TOTP code or backup code to verify.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether MFA was disabled.</returns>
        Task<bool> DisableMfaAsync(Guid userId, string code);

        /// <summary>
        /// Verify MFA code.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="code">The MFA code to verify.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the code is valid.</returns>
        Task<bool> VerifyMfaCodeAsync(Guid userId, string code);

        /// <summary>
        /// Update cross-border consent.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="consentGiven">Whether consent was given.</param>
        /// <param name="version">The consent version.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the update was successful.</returns>
        Task<bool> UpdateCrossBorderConsentAsync(Guid userId, bool consentGiven, string version);

        /// <summary>
        /// Check if user has given cross-border consent.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether consent was given.</returns>
        Task<bool> HasCrossBorderConsentAsync(Guid userId);

        /// <summary>
        /// Hash password using BCrypt.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <returns>The hashed password.</returns>
        string HashPassword(string password);

        /// <summary>
        /// Verify password against hash.
        /// </summary>
        /// <param name="password">The password to verify.</param>
        /// <param name="hash">The password hash.</param>
        /// <returns>True if password matches hash; otherwise, false.</returns>
        bool VerifyPassword(string password, string hash);
    }

    /// <summary>
    /// Represents a request to create a user.
    /// </summary>
    public class CreateUserRequest
    {
        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        public string Role { get; set; } = "member";

        /// <summary>
        /// Gets or sets the data residency region.
        /// </summary>
        public string DataResidencyRegion { get; set; }

        /// <summary>
        /// Gets or sets the region where user was created.
        /// </summary>
        public string CreatedInRegion { get; set; }
    }

    /// <summary>
    /// Represents a request to update a user.
    /// </summary>
    public class UpdateUserRequest
    {
        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the timezone.
        /// </summary>
        public string Timezone { get; set; }

        /// <summary>
        /// Gets or sets the locale.
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        /// Gets or sets the avatar URL.
        /// </summary>
        public string AvatarUrl { get; set; }
    }

    /// <summary>
    /// Represents the result of MFA setup.
    /// </summary>
    public class MfaSetupResult
    {
        /// <summary>
        /// Gets or sets the MFA secret.
        /// </summary>
        public string Secret { get; set; }

        /// <summary>
        /// Gets or sets the QR code URL.
        /// </summary>
        public string QrCodeUrl { get; set; }

        /// <summary>
        /// Gets or sets the QR code image as base64 string.
        /// </summary>
        public string QrCodeImage { get; set; }

        /// <summary>
        /// Gets or sets the manual entry key.
        /// </summary>
        public string ManualEntryKey { get; set; }
    }
}
