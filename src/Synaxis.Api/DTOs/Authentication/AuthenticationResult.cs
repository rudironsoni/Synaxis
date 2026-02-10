// <copyright file="AuthenticationResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.Authentication
{
    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public UserDto User { get; set; }
        public string ErrorMessage { get; set; }
    }
}
