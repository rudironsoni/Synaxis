using System;
using System.Collections.Generic;

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Core
{
    public class IdentityAccount
    {
        public string Id { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }
}
