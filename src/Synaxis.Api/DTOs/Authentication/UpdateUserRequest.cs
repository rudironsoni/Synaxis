// <copyright file="UpdateUserRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.Authentication
{
    public class UpdateUserRequest
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string AvatarUrl { get; set; }

        public string Timezone { get; set; }

        public string Locale { get; set; }
    }
}
