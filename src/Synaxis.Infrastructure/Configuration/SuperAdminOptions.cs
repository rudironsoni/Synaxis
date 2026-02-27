// <copyright file="SuperAdminOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Configuration
{
    using System.Collections.Generic;

    /// <summary>
    /// Configuration options for SuperAdmin service.
    /// </summary>
    public class SuperAdminOptions
    {
        /// <summary>
        /// Gets or sets the default current region.
        /// </summary>
        public string DefaultRegion { get; set; } = "us-east-1";

        /// <summary>
        /// Gets or sets the region endpoint mappings for cross-region operations.
        /// Key: Region identifier (e.g., "eu-west-1", "us-east-1")
        /// Value: API endpoint URL for the region.
        /// </summary>
        public IDictionary<string, string> RegionEndpoints { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "us-east-1", "https://api-us.synaxis.io" },
            { "eu-west-1", "https://api-eu.synaxis.io" },
            { "sa-east-1", "https://api-br.synaxis.io" },
        };

        /// <summary>
        /// Gets or sets the allowed IP ranges for SuperAdmin access.
        /// </summary>
        public ISet<string> AllowedIpRanges { get; set; } = new HashSet<string>(StringComparer.Ordinal)
        {
            "10.0.0.0/8",
            "172.16.0.0/12",
            "192.168.0.0/16",
        };

        /// <summary>
        /// Gets or sets the start hour for business hours (24-hour format).
        /// </summary>
        public int BusinessHoursStart { get; set; } = 8;

        /// <summary>
        /// Gets or sets the end hour for business hours (24-hour format).
        /// </summary>
        public int BusinessHoursEnd { get; set; } = 18;

        /// <summary>
        /// Gets or sets the HTTP client timeout in seconds for cross-region requests.
        /// </summary>
        public int HttpClientTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// Gets or sets the token secret key for HMAC operations.
        /// This should be set via configuration and not hardcoded.
        /// </summary>
        public string TokenSecret { get; set; } = string.Empty;
    }
}
