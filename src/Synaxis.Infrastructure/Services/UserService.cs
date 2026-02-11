using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;

namespace Synaxis.Infrastructure.Services
{
    /// <summary>
    /// Service for managing users with data residency compliance.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly SynaxisDbContext _context;

        public UserService(SynaxisDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<User> CreateUserAsync(CreateUserRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Email is required", nameof(request));

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Password is required", nameof(request));

            if (string.IsNullOrWhiteSpace(request.DataResidencyRegion))
                throw new ArgumentException("Data residency region is required", nameof(request));

            if (string.IsNullOrWhiteSpace(request.CreatedInRegion))
                throw new ArgumentException("Created in region is required", nameof(request));

            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
                throw new InvalidOperationException($"User with email '{request.Email}' already exists");

            // Verify organization exists
            var organization = await _context.Organizations.FindAsync(request.OrganizationId);
            if (organization == null)
                throw new InvalidOperationException($"Organization with ID '{request.OrganizationId}' not found");

            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                Email = request.Email.ToLowerInvariant(),
                PasswordHash = HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = request.Role ?? "member",
                DataResidencyRegion = request.DataResidencyRegion,
                CreatedInRegion = request.CreatedInRegion,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User> GetUserAsync(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.Organization)
                .Include(u => u.TeamMemberships)
                .ThenInclude(tm => tm.Team)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                throw new InvalidOperationException($"User with ID '{id}' not found");

            return user;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required", nameof(email));

            var user = await _context.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

            if (user == null)
                throw new InvalidOperationException($"User with email '{email}' not found");

            return user;
        }

        public async Task<User> UpdateUserAsync(Guid id, UpdateUserRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var user = await _context.Users.FindAsync(id);

            if (user == null)
                throw new InvalidOperationException($"User with ID '{id}' not found");

            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName;

            if (!string.IsNullOrWhiteSpace(request.LastName))
                user.LastName = request.LastName;

            if (!string.IsNullOrWhiteSpace(request.Timezone))
                user.Timezone = request.Timezone;

            if (!string.IsNullOrWhiteSpace(request.Locale))
                user.Locale = request.Locale;

            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                user.AvatarUrl = request.AvatarUrl;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return false;

            // Soft delete: set IsActive to false
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<User> AuthenticateAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required", nameof(email));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required", nameof(password));

            var user = await _context.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

            if (user == null)
                throw new UnauthorizedAccessException("Invalid email or password");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("User account is not active");

            // Check if account is locked
            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
                throw new UnauthorizedAccessException($"Account is locked until {user.LockedUntil.Value}");

            if (!VerifyPassword(password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;

                // Lock account after 5 failed attempts
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                }

                await _context.SaveChangesAsync();

                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Reset failed attempts on successful login
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            user.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<MfaSetupResult> SetupMfaAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new InvalidOperationException($"User with ID '{userId}' not found");

            // Generate TOTP secret using Otp.NET
            var key = OtpNet.KeyGeneration.GenerateRandomKey(20);
            var secret = OtpNet.Base32Encoding.ToString(key);

            user.MfaSecret = secret;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Generate QR code URL for authenticator apps
            var issuer = "Synaxis";
            var accountName = user.Email;
            var qrCodeUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

            // Generate QR code as base64 image
            var qrCodeImage = GenerateQrCodeImage(qrCodeUri);

            return new MfaSetupResult
            {
                Secret = secret,
                QrCodeUrl = qrCodeUri,
                QrCodeImage = qrCodeImage,
                ManualEntryKey = FormatSecretForManualEntry(secret)
            };
        }

        public async Task<MfaEnableResult> EnableMfaAsync(Guid userId, string totpCode)
        {
            if (string.IsNullOrWhiteSpace(totpCode))
                throw new ArgumentException("TOTP code is required", nameof(totpCode));

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new InvalidOperationException($"User with ID '{userId}' not found");

            if (string.IsNullOrWhiteSpace(user.MfaSecret))
                throw new InvalidOperationException("MFA setup not completed. Call SetupMfaAsync first.");

            // Verify the TOTP code using Otp.NET
            var key = OtpNet.Base32Encoding.ToBytes(user.MfaSecret);
            var totp = new OtpNet.Totp(key);
            if (!totp.VerifyTotp(totpCode, out var timeWindowUsed))
                throw new InvalidOperationException("Invalid TOTP code");

            // Generate backup codes
            var backupCodes = GenerateBackupCodes();

            // Store backup codes (hashed) in the user record
            user.MfaBackupCodes = string.Join(",", backupCodes.Select(BCrypt.Net.BCrypt.HashPassword));
            user.MfaEnabled = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new MfaEnableResult
            {
                Success = true,
                BackupCodes = backupCodes.ToArray(),
                ErrorMessage = "MFA enabled successfully. Please save your backup codes in a secure location."
            };
        }

        public async Task<bool> DisableMfaAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new InvalidOperationException($"User with ID '{userId}' not found");

            user.MfaEnabled = false;
            user.MfaSecret = null;
            user.MfaBackupCodes = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DisableMfaAsync(Guid userId, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code is required", nameof(code));

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new InvalidOperationException($"User with ID '{userId}' not found");

            if (!user.MfaEnabled)
                throw new InvalidOperationException("MFA is not enabled for this user");

            // Try TOTP code first using Otp.NET
            if (!string.IsNullOrWhiteSpace(user.MfaSecret))
            {
                var key = OtpNet.Base32Encoding.ToBytes(user.MfaSecret);
                var totp = new OtpNet.Totp(key);
                if (totp.VerifyTotp(code, out var timeWindowUsed))
                {
                    user.MfaEnabled = false;
                    user.MfaSecret = null;
                    user.MfaBackupCodes = null;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    return true;
                }
            }

            // Try backup code
            if (!string.IsNullOrWhiteSpace(user.MfaBackupCodes))
            {
                var hashedBackupCodes = user.MfaBackupCodes.Split(',');
                foreach (var hashedCode in hashedBackupCodes)
                {
                    if (BCrypt.Net.BCrypt.Verify(code, hashedCode))
                    {
                        // Remove the used backup code
                        var remainingCodes = hashedBackupCodes.Where(c => c != hashedCode).ToArray();
                        user.MfaBackupCodes = remainingCodes.Length > 0 ? string.Join(",", remainingCodes) : null;

                        // Disable MFA
                        user.MfaEnabled = false;
                        user.MfaSecret = null;
                        user.UpdatedAt = DateTime.UtcNow;

                        await _context.SaveChangesAsync();

                        return true;
                    }
                }
            }

            return false;
        }

        public async Task<bool> VerifyMfaCodeAsync(Guid userId, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var user = await _context.Users.FindAsync(userId);

            if (user == null || !user.MfaEnabled || string.IsNullOrWhiteSpace(user.MfaSecret))
                return false;

            // Verify TOTP code using Otp.NET
            var key = OtpNet.Base32Encoding.ToBytes(user.MfaSecret);
            var totp = new OtpNet.Totp(key);
            return totp.VerifyTotp(code, out var timeWindowUsed);
        }

        public async Task<bool> UpdateCrossBorderConsentAsync(Guid userId, bool consentGiven, string version)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new InvalidOperationException($"User with ID '{userId}' not found");

            user.CrossBorderConsentGiven = consentGiven;
            user.CrossBorderConsentDate = consentGiven ? DateTime.UtcNow : null;
            user.CrossBorderConsentVersion = consentGiven ? version : null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> HasCrossBorderConsentAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return false;

            return user.CrossBorderConsentGiven;
        }

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            // Using BCrypt.Net-Next library
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        public bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        private IList<string> GenerateBackupCodes()
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

        private string FormatSecretForManualEntry(string secret)
        {
            // Format as groups of 4 characters for easier manual entry
            var formatted = new StringBuilder();
            for (int i = 0; i < secret.Length; i += 4)
            {
                if (i > 0) formatted.Append(' ');
                formatted.Append(secret.Substring(i, Math.Min(4, secret.Length - i)));
            }

            return formatted.ToString();
        }

        private string GenerateQrCodeImage(string qrCodeUri)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);

            return Convert.ToBase64String(qrCodeBytes);
        }
    }
}
