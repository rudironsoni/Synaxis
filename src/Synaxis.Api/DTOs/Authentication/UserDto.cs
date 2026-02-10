// <copyright file="UserDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.Authentication
{
    using System;

    public class UserDto
    {
        public Guid Id { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string FullName => $"{this.FirstName} {this.LastName}".Trim();

        public string AvatarUrl { get; set; }

        public DateTime? EmailVerifiedAt { get; set; }

        public bool EmailVerified => this.EmailVerifiedAt.HasValue;

        public string Role { get; set; }

        public bool MfaEnabled { get; set; }
    }
}
