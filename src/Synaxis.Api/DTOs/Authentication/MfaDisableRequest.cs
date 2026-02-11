// <copyright file="MfaDisableRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.Authentication
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Request to disable MFA.
    /// </summary>
    public class MfaDisableRequest
    {
        /// <summary>
        /// Gets or sets the TOTP code or backup code.
        /// </summary>
        [Required]
        [StringLength(8, MinimumLength = 6)]
        public string Code { get; set; }
    }
}
