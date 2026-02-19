namespace Synaxis.Contracts.V2.Common;

/// <summary>
/// Represents the status of a user in the system (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Renamed 'Deactivated' to 'Inactive' for clarity
/// - Removed 'Pending' status (users are now immediately Active or require explicit approval)
/// - Added 'PendingVerification' for email verification flow
/// </remarks>
public enum UserStatus
{
    /// <summary>
    /// User account is pending email verification.
    /// </summary>
    PendingVerification = 0,

    /// <summary>
    /// User account is active and can access the system.
    /// </summary>
    Active = 1,

    /// <summary>
    /// User account has been suspended by an administrator.
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// User account has been marked as inactive.
    /// </summary>
    Inactive = 3
}
