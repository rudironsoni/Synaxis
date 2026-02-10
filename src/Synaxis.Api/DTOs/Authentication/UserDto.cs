// <copyright file="UserDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;

namespace Synaxis.Api.DTOs.Authentication
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
        public bool EmailVerified { get; set; }
        public string Role { get; set; }
    }
}
