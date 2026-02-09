using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

            // Generate TOTP secret (Base32 encoded)
            var secret = GenerateTotpSecret();

            user.MfaSecret = secret;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Generate QR code URL for authenticator apps
            var issuer = "Synaxis";
            var accountName = user.Email;
            var qrCodeUrl = $"otpauth://totp/{issuer}:{accountName}?secret={secret}&issuer={issuer}";

            return new MfaSetupResult
            {
                Secret = secret,
                QrCodeUrl = qrCodeUrl,
                ManualEntryKey = FormatSecretForManualEntry(secret)
            };
        }

        public async Task<bool> EnableMfaAsync(Guid userId, string totpCode)
        {
            if (string.IsNullOrWhiteSpace(totpCode))
                throw new ArgumentException("TOTP code is required", nameof(totpCode));

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new InvalidOperationException($"User with ID '{userId}' not found");

            if (string.IsNullOrWhiteSpace(user.MfaSecret))
                throw new InvalidOperationException("MFA setup not completed. Call SetupMfaAsync first.");

            // Verify the TOTP code
            if (!VerifyTotpCode(user.MfaSecret, totpCode))
                throw new InvalidOperationException("Invalid TOTP code");

            user.MfaEnabled = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DisableMfaAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new InvalidOperationException($"User with ID '{userId}' not found");

            user.MfaEnabled = false;
            user.MfaSecret = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> VerifyMfaCodeAsync(Guid userId, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var user = await _context.Users.FindAsync(userId);

            if (user == null || !user.MfaEnabled || string.IsNullOrWhiteSpace(user.MfaSecret))
                return false;

            return VerifyTotpCode(user.MfaSecret, code);
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

        private string GenerateTotpSecret()
        {
            // Generate 20 random bytes (160 bits) for TOTP secret
            var bytes = new byte[20];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return Base32Encode(bytes);
        }

        private string Base32Encode(byte[] data)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new StringBuilder();

            for (int i = 0; i < data.Length; i += 5)
            {
                int byteCount = Math.Min(5, data.Length - i);
                ulong buffer = 0;

                for (int j = 0; j < byteCount; j++)
                {
                    buffer = (buffer << 8) | data[i + j];
                }

                int bitCount = byteCount * 8;
                while (bitCount > 0)
                {
                    int index = (int)((buffer >> (bitCount - 5)) & 0x1F);
                    result.Append(alphabet[index]);
                    bitCount -= 5;
                }
            }

            return result.ToString();
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

        private bool VerifyTotpCode(string secret, string code)
        {
            // Simple TOTP verification (30-second window)
            var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timeStep = unixTime / 30;

            // Check current window and ±1 window for clock drift
            for (long i = -1; i <= 1; i++)
            {
                var generatedCode = GenerateTotpCode(secret, timeStep + i);
                if (generatedCode == code)
                    return true;
            }

            return false;
        }

        private string GenerateTotpCode(string secret, long timeStep)
        {
            var key = Base32Decode(secret);
            var timeBytes = BitConverter.GetBytes(timeStep);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(timeBytes);

            using (var hmac = new HMACSHA1(key))
            {
                var hash = hmac.ComputeHash(timeBytes);
                var offset = hash[hash.Length - 1] & 0x0F;

                var binary = ((hash[offset] & 0x7F) << 24)
                    | ((hash[offset + 1] & 0xFF) << 16)
                    | ((hash[offset + 2] & 0xFF) << 8)
                    | (hash[offset + 3] & 0xFF);

                var otp = binary % 1000000;
                return otp.ToString("D6");
            }
        }

        private byte[] Base32Decode(string input)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            input = input.ToUpperInvariant().Replace(" ", "");

            var bytes = new System.Collections.Generic.List<byte>();
            ulong buffer = 0;
            int bitsInBuffer = 0;

            foreach (char c in input)
            {
                int value = alphabet.IndexOf(c);
                if (value < 0)
                    continue;

                buffer = (buffer << 5) | (uint)value;
                bitsInBuffer += 5;

                if (bitsInBuffer >= 8)
                {
                    bytes.Add((byte)(buffer >> (bitsInBuffer - 8)));
                    bitsInBuffer -= 8;
                }
            }

            return bytes.ToArray();
        }
    }
}
