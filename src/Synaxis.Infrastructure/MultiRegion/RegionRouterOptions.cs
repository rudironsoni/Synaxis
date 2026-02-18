// <copyright file="RegionRouterOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.MultiRegion
{
    using System.Collections.Generic;

    /// <summary>
    /// Configuration options for region routing.
    /// </summary>
    public class RegionRouterOptions
    {
        /// <summary>
        /// Gets or sets the default current region.
        /// </summary>
        public string DefaultRegion { get; set; } = "us-east-1";

        /// <summary>
        /// Gets or sets the fallback region when all other regions are unavailable.
        /// </summary>
        public string FallbackRegion { get; set; } = "us-east-1";

        /// <summary>
        /// Gets or sets the region endpoint mappings.
        /// Key: Region identifier (e.g., "eu-west-1", "us-east-1")
        /// Value: Base URL for the region endpoint.
        /// </summary>
        public IDictionary<string, string> RegionEndpoints { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "eu-west-1", "https://eu-west-1.synaxis.io" },
            { "us-east-1", "https://us-east-1.synaxis.io" },
            { "sa-east-1", "https://sa-east-1.synaxis.io" },
        };

        /// <summary>
        /// Gets or sets the list of countries with adequacy decisions.
        /// These countries are considered to have adequate data protection laws.
        /// </summary>
        public ISet<string> AdequacyCountries { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CH", // Switzerland
            "IL", // Israel
            "NZ", // New Zealand
            "CA", // Canada
            "JP", // Japan
            "GB", // United Kingdom
        };

        /// <summary>
        /// Gets or sets the region that requires cross-border consent for transfers.
        /// EU users require consent for transfers outside EU.
        /// </summary>
        public string EuRegion { get; set; } = "eu-west-1";

        /// <summary>
        /// Gets or sets the region identifier for Brazil (LGPD).
        /// Brazil users require consent for transfers outside Brazil.
        /// </summary>
        public string BrazilRegion { get; set; } = "sa-east-1";

        /// <summary>
        /// Gets or sets the geolocation data for regions.
        /// </summary>
        public IDictionary<string, RegionLocation> RegionLocations { get; set; } = new Dictionary<string, RegionLocation>(StringComparer.Ordinal)
        {
            {
                "eu-west-1", new RegionLocation
                {
                    CountryCode = "IE",
                    CountryName = "Ireland",
                    City = "Dublin",
                    ContinentCode = "EU",
                    TimeZone = "Europe/Dublin",
                    Latitude = 53.3498,
                    Longitude = -6.2603,
                }
            },
            {
                "us-east-1", new RegionLocation
                {
                    CountryCode = "US",
                    CountryName = "United States",
                    City = "Virginia",
                    ContinentCode = "NA",
                    TimeZone = "America/New_York",
                    Latitude = 39.0438,
                    Longitude = -77.4874,
                }
            },
            {
                "sa-east-1", new RegionLocation
                {
                    CountryCode = "BR",
                    CountryName = "Brazil",
                    City = "São Paulo",
                    ContinentCode = "SA",
                    TimeZone = "America/Sao_Paulo",
                    Latitude = -23.5505,
                    Longitude = -46.6333,
                }
            },
        };
    }
}
