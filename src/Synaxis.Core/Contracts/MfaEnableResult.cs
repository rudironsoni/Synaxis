// <copyright file="MfaEnableResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    /// <summary>
    /// Represents the result of MFA enable.
    /// </summary>
    public class MfaEnableResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether MFA was enabled successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the backup codes for account recovery.
        /// </summary>
        public required string[] BackupCodes { get; set; }

        /// <summary>
        /// Gets or sets the error message if enabling MFA failed.
        /// </summary>
        public required string ErrorMessage { get; set; }
    }
}
