using System;
using System.Threading.Tasks;
using Synaxis.Core.Models;

namespace Synaxis.Core.Contracts
{
    /// <summary>
    /// Service for managing users with data residency compliance
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Create a new user
        /// </summary>
        Task<User> CreateUserAsync(CreateUserRequest request);
        
        /// <summary>
        /// Get user by ID
        /// </summary>
        Task<User> GetUserAsync(Guid id);
        
        /// <summary>
        /// Get user by email
        /// </summary>
        Task<User> GetUserByEmailAsync(string email);
        
        /// <summary>
        /// Update user profile
        /// </summary>
        Task<User> UpdateUserAsync(Guid id, UpdateUserRequest request);
        
        /// <summary>
        /// Delete user (soft delete)
        /// </summary>
        Task<bool> DeleteUserAsync(Guid id);
        
        /// <summary>
        /// Authenticate user with email and password
        /// </summary>
        Task<User> AuthenticateAsync(string email, string password);
        
        /// <summary>
        /// Setup MFA for user
        /// </summary>
        Task<MfaSetupResult> SetupMfaAsync(Guid userId);
        
        /// <summary>
        /// Enable MFA for user
        /// </summary>
        Task<bool> EnableMfaAsync(Guid userId, string totpCode);
        
        /// <summary>
        /// Disable MFA for user
        /// </summary>
        Task<bool> DisableMfaAsync(Guid userId);
        
        /// <summary>
        /// Verify MFA code
        /// </summary>
        Task<bool> VerifyMfaCodeAsync(Guid userId, string code);
        
        /// <summary>
        /// Update cross-border consent
        /// </summary>
        Task<bool> UpdateCrossBorderConsentAsync(Guid userId, bool consentGiven, string version);
        
        /// <summary>
        /// Check if user has given cross-border consent
        /// </summary>
        Task<bool> HasCrossBorderConsentAsync(Guid userId);
        
        /// <summary>
        /// Hash password using BCrypt
        /// </summary>
        string HashPassword(string password);
        
        /// <summary>
        /// Verify password against hash
        /// </summary>
        bool VerifyPassword(string password, string hash);
    }
    
    public class CreateUserRequest
    {
        public Guid OrganizationId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; } = "member";
        public string DataResidencyRegion { get; set; }
        public string CreatedInRegion { get; set; }
    }
    
    public class UpdateUserRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Timezone { get; set; }
        public string Locale { get; set; }
        public string AvatarUrl { get; set; }
    }
    
    public class MfaSetupResult
    {
        public string Secret { get; set; }
        public string QrCodeUrl { get; set; }
        public string ManualEntryKey { get; set; }
    }
}
