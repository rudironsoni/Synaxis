// <copyright file="LoginRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Synaxis.Api.DTOs.Authentication
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
