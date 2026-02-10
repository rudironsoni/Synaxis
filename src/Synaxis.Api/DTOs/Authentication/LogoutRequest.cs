// <copyright file="LogoutRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.Authentication
{
    using System.ComponentModel.DataAnnotations;

    public class LogoutRequest
    {
        [Required]
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the JWT access token to invalidate.
        /// </summary>
        public string AccessToken { get; set; }
    }
}
