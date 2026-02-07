// <copyright file="IRegionRouter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Threading.Tasks;
    using Synaxis.Core.Models;

    /// <summary>
    /// Routes requests to appropriate region based on user data residency.
    /// </summary>
    public interface IRegionRouter
    {
        /// <summary>
        /// Get the region where a user's data is stored.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the region identifier.</returns>
        Task<string> GetUserRegionAsync(Guid userId);

        /// <summary>
        /// Route request to user's region.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="endpoint">The endpoint to route to.</param>
        /// <param name="request">The request to route.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response.</returns>
        Task<TResponse> RouteToUserRegionAsync<TRequest, TResponse>(
            Guid userId,
            string endpoint,
            TRequest request);

        /// <summary>
        /// Check if routing would be cross-border.
        /// </summary>
        /// <param name="fromRegion">The source region.</param>
        /// <param name="toRegion">The target region.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether routing is cross-border.</returns>
        Task<bool> IsCrossBorderAsync(string fromRegion, string toRegion);

        /// <summary>
        /// Process request locally (current region).
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The request to process.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response.</returns>
        Task<TResponse> ProcessLocallyAsync<TRequest, TResponse>(TRequest request);

        /// <summary>
        /// Check if cross-border consent is required.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="targetRegion">The target region.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether consent is required.</returns>
        Task<bool> RequiresCrossBorderConsentAsync(Guid userId, string targetRegion);

        /// <summary>
        /// Get nearest healthy region for failover.
        /// </summary>
        /// <param name="currentRegion">The current region.</param>
        /// <param name="userLocation">The user's location.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the nearest healthy region.</returns>
        Task<string> GetNearestHealthyRegionAsync(string currentRegion, GeoLocation userLocation);

        /// <summary>
        /// Log cross-border transfer for compliance.
        /// </summary>
        /// <param name="context">The cross-border transfer context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task LogCrossBorderTransferAsync(CrossBorderTransferContext context);
    }

    /// <summary>
    /// Represents the context for a cross-border transfer.
    /// </summary>
    public class CrossBorderTransferContext
    {
        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the source region.
        /// </summary>
        public string FromRegion { get; set; }

        /// <summary>
        /// Gets or sets the destination region.
        /// </summary>
        public string ToRegion { get; set; }

        /// <summary>
        /// Gets or sets the legal basis (SCC, consent, adequacy).
        /// </summary>
        public string LegalBasis { get; set; }

        /// <summary>
        /// Gets or sets the purpose of transfer.
        /// </summary>
        public string Purpose { get; set; }

        /// <summary>
        /// Gets or sets the data categories being transferred.
        /// </summary>
        public string[] DataCategories { get; set; }
    }

    /// <summary>
    /// Represents a geographic location.
    /// </summary>
    public class GeoLocation
    {
        /// <summary>
        /// Gets or sets the IP address.
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the country code.
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Gets or sets the country name.
        /// </summary>
        public string CountryName { get; set; }

        /// <summary>
        /// Gets or sets the city.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Gets or sets the continent code.
        /// </summary>
        public string ContinentCode { get; set; }

        /// <summary>
        /// Gets or sets the time zone.
        /// </summary>
        public string TimeZone { get; set; }
    }
}
