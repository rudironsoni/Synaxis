// <copyright file="VerifyEmailRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.Authentication
{
    using System.ComponentModel.DataAnnotations;

    public class VerifyEmailRequest
    {
        [Required]
        public string Token { get; set; }
    }
}
