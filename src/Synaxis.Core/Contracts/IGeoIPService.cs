// <copyright file="IGeoIPService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System.Threading.Tasks;

    /// <summary>
    /// Service for IP to location lookup.
    /// </summary>
    public interface IGeoIPService
    {
        /// <summary>
        /// Get location from IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address to lookup.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the geo location.</returns>
        Task<GeoLocation> GetLocationAsync(string ipAddress);

        /// <summary>
        /// Get region based on country code.
        /// </summary>
        /// <param name="countryCode">The country code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the region identifier.</returns>
        Task<string> GetRegionForCountryAsync(string countryCode);

        /// <summary>
        /// Get region based on IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address to lookup.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the region identifier.</returns>
        Task<string> GetRegionForIpAsync(string ipAddress);

        /// <summary>
        /// Check if IP is in EU region.
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether IP is in EU.</returns>
        Task<bool> IsEuRegionAsync(string ipAddress);

        /// <summary>
        /// Check if country requires data residency.
        /// </summary>
        /// <param name="countryCode">The country code to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether residency is required.</returns>
        Task<bool> RequiresDataResidencyAsync(string countryCode);
    }
}
