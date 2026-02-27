// <copyright file="PasswordService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for managing password policies and password operations.
    /// </summary>
    public class PasswordService : IPasswordService
    {
        private readonly SynaxisDbContext _context;
        private readonly IUserService _userService;

        // Common weak passwords list (subset for demonstration)
        private static readonly HashSet<string> CommonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "123456", "12345678", "qwerty", "abc123", "monkey", "master",
            "dragon", "111111", "baseball", "iloveyou", "trustno1", "sunshine", "princess",
            "admin", "welcome", "shadow", "ashley", "football", "jesus", "michael", "ninja",
            "mustang", "password1", "password123", "letmein", "login", "starwars",
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="userService">The user service.</param>
        public PasswordService(SynaxisDbContext context, IUserService userService)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <inheritdoc/>
        public async Task<PasswordValidationResult> ValidatePasswordAsync(Guid userId, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return new PasswordValidationResult
                {
                    IsValid = false,
                    StrengthScore = 0,
                    StrengthLevel = "Invalid",
                    Errors = new List<string> { "Password is required" },
                };
            }

            var user = await this._context.Users
                .Include(u => u.Organization)
                .ThenInclude(o => o!.PasswordPolicy)
                .FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false);

            if (user == null)
            {
                return new PasswordValidationResult
                {
                    IsValid = false,
                    StrengthScore = 0,
                    StrengthLevel = "Invalid",
                    Errors = new List<string> { "User not found" },
                };
            }

            var policy = user.Organization?.PasswordPolicy ?? this.GetDefaultPolicy();
            var result = new PasswordValidationResult
            {
                IsValid = true,
                StrengthLevel = "Unknown",
                Errors = new List<string>(),
                Warnings = new List<string>(),
            };

            PasswordService.ValidatePasswordRequirements(password, policy, result);
            PasswordService.ValidateCommonPasswords(password, policy, result);
            PasswordService.ValidateUserInfoInPassword(password, user, policy, result);
            await this.ValidatePasswordHistoryAsync(userId, password, policy, result).ConfigureAwait(false);

            result.StrengthScore = this.GetPasswordStrength(password);
            result.StrengthLevel = GetStrengthLevel(result.StrengthScore);

            AddStrengthWarnings(result);

            return result;
        }

        private static void ValidatePasswordRequirements(string password, PasswordPolicy policy, PasswordValidationResult result)
        {
            if (password.Length < policy.MinLength)
            {
                result.IsValid = false;
                result.Errors.Add($"Password must be at least {policy.MinLength} characters long");
            }

            if (policy.RequireUppercase && !password.Any(char.IsUpper))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one uppercase letter");
            }

            if (policy.RequireLowercase && !password.Any(char.IsLower))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one lowercase letter");
            }

            if (policy.RequireNumbers && !password.Any(char.IsDigit))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one number");
            }

            if (policy.RequireSpecialCharacters && !password.Any(c => !char.IsLetterOrDigit(c)))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one special character");
            }
        }

        private static void ValidateCommonPasswords(string password, PasswordPolicy policy, PasswordValidationResult result)
        {
            if (policy.BlockCommonPasswords && CommonPasswords.Contains(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password is too common. Please choose a stronger password");
            }
        }

        private static void ValidateUserInfoInPassword(string password, User user, PasswordPolicy policy, PasswordValidationResult result)
        {
            if (!policy.BlockUserInfoInPassword)
            {
                return;
            }

            var emailLocal = user.Email.Split('@')[0].ToLowerInvariant();
            var firstName = user.FirstName?.ToLowerInvariant() ?? string.Empty;
            var lastName = user.LastName?.ToLowerInvariant() ?? string.Empty;

            var passwordLower = password.ToLowerInvariant();
            if (!string.IsNullOrEmpty(emailLocal) && passwordLower.Contains(emailLocal))
            {
                result.IsValid = false;
                result.Errors.Add("Password must not contain your email address");
            }

            if (!string.IsNullOrEmpty(firstName) && passwordLower.Contains(firstName))
            {
                result.IsValid = false;
                result.Errors.Add("Password must not contain your first name");
            }

            if (!string.IsNullOrEmpty(lastName) && passwordLower.Contains(lastName))
            {
                result.IsValid = false;
                result.Errors.Add("Password must not contain your last name");
            }
        }

        private async Task ValidatePasswordHistoryAsync(Guid userId, string password, PasswordPolicy policy, PasswordValidationResult result)
        {
            if (policy.PasswordHistoryCount <= 0)
            {
                return;
            }

            var recentPasswords = await this._context.PasswordHistories
                .Where(ph => ph.UserId == userId)
                .OrderByDescending(ph => ph.SetAt)
                .Take(policy.PasswordHistoryCount)
                .ToListAsync()
                .ConfigureAwait(false);

            if (recentPasswords.Any(historyEntry => this._userService.VerifyPassword(password, historyEntry.PasswordHash)))
            {
                result.IsValid = false;
                result.Errors.Add($"You cannot reuse your last {policy.PasswordHistoryCount} passwords");
            }
        }

        private static void AddStrengthWarnings(PasswordValidationResult result)
        {
            if (result.StrengthScore < 50)
            {
                result.Warnings.Add("Password strength is weak. Consider using a stronger password");
            }
            else if (result.StrengthScore < 70)
            {
                result.Warnings.Add("Password strength is fair. Consider adding more complexity");
            }
        }

        /// <inheritdoc/>
        public async Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await this._context.Users
                .Include(u => u.Organization)
                .ThenInclude(o => o!.PasswordPolicy)
                .FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false);

            if (user == null)
            {
                return new ChangePasswordResult
                {
                    Success = false,
                    ErrorMessage = "User not found",
                };
            }

            var policy = user.Organization?.PasswordPolicy ?? this.GetDefaultPolicy();

            if (user.PasswordChangeLockedUntil.HasValue && user.PasswordChangeLockedUntil.Value > DateTime.UtcNow)
            {
                return new ChangePasswordResult
                {
                    Success = false,
                    ErrorMessage = $"Password change is locked until {user.PasswordChangeLockedUntil.Value}",
                };
            }

            if (!this._userService.VerifyPassword(currentPassword, user.PasswordHash))
            {
                return await this.HandleFailedPasswordChangeAsync(user, policy).ConfigureAwait(false);
            }

            var validationResult = await this.ValidatePasswordAsync(userId, newPassword).ConfigureAwait(false);
            if (!validationResult.IsValid)
            {
                return new ChangePasswordResult
                {
                    Success = false,
                    ErrorMessage = string.Join("; ", validationResult.Errors),
                };
            }

            if (this._userService.VerifyPassword(newPassword, user.PasswordHash))
            {
                return new ChangePasswordResult
                {
                    Success = false,
                    ErrorMessage = "New password must be different from current password",
                };
            }

            await this.UpdatePasswordAsync(user, newPassword, policy).ConfigureAwait(false);

            return new ChangePasswordResult
            {
                Success = true,
                PasswordExpiresAt = user.PasswordExpiresAt,
            };
        }

        private async Task<ChangePasswordResult> HandleFailedPasswordChangeAsync(User user, PasswordPolicy policy)
        {
            user.FailedPasswordChangeAttempts++;

            if (user.FailedPasswordChangeAttempts >= policy.MaxFailedChangeAttempts)
            {
                user.PasswordChangeLockedUntil = DateTime.UtcNow.AddMinutes(policy.LockoutDurationMinutes);
                await this._context.SaveChangesAsync().ConfigureAwait(false);
                return new ChangePasswordResult
                {
                    Success = false,
                    ErrorMessage = $"Too many failed attempts. Password change locked for {policy.LockoutDurationMinutes} minutes",
                };
            }

            await this._context.SaveChangesAsync().ConfigureAwait(false);
            return new ChangePasswordResult
            {
                Success = false,
                ErrorMessage = "Current password is incorrect",
            };
        }

        private async Task UpdatePasswordAsync(User user, string newPassword, PasswordPolicy policy)
        {
            var historyEntry = new PasswordHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PasswordHash = user.PasswordHash,
                SetAt = DateTime.UtcNow,
            };
            this._context.PasswordHistories.Add(historyEntry);

            var oldHistoryEntries = await this._context.PasswordHistories
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.SetAt)
                .Skip(policy.PasswordHistoryCount + 5)
                .ToListAsync()
                .ConfigureAwait(false);
            this._context.PasswordHistories.RemoveRange(oldHistoryEntries);

            user.PasswordHash = this._userService.HashPassword(newPassword);
            user.FailedPasswordChangeAttempts = 0;
            user.PasswordChangeLockedUntil = null;

            if (policy.PasswordExpirationDays > 0)
            {
                user.PasswordExpiresAt = DateTime.UtcNow.AddDays(policy.PasswordExpirationDays);
            }
            else
            {
                user.PasswordExpiresAt = null;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await this._context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ResetPasswordResult> ResetPasswordAsync(Guid userId, string newPassword)
        {
            var user = await this._context.Users
                .Include(u => u.Organization)
                .ThenInclude(o => o!.PasswordPolicy)
                .FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false);

            if (user == null)
            {
                return new ResetPasswordResult
                {
                    Success = false,
                    ErrorMessage = "User not found",
                };
            }

            var policy = user.Organization?.PasswordPolicy ?? this.GetDefaultPolicy();

            var validationResult = await this.ValidatePasswordAsync(userId, newPassword).ConfigureAwait(false);
            if (!validationResult.IsValid)
            {
                return new ResetPasswordResult
                {
                    Success = false,
                    ErrorMessage = string.Join("; ", validationResult.Errors),
                };
            }

            await this.ResetPasswordInternalAsync(user, newPassword, policy).ConfigureAwait(false);

            return new ResetPasswordResult
            {
                Success = true,
                PasswordExpiresAt = user.PasswordExpiresAt,
            };
        }

        private async Task ResetPasswordInternalAsync(User user, string newPassword, PasswordPolicy policy)
        {
            var historyEntry = new PasswordHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PasswordHash = user.PasswordHash,
                SetAt = DateTime.UtcNow,
            };
            this._context.PasswordHistories.Add(historyEntry);

            user.PasswordHash = this._userService.HashPassword(newPassword);
            user.FailedPasswordChangeAttempts = 0;
            user.PasswordChangeLockedUntil = null;

            if (policy.PasswordExpirationDays > 0)
            {
                user.PasswordExpiresAt = DateTime.UtcNow.AddDays(policy.PasswordExpirationDays);
            }
            else
            {
                user.PasswordExpiresAt = null;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await this._context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PasswordPolicy> GetPasswordPolicyAsync(Guid organizationId)
        {
            var policy = await this._context.PasswordPolicies
                .FirstOrDefaultAsync(p => p.OrganizationId == organizationId).ConfigureAwait(false);

            if (policy == null)
            {
                // Create default policy for organization
                policy = this.GetDefaultPolicy();
                policy.OrganizationId = organizationId;
                this._context.PasswordPolicies.Add(policy);
                await this._context.SaveChangesAsync().ConfigureAwait(false);
            }

            return policy;
        }

        /// <inheritdoc/>
        public async Task<PasswordPolicy> UpdatePasswordPolicyAsync(Guid organizationId, PasswordPolicy policy)
        {
            var existingPolicy = await this._context.PasswordPolicies
                .FirstOrDefaultAsync(p => p.OrganizationId == organizationId).ConfigureAwait(false);

            if (existingPolicy == null)
            {
                policy.Id = Guid.NewGuid();
                policy.OrganizationId = organizationId;
                policy.CreatedAt = DateTime.UtcNow;
                policy.UpdatedAt = DateTime.UtcNow;
                this._context.PasswordPolicies.Add(policy);
            }
            else
            {
                existingPolicy.MinLength = policy.MinLength;
                existingPolicy.RequireUppercase = policy.RequireUppercase;
                existingPolicy.RequireLowercase = policy.RequireLowercase;
                existingPolicy.RequireNumbers = policy.RequireNumbers;
                existingPolicy.RequireSpecialCharacters = policy.RequireSpecialCharacters;
                existingPolicy.PasswordHistoryCount = policy.PasswordHistoryCount;
                existingPolicy.PasswordExpirationDays = policy.PasswordExpirationDays;
                existingPolicy.PasswordExpirationWarningDays = policy.PasswordExpirationWarningDays;
                existingPolicy.MaxFailedChangeAttempts = policy.MaxFailedChangeAttempts;
                existingPolicy.LockoutDurationMinutes = policy.LockoutDurationMinutes;
                existingPolicy.BlockCommonPasswords = policy.BlockCommonPasswords;
                existingPolicy.BlockUserInfoInPassword = policy.BlockUserInfoInPassword;
                existingPolicy.UpdatedAt = DateTime.UtcNow;
                policy = existingPolicy;
            }

            await this._context.SaveChangesAsync().ConfigureAwait(false);
            return policy;
        }

        /// <inheritdoc/>
        public async Task<PasswordExpirationStatus> CheckPasswordExpirationAsync(Guid userId)
        {
            var user = await this._context.Users
                .Include(u => u.Organization)
                .ThenInclude(o => o!.PasswordPolicy)
                .FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false);

            if (user == null)
            {
                return new PasswordExpirationStatus
                {
                    IsExpired = false,
                    IsExpiringSoon = false,
                    DaysUntilExpiration = -1,
                };
            }

            var policy = user.Organization?.PasswordPolicy ?? this.GetDefaultPolicy();
            var status = new PasswordExpirationStatus
            {
                ExpiresAt = user.PasswordExpiresAt,
            };

            if (user.PasswordExpiresAt.HasValue)
            {
                var daysUntilExpiration = (user.PasswordExpiresAt.Value - DateTime.UtcNow).Days;
                status.DaysUntilExpiration = daysUntilExpiration;

                if (daysUntilExpiration <= 0)
                {
                    status.IsExpired = true;
                    status.IsExpiringSoon = true;
                }
                else if (daysUntilExpiration <= policy.PasswordExpirationWarningDays)
                {
                    status.IsExpiringSoon = true;
                }
            }

            return status;
        }

        /// <inheritdoc/>
        public int GetPasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return 0;
            }

            int score = 0;

            // Length score (up to 25 points)
            score += Math.Min(25, password.Length * 2);

            // Character variety (up to 25 points)
            bool hasLower = password.Any(char.IsLower);
            bool hasUpper = password.Any(char.IsUpper);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            int varietyCount = (hasLower ? 1 : 0) + (hasUpper ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSpecial ? 1 : 0);
            score += varietyCount * 6;

            // Pattern detection (deduct points for common patterns)
            if (Regex.IsMatch(password, @"(?<ch>.)\k<ch>{2,}", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
            {
                // Repeated characters
                score -= 10;
            }

            if (Regex.IsMatch(password, @"(012|123|234|345|456|567|678|789|890|abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)", RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100)))
            {
                score -= 10;
            }

            if (Regex.IsMatch(password, @"(qwe|asd|zxc)", RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100)))
            {
                // Keyboard patterns
                score -= 10;
            }

            // Bonus for length > 12
            if (password.Length > 12)
            {
                score += 10;
            }

            // Bonus for mixed case + numbers + special
            if (hasLower && hasUpper && hasDigit && hasSpecial)
            {
                score += 10;
            }

            return Math.Max(0, Math.Min(100, score));
        }

        private static string GetStrengthLevel(int score)
        {
            return score switch
            {
                < 20 => "Very Weak",
                < 40 => "Weak",
                < 60 => "Fair",
                < 80 => "Good",
                _ => "Strong",
            };
        }

        private PasswordPolicy GetDefaultPolicy()
        {
            return new PasswordPolicy
            {
                Id = Guid.NewGuid(),
                MinLength = 12,
                RequireUppercase = true,
                RequireLowercase = true,
                RequireNumbers = true,
                RequireSpecialCharacters = true,
                PasswordHistoryCount = 5,
                PasswordExpirationDays = 90,
                PasswordExpirationWarningDays = 14,
                MaxFailedChangeAttempts = 5,
                LockoutDurationMinutes = 15,
                BlockCommonPasswords = true,
                BlockUserInfoInPassword = true,
            };
        }
    }
}
