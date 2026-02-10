// <copyright file="RefreshTokenRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.Authentication
{
    using System.ComponentModel.DataAnnotations;

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}
