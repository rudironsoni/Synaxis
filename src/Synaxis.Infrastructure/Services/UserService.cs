// <copyright file="UserService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using OtpNet;
    using QRCoder;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for managing users with data residency compliance.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly SynaxisDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public UserService(SynaxisDbContext context)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<User> CreateUserAsync(CreateUserRequest request)
        {
            ValidateCreateUserRequest(request);
            await this.ValidateUserDoesNotExistAsync(request.Email).ConfigureAwait(false);
            await this.ValidateOrganizationExistsAsync(request.OrganizationId).ConfigureAwait(false);

            var user = this.CreateUserEntity(request);
            this._context.Users.Add(user);
            await this._context.SaveChangesAsync().ConfigureAwait(false);

            return user;
        }

        private static void ValidateCreateUserRequest(CreateUserRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Password is required", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.DataResidencyRegion))
            {
                throw new ArgumentException("Data residency region is required", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.CreatedInRegion))
            {
                throw new ArgumentException("Created in region is required", nameof(request));
            }
        }

        private async Task ValidateUserDoesNotExistAsync(string email)
        {
            var existingUser = await this._context.Users
                .FirstOrDefaultAsync(u => u.Email == email).ConfigureAwait(false);

            if (existingUser != null)
            {
                throw new InvalidOperationException($"User with email '{email}' already exists");
            }
        }

        private async Task ValidateOrganizationExistsAsync(Guid organizationId)
        {
            var organization = await this._context.Organizations.FindAsync(organizationId).ConfigureAwait(false);
            if (organization == null)
            {
                throw new InvalidOperationException($"Organization with ID '{organizationId}' not found");
            }
        }

        private User CreateUserEntity(CreateUserRequest request)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                Email = request.Email.ToLowerInvariant(),
                PasswordHash = this.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = request.Role ?? "member",
                DataResidencyRegion = request.DataResidencyRegion,
                CreatedInRegion = request.CreatedInRegion,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }

        /// <inheritdoc/>
        public async Task<User> GetUserAsync(Guid id)
        {
            var user = await this._context.Users
                .Include(u => u.Organization)
                .Include(u => u.TeamMemberships)
                .ThenInclude(tm => tm.Team)
                .FirstOrDefaultAsync(u => u.Id == id).ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException($"User with ID '{id}' not found");
            }

            return user;
        }

        /// <inheritdoc/>
        public async Task<User> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email is required", nameof(email));
            }

            var user = await this._context.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant()).ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException($"User with email '{email}' not found");
            }

            return user;
        }

        /// <inheritdoc/>
        public async Task<User> UpdateUserAsync(Guid id, UpdateUserRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var user = await this._context.Users.FindAsync(id).ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException($"User with ID '{id}' not found");
            }

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

            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            {
                user.AvatarUrl = request.AvatarUrl;
            }

            user.UpdatedAt = DateTime.UtcNow;

            await this._context.SaveChangesAsync().ConfigureAwait(false);

            return user;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await this._context.Users.FindAsync(id).ConfigureAwait(false);

            if (user == null)
            {
                return false;
            }

            // Soft delete: set IsActive to false
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await this._context.SaveChangesAsync().ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Disables multi-factor authentication for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if MFA was disabled successfully.</returns>
        public async Task<bool> DisableMfaAsync(Guid userId)
        {
            var user = await this._context.Users.FindAsync(userId).ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException($"User with ID '{userId}' not found");
            }

            if (!user.MfaEnabled)
            {
                throw new InvalidOperationException("MFA is not enabled for this user");
            }

            user.MfaEnabled = false;
            user.MfaSecret = null;
            user.UpdatedAt = DateTime.UtcNow;

            await this._context.SaveChangesAsync().ConfigureAwait(false);

            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> VerifyMfaCodeAsync(Guid userId, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var user = await this._context.Users.FindAsync(userId).ConfigureAwait(false);

            if (user == null || !user.MfaEnabled || string.IsNullOrWhiteSpace(user.MfaSecret))
            {
                return false;
            }

            // Verify TOTP code using Otp.NET
            var key = Base32Encoding.ToBytes(user.MfaSecret);
            var totp = new Totp(key);
            return totp.VerifyTotp(code, out _);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateCrossBorderConsentAsync(Guid userId, bool consentGiven, string version)
        {
            var user = await this._context.Users.FindAsync(userId).ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException($"User with ID '{userId}' not found");
            }

            user.CrossBorderConsentGiven = consentGiven;
            user.CrossBorderConsentDate = consentGiven ? DateTime.UtcNow : null;
            user.CrossBorderConsentVersion = consentGiven ? version : null;
            user.UpdatedAt = DateTime.UtcNow;

            await this._context.SaveChangesAsync().ConfigureAwait(false);

            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> HasCrossBorderConsentAsync(Guid userId)
        {
            var user = await this._context.Users.FindAsync(userId).ConfigureAwait(false);

            if (user == null)
            {
                return false;
            }

            return user.CrossBorderConsentGiven;
        }

        /// <inheritdoc/>
        public async Task<User> AuthenticateAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email is required", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password is required", nameof(password));
            }

            var user = await this._context.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant()).ConfigureAwait(false);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            if (!this.VerifyPassword(password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("User account is not active");
            }

            return user;
        }

        /// <inheritdoc/>
        public async Task<MfaSetupResult> SetupMfaAsync(Guid userId)
        {
            var user = await this._context.Users.FindAsync(userId).ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException($"User with ID '{userId}' not found");
            }

            if (user.MfaEnabled)
            {
                throw new InvalidOperationException("MFA is already enabled for this user");
            }

            var key = KeyGeneration.GenerateRandomKey(20);
            var secret = Base32Encoding.ToString(key);

            user.MfaSecret = secret;
            user.UpdatedAt = DateTime.UtcNow;

            await this._context.SaveChangesAsync().ConfigureAwait(false);

            var issuer = "Synaxis";
            var account = user.Email;
            var qrCodeUrl = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

            return new MfaSetupResult
            {
                Secret = secret,
                QrCodeUrl = qrCodeUrl,
                ManualEntryKey = secret,
            };
        }

        /// <inheritdoc/>
        public async Task<bool> EnableMfaAsync(Guid userId, string totpCode)
        {
            if (string.IsNullOrWhiteSpace(totpCode))
            {
                throw new ArgumentException("TOTP code is required", nameof(totpCode));
            }

            var user = await this._context.Users.FindAsync(userId).ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException($"User with ID '{userId}' not found");
            }

            if (string.IsNullOrWhiteSpace(user.MfaSecret))
            {
                throw new InvalidOperationException("MFA setup not completed. Please call SetupMfaAsync first.");
            }

            var key = Base32Encoding.ToBytes(user.MfaSecret);
            var totp = new Totp(key);

            if (!totp.VerifyTotp(totpCode, out _))
            {
                return false;
            }

            user.MfaEnabled = true;
            user.UpdatedAt = DateTime.UtcNow;

            await this._context.SaveChangesAsync().ConfigureAwait(false);

            return true;
        }

        /// <inheritdoc/>
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be empty", nameof(password));
            }

            // Using BCrypt.Net-Next library
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        /// <inheritdoc/>
        public bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        private static IList<string> GenerateBackupCodes()
        {
            var codes = new List<string>();
            var chars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"; // No ambiguous characters (0, O, 1, I)

            for (int i = 0; i < 10; i++)
            {
                var code = new char[8];
                for (int j = 0; j < 8; j++)
                {
                    code[j] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
                }

                codes.Add(new string(code));
            }

            return codes;
        }

        private static string FormatSecretForManualEntry(string secret)
        {
            // Format as groups of 4 characters for easier manual entry
            var formatted = new StringBuilder();
            for (int i = 0; i < secret.Length; i += 4)
            {
                if (i > 0)
                {
                    formatted.Append(' ');
                }

                formatted.Append(secret.Substring(i, Math.Min(4, secret.Length - i)));
            }

            return formatted.ToString();
        }

        private static string GenerateQrCodeImage(string qrCodeUri)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);

            return Convert.ToBase64String(qrCodeBytes);
        }
    }
}
