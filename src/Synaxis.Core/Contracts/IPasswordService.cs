// <copyright file="IPasswordService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Synaxis.Core.Models;

    /// <summary>
    /// Service for managing password policies and password operations.
    /// </summary>
    public interface IPasswordService
    {
        /// <summary>
        /// Validates a password against the organization's password policy.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="password">The password to validate.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the validation result.</returns>
        Task<PasswordValidationResult> ValidatePasswordAsync(Guid userId, string password);

        /// <summary>
        /// Changes the user's password.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="currentPassword">The current password.</param>
        /// <param name="newPassword">The new password.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the change password result.</returns>
        Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

        /// <summary>
        /// Resets the user's password (admin operation).
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="newPassword">The new password.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the reset password result.</returns>
        Task<ResetPasswordResult> ResetPasswordAsync(Guid userId, string newPassword);

        /// <summary>
        /// Gets the password policy for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the password policy.</returns>
        Task<PasswordPolicy> GetPasswordPolicyAsync(Guid organizationId);

        /// <summary>
        /// Updates the password policy for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="policy">The password policy to update.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated password policy.</returns>
        Task<PasswordPolicy> UpdatePasswordPolicyAsync(Guid organizationId, PasswordPolicy policy);

        /// <summary>
        /// Checks if a user's password is expired or about to expire.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the password expiration status.</returns>
        Task<PasswordExpirationStatus> CheckPasswordExpirationAsync(Guid userId);

        /// <summary>
        /// Gets the password strength score for a password.
        /// </summary>
        /// <param name="password">The password to evaluate.</param>
        /// <returns>The password strength score (0-100).</returns>
        int GetPasswordStrength(string password);
    }

    /// <summary>
    /// Result of password validation.
    /// </summary>
    public class PasswordValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the password is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the password strength score (0-100).
        /// </summary>
        public int StrengthScore { get; set; }

        /// <summary>
        /// Gets or sets the strength level (Weak, Fair, Good, Strong, Very Strong).
        /// </summary>
        public required string StrengthLevel { get; set; }

        /// <summary>
        /// Gets or sets the validation errors.
        /// </summary>
        public IList<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the validation warnings.
        /// </summary>
        public IList<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of password change operation.
    /// </summary>
    public class ChangePasswordResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the password change was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the change failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the password expiration date.
        /// </summary>
        public DateTime? PasswordExpiresAt { get; set; }
    }

    /// <summary>
    /// Result of password reset operation.
    /// </summary>
    public class ResetPasswordResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the password reset was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the reset failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the password expiration date.
        /// </summary>
        public DateTime? PasswordExpiresAt { get; set; }
    }

    /// <summary>
    /// Password expiration status.
    /// </summary>
    public class PasswordExpirationStatus
    {
        /// <summary>
        /// Gets or sets a value indicating whether the password is expired.
        /// </summary>
        public bool IsExpired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the password is about to expire.
        /// </summary>
        public bool IsExpiringSoon { get; set; }

        /// <summary>
        /// Gets or sets the number of days until expiration.
        /// </summary>
        public int DaysUntilExpiration { get; set; }

        /// <summary>
        /// Gets or sets the password expiration date.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }
}
