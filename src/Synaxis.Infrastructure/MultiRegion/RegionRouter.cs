using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synaxis.Core.Contracts;
using Synaxis.Infrastructure.Data;

namespace Synaxis.Infrastructure.MultiRegion
{
    /// <summary>
    /// Routes requests to appropriate region based on user data residency.
    /// </summary>
    public class RegionRouter : IRegionRouter
    {
        private readonly SynaxisDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RegionRouter> _logger;
        private readonly string _currentRegion;

        // Regional endpoint mappings
        private static readonly Dictionary<string, string> RegionEndpoints = new Dictionary<string, string>
        {
            { "eu-west-1", "https://eu-west-1.synaxis.io" },
            { "us-east-1", "https://us-east-1.synaxis.io" },
            { "sa-east-1", "https://sa-east-1.synaxis.io" }
        };

        // Countries with adequacy decisions (simplified)
        private static readonly HashSet<string> AdequacyCountries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CH", "IL", "NZ", "CA", "JP", "GB" // Switzerland, Israel, New Zealand, Canada, Japan, UK
        };

        public RegionRouter(
            SynaxisDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<RegionRouter> logger,
            string currentRegion = "us-east-1")
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentRegion = currentRegion ?? "us-east-1";
        }

        public async Task<string> GetUserRegionAsync(Guid userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException($"User with ID '{userId}' not found");

            return user.DataResidencyRegion;
        }

        public async Task<TResponse> RouteToUserRegionAsync<TRequest, TResponse>(
            Guid userId,
            string endpoint,
            TRequest request)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("Endpoint is required", nameof(endpoint));

            var userRegion = await GetUserRegionAsync(userId);

            // If user's region matches current region, process locally
            if (userRegion == _currentRegion)
            {
                _logger.LogInformation("Processing request locally in region {Region} for user {UserId}",
                    _currentRegion, userId);
                return await ProcessLocallyAsync<TRequest, TResponse>(request);
            }

            // Check if cross-border transfer requires consent
            var requiresConsent = await RequiresCrossBorderConsentAsync(userId, userRegion);
            if (requiresConsent)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.CrossBorderConsentGiven)
                {
                    throw new UnauthorizedAccessException(
                        "Cross-border data transfer requires user consent. Please update consent settings.");
                }
            }

            // Route to user's region
            _logger.LogInformation("Routing request from {CurrentRegion} to {TargetRegion} for user {UserId}",
                _currentRegion, userRegion, userId);

            var response = await ForwardRequestToRegionAsync<TRequest, TResponse>(
                userRegion, endpoint, request);

            // Log cross-border transfer
            await LogCrossBorderTransferAsync(new CrossBorderTransferContext
            {
                UserId = userId,
                FromRegion = _currentRegion,
                ToRegion = userRegion,
                LegalBasis = DetermineLegalBasis(userRegion),
                Purpose = "API request routing",
                DataCategories = new[] { "request_data", "user_context" }
            });

            return response;
        }

        public async Task<bool> IsCrossBorderAsync(string fromRegion, string toRegion)
        {
            if (string.IsNullOrWhiteSpace(fromRegion) || string.IsNullOrWhiteSpace(toRegion))
                return false;

            await Task.CompletedTask;

            return !fromRegion.Equals(toRegion, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<TResponse> ProcessLocallyAsync<TRequest, TResponse>(TRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // This is a placeholder - actual processing would be implemented by calling service
            await Task.CompletedTask;

            _logger.LogInformation("Processing request locally in region {Region}", _currentRegion);

            // In production, this would delegate to the appropriate handler
            throw new NotImplementedException(
                "Local processing must be implemented by the calling service");
        }

        public async Task<bool> RequiresCrossBorderConsentAsync(Guid userId, string targetRegion)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return false;

            // Check if this is a cross-border transfer
            if (user.DataResidencyRegion == targetRegion)
                return false;

            // EU users always require consent for transfers outside EU
            if (user.DataResidencyRegion == "eu-west-1" && targetRegion != "eu-west-1")
                return true;

            // Brazil (LGPD) users require consent for transfers outside Brazil
            if (user.DataResidencyRegion == "sa-east-1" && targetRegion != "sa-east-1")
                return true;

            return false;
        }

        public async Task<string> GetNearestHealthyRegionAsync(string currentRegion, GeoLocation userLocation)
        {
            if (userLocation == null)
                throw new ArgumentNullException(nameof(userLocation));

            await Task.CompletedTask;

            // In production, check health endpoints for each region
            // For now, return regions in order of preference
            if (string.IsNullOrWhiteSpace(currentRegion))
                currentRegion = _currentRegion;

            // Try current region first
            if (await IsRegionHealthyAsync(currentRegion))
                return currentRegion;

            // Calculate distances and find nearest healthy region
            var regionDistances = new List<(string region, double distance)>();

            foreach (var region in RegionEndpoints.Keys)
            {
                if (region == currentRegion)
                    continue;

                if (await IsRegionHealthyAsync(region))
                {
                    var distance = CalculateDistance(userLocation, GetRegionLocation(region));
                    regionDistances.Add((region, distance));
                }
            }

            // Sort by distance and return nearest
            regionDistances.Sort((a, b) => a.distance.CompareTo(b.distance));

            if (regionDistances.Count > 0)
                return regionDistances[0].region;

            // Fallback to us-east-1 if all else fails
            return "us-east-1";
        }

        public async Task LogCrossBorderTransferAsync(CrossBorderTransferContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _logger.LogWarning(
                "Cross-border transfer: User {UserId}, Org {OrgId}, {FromRegion} -> {ToRegion}, Basis: {LegalBasis}",
                context.UserId, context.OrganizationId, context.FromRegion,
                context.ToRegion, context.LegalBasis);

            // In production, write to cross_border_transfers table
            await Task.CompletedTask;
        }

        private async Task<TResponse> ForwardRequestToRegionAsync<TRequest, TResponse>(
            string targetRegion,
            string endpoint,
            TRequest request)
        {
            if (!RegionEndpoints.TryGetValue(targetRegion, out var baseUrl))
                throw new InvalidOperationException($"Unknown region: {targetRegion}");

            var client = _httpClientFactory.CreateClient("RegionRouter");

            var fullUrl = $"{baseUrl}{endpoint}";
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Forwarding request to {Url}", fullUrl);

            try
            {
                var response = await client.PostAsync(fullUrl, content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseJson);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to forward request to {Region}", targetRegion);
                throw new InvalidOperationException(
                    $"Failed to forward request to region {targetRegion}", ex);
            }
        }

        private string DetermineLegalBasis(string targetRegion)
        {
            // Simplified legal basis determination
            if (targetRegion == "eu-west-1" || _currentRegion == "eu-west-1")
            {
                // EU transfers typically require SCC (Standard Contractual Clauses)
                return "SCC";
            }

            if (targetRegion == "sa-east-1" || _currentRegion == "sa-east-1")
            {
                // Brazil LGPD transfers
                return "consent";
            }

            // US and other regions may use adequacy decisions
            return "adequacy";
        }

        private async Task<bool> IsRegionHealthyAsync(string region)
        {
            // In production, call health check endpoint
            // For now, assume all regions are healthy
            await Task.CompletedTask;
            return true;
        }

        private GeoLocation GetRegionLocation(string region)
        {
            // Approximate datacenter locations
            return region switch
            {
                "eu-west-1" => new GeoLocation { Latitude = 53.3498, Longitude = -6.2603 }, // Dublin
                "us-east-1" => new GeoLocation { Latitude = 39.0438, Longitude = -77.4874 }, // Virginia
                "sa-east-1" => new GeoLocation { Latitude = -23.5505, Longitude = -46.6333 }, // São Paulo
                _ => new GeoLocation { Latitude = 0, Longitude = 0 }
            };
        }

        private double CalculateDistance(GeoLocation loc1, GeoLocation loc2)
        {
            if (loc1.Latitude == null || loc1.Longitude == null ||
                loc2.Latitude == null || loc2.Longitude == null)
                return double.MaxValue;

            // Haversine formula for distance calculation
            const double earthRadiusKm = 6371;

            var lat1Rad = ToRadians(loc1.Latitude.Value);
            var lat2Rad = ToRadians(loc2.Latitude.Value);
            var deltaLat = ToRadians(loc2.Latitude.Value - loc1.Latitude.Value);
            var deltaLon = ToRadians(loc2.Longitude.Value - loc1.Longitude.Value);

            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}
