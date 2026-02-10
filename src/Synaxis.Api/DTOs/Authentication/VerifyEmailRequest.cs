// <copyright file="VerifyEmailRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Synaxis.Api.DTOs.Authentication
{
    public class VerifyEmailRequest
    {
        [Required]
        public string Token { get; set; }
    }
}
