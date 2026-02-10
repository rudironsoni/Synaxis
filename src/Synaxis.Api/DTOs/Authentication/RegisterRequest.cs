// <copyright file="RegisterRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Synaxis.Api.DTOs.Authentication
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string OrganizationName { get; set; }

        [Required]
        public string DataResidencyRegion { get; set; }
    }
}
