// <copyright file="LogoutRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Synaxis.Api.DTOs.Authentication
{
    public class LogoutRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}
