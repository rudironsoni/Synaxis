// <copyright file="RegisterRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.Authentication
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class RegisterRequest
    {
        [Required]
        public Guid? OrganizationId { get; set; }

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

        public string DataResidencyRegion { get; set; }

        public string CreatedInRegion { get; set; }
    }
}
