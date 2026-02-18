// <copyright file="RegionLocation.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.MultiRegion
{
    /// <summary>
    /// Represents the geographic location of a region.
    /// </summary>
    public class RegionLocation
    {
        /// <summary>
        /// Gets or sets the country code.
        /// </summary>
        public string CountryCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the country name.
        /// </summary>
        public string CountryName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the city name.
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the continent code.
        /// </summary>
        public string ContinentCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timezone.
        /// </summary>
        public string TimeZone { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        public double Longitude { get; set; }
    }
}
