// <copyright file="RefreshTokenRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Synaxis.Api.DTOs.Authentication
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}
