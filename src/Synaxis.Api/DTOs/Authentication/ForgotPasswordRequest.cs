// <copyright file="ForgotPasswordRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Synaxis.Api.DTOs.Authentication
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
