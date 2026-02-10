// <copyright file="MfaEnableRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.Authentication
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Request to enable MFA.
    /// </summary>
    public class MfaEnableRequest
    {
        /// <summary>
        /// Gets or sets the TOTP code.
        /// </summary>
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; }
    }
}
