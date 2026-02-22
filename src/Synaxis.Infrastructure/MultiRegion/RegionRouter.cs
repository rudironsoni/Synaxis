// <copyright file="RegionRouter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.MultiRegion
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Synaxis.Core.Contracts;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Routes requests to appropriate region based on user data residency.
    /// </summary>
    public class RegionRouter : IRegionRouter
    {
        private readonly SynaxisDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RegionRouter> _logger;
        private readonly RegionRouterOptions _options;
        private readonly string _currentRegion;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionRouter"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The region router options.</param>
        public RegionRouter(
            SynaxisDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<RegionRouter> logger,
            IOptions<RegionRouterOptions> options)
        {
            ArgumentNullException.ThrowIfNull(context);
            this._context = context;
            ArgumentNullException.ThrowIfNull(httpClientFactory);
            this._httpClientFactory = httpClientFactory;
            ArgumentNullException.ThrowIfNull(logger);
            this._logger = logger;
            ArgumentNullException.ThrowIfNull(options);
            this._options = options.Value;
            this._currentRegion = this._options.DefaultRegion;
        }

        /// <inheritdoc/>
        public async Task<string> GetUserRegionAsync(Guid userId)
        {
            var user = await this._context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException($"User with ID '{userId}' not found");
            }

            return user.DataResidencyRegion;
        }

        /// <inheritdoc/>
        public async Task<TResponse> RouteToUserRegionAsync<TRequest, TResponse>(
            Guid userId,
            string endpoint,
            TRequest request)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("Endpoint is required", nameof(endpoint));
            }

            var userRegion = await this.GetUserRegionAsync(userId).ConfigureAwait(false);

            // If user's region matches current region, process locally
            if (string.Equals(userRegion, this._currentRegion, StringComparison.Ordinal))
            {
                this._logger.LogInformation("Processing request locally in region {Region} for user {UserId}", this._currentRegion, userId);
                return await this.ProcessLocallyAsync<TRequest, TResponse>(request).ConfigureAwait(false);
            }

            // Check if cross-border transfer requires consent
            var requiresConsent = await this.RequiresCrossBorderConsentAsync(userId, userRegion).ConfigureAwait(false);
            if (requiresConsent)
            {
                var user = await this._context.Users.FindAsync(userId).ConfigureAwait(false);
                if (user == null || !user.CrossBorderConsentGiven)
                {
                    throw new UnauthorizedAccessException(
                        "Cross-border data transfer requires user consent. Please update consent settings.");
                }
            }

            // Route to user's region
            this._logger.LogInformation("Routing request from {CurrentRegion} to {TargetRegion} for user {UserId}", this._currentRegion, userRegion, userId);

            var response = await this.ForwardRequestToRegionAsync<TRequest, TResponse>(
                userRegion, endpoint, request).ConfigureAwait(false);

            // Log cross-border transfer
            await this.LogCrossBorderTransferAsync(new CrossBorderTransferContext
            {
                UserId = userId,
                FromRegion = this._currentRegion,
                ToRegion = userRegion,
                LegalBasis = this.DetermineLegalBasis(userRegion),
                Purpose = "API request routing",
                DataCategories = new[] { "request_data", "user_context" },
            }).ConfigureAwait(false);

            return response;
        }

        /// <inheritdoc/>
        public async Task<bool> IsCrossBorderAsync(string fromRegion, string toRegion)
        {
            if (string.IsNullOrWhiteSpace(fromRegion) || string.IsNullOrWhiteSpace(toRegion))
            {
                return false;
            }

            await Task.CompletedTask.ConfigureAwait(false);

            return !fromRegion.Equals(toRegion, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public async Task<TResponse> ProcessLocallyAsync<TRequest, TResponse>(TRequest request)
        {
            if (object.Equals(request, default(TRequest)))
            {
                throw new ArgumentNullException(nameof(request));
            }

            // This is a placeholder - actual processing would be implemented by calling service
            await Task.CompletedTask.ConfigureAwait(false);

            this._logger.LogInformation("Processing request locally in region {Region}", this._currentRegion);

            // In production, this would delegate to the appropriate handler
            throw new NotSupportedException(
                "Local processing must be implemented by the calling service");
        }

        /// <inheritdoc/>
        public async Task<bool> RequiresCrossBorderConsentAsync(Guid userId, string targetRegion)
        {
            var user = await this._context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false);

            if (user == null)
            {
                return false;
            }

            // Check if this is a cross-border transfer
            if (string.Equals(user.DataResidencyRegion, targetRegion, StringComparison.Ordinal))
            {
                return false;
            }

            // EU users always require consent for transfers outside EU
            if (string.Equals(user.DataResidencyRegion, this._options.EuRegion, StringComparison.Ordinal) && !string.Equals(targetRegion, this._options.EuRegion, StringComparison.Ordinal))
            {
                return true;
            }

            // Brazil (LGPD) users require consent for transfers outside Brazil
            if (string.Equals(user.DataResidencyRegion, this._options.BrazilRegion, StringComparison.Ordinal) && !string.Equals(targetRegion, this._options.BrazilRegion, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task<string> GetNearestHealthyRegionAsync(string currentRegion, GeoLocation userLocation)
        {
            ArgumentNullException.ThrowIfNull(userLocation);
            await Task.CompletedTask.ConfigureAwait(false);

            // In production, check health endpoints for each region
            // For now, return regions in order of preference
            if (string.IsNullOrWhiteSpace(currentRegion))
            {
                currentRegion = this._currentRegion;
            }

            // Try current region first
            if (await IsRegionHealthyAsync().ConfigureAwait(false))
            {
                return currentRegion;
            }

            // Calculate distances and find nearest healthy region
            var regionDistances = new List<(string Region, double Distance)>();

            foreach (var region in this._options.RegionEndpoints.Keys)
            {
                if (string.Equals(region, currentRegion, StringComparison.Ordinal))
                {
                    continue;
                }

                if (await IsRegionHealthyAsync().ConfigureAwait(false))
                {
                    var distance = RegionRouter.CalculateDistance(userLocation, this.GetRegionLocation(region));
                    regionDistances.Add((region, distance));
                }
            }

            // Sort by distance and return nearest
            regionDistances.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            if (regionDistances.Count > 0)
            {
                return regionDistances[0].Region;
            }

            // Fallback to default region if all else fails
            return this._options.FallbackRegion;
        }

        /// <inheritdoc/>
        public Task LogCrossBorderTransferAsync(CrossBorderTransferContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this._logger.LogWarning("Cross-border transfer: User {UserId}, Org {OrgId}, {FromRegion} -> {ToRegion}, Basis: {LegalBasis}", context.UserId, context.OrganizationId, context.FromRegion, context.ToRegion, context.LegalBasis);

            // In production, write to cross_border_transfers table
            return Task.CompletedTask;
        }

        private async Task<TResponse> ForwardRequestToRegionAsync<TRequest, TResponse>(
            string targetRegion,
            string endpoint,
            TRequest request)
        {
            if (!this._options.RegionEndpoints.TryGetValue(targetRegion, out var baseUrl))
            {
                throw new InvalidOperationException($"Unknown region: {targetRegion}");
            }

            using var client = this._httpClientFactory.CreateClient("RegionRouter");

            var fullUrl = $"{baseUrl}{endpoint}";
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            this._logger.LogInformation("Forwarding request to {Url}", fullUrl);

            try
            {
                using var response = await client.PostAsync(fullUrl, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<TResponse>(responseJson);
                if (EqualityComparer<TResponse>.Default.Equals(result, default))
                {
                    throw new InvalidOperationException("Failed to deserialize response from remote region");
                }

                return result!;
            }
            catch (HttpRequestException ex)
            {
                this._logger.LogError(ex, "Failed to forward request to {Region}", targetRegion);
                throw new InvalidOperationException(
                    $"Failed to forward request to region {targetRegion}", ex);
            }
        }

        private string DetermineLegalBasis(string targetRegion)
        {
            // Simplified legal basis determination
            if (string.Equals(targetRegion, this._options.EuRegion, StringComparison.Ordinal) || string.Equals(this._currentRegion, this._options.EuRegion, StringComparison.Ordinal))
            {
                // EU transfers typically require SCC (Standard Contractual Clauses)
                return "SCC";
            }

            if (string.Equals(targetRegion, this._options.BrazilRegion, StringComparison.Ordinal) || string.Equals(this._currentRegion, this._options.BrazilRegion, StringComparison.Ordinal))
            {
                // Brazil LGPD transfers
                return "consent";
            }

            // US and other regions may use adequacy decisions
            return "adequacy";
        }

        private static async Task<bool> IsRegionHealthyAsync()
        {
            // In production, call health check endpoint
            // For now, assume all regions are healthy
            await Task.CompletedTask.ConfigureAwait(false);
            return true;
        }

        private GeoLocation GetRegionLocation(string region)
        {
            if (this._options.RegionLocations.TryGetValue(region, out var location))
            {
                return new GeoLocation
                {
                    IpAddress = string.Empty,
                    CountryCode = location.CountryCode,
                    CountryName = location.CountryName,
                    City = location.City,
                    ContinentCode = location.ContinentCode,
                    TimeZone = location.TimeZone,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                };
            }

            // Return unknown location if region not configured
            return new GeoLocation
            {
                IpAddress = string.Empty,
                CountryCode = "Unknown",
                CountryName = "Unknown",
                City = "Unknown",
                ContinentCode = "Unknown",
                TimeZone = "UTC",
                Latitude = 0,
                Longitude = 0,
            };
        }

        private static double CalculateDistance(GeoLocation loc1, GeoLocation loc2)
        {
            if (loc1.Latitude == null || loc1.Longitude == null ||
                loc2.Latitude == null || loc2.Longitude == null)
            {
                return double.MaxValue;
            }

            // Haversine formula for distance calculation
            const double earthRadiusKm = 6371;

            var lat1Rad = ToRadians(loc1.Latitude.Value);
            var lat2Rad = ToRadians(loc2.Latitude.Value);
            var deltaLat = ToRadians(loc2.Latitude.Value - loc1.Latitude.Value);
            var deltaLon = ToRadians(loc2.Longitude.Value - loc1.Longitude.Value);

            var a = (Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)) +
                    (Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2));

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}
