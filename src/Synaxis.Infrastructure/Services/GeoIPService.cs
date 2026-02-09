using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Synaxis.Core.Contracts;

namespace Synaxis.Infrastructure.Services
{
    /// <summary>
    /// Service for IP to location lookup
    /// Mock implementation - in production would use MaxMind GeoIP2 or similar.
    /// </summary>
    public class GeoIPService : IGeoIPService
    {
        // EU countries for GDPR compliance
        private static readonly HashSet<string> EuCountries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR",
            "DE", "GR", "HU", "IE", "IT", "LV", "LT", "LU", "MT", "NL",
            "PL", "PT", "RO", "SK", "SI", "ES", "SE"
        };

        // Countries requiring data residency
        private static readonly HashSet<string> DataResidencyCountries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BR", // Brazil (LGPD)
            "RU", // Russia
            "CN", // China
            "IN" // India
        };

        // Mock IP to country mapping for testing
        private static readonly Dictionary<string, string> MockIpDatabase = new Dictionary<string, string>
        {
            { "127.0.0.1", "US" },
            { "::1", "US" },
            { "192.168.1.1", "US" },
            { "10.0.0.1", "US" }
        };

        public async Task<GeoLocation> GetLocationAsync(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IP address is required", nameof(ipAddress));

            // Mock implementation - in production, use MaxMind GeoIP2
            await Task.CompletedTask;

            var countryCode = GetMockCountryCode(ipAddress);
            var location = new GeoLocation
            {
                IpAddress = ipAddress,
                CountryCode = countryCode,
                CountryName = GetCountryName(countryCode),
                City = GetMockCity(countryCode),
                ContinentCode = GetContinentCode(countryCode),
                TimeZone = GetMockTimeZone(countryCode)
            };

            // Set mock coordinates
            SetMockCoordinates(location, countryCode);

            return location;
        }

        public async Task<string> GetRegionForCountryAsync(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                throw new ArgumentException("Country code is required", nameof(countryCode));

            await Task.CompletedTask;

            // EU countries -> eu-west-1
            if (EuCountries.Contains(countryCode))
                return "eu-west-1";

            // Brazil and South America -> sa-east-1
            if (countryCode == "BR" || countryCode == "AR" || countryCode == "CL" ||
                countryCode == "CO" || countryCode == "PE" || countryCode == "VE" ||
                countryCode == "EC" || countryCode == "UY" || countryCode == "PY" || countryCode == "BO")
                return "sa-east-1";

            // Default to US East
            return "us-east-1";
        }

        public async Task<string> GetRegionForIpAsync(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IP address is required", nameof(ipAddress));

            var location = await GetLocationAsync(ipAddress);
            return await GetRegionForCountryAsync(location.CountryCode);
        }

        public async Task<bool> IsEuRegionAsync(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            var location = await GetLocationAsync(ipAddress);
            return EuCountries.Contains(location.CountryCode);
        }

        public async Task<bool> RequiresDataResidencyAsync(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return false;

            await Task.CompletedTask;
            return DataResidencyCountries.Contains(countryCode) || EuCountries.Contains(countryCode);
        }

        private string GetMockCountryCode(string ipAddress)
        {
            if (MockIpDatabase.TryGetValue(ipAddress, out var countryCode))
                return countryCode;

            // Parse IP to determine mock country
            // This is a very simplified mock implementation
            if (ipAddress.StartsWith("192.") || ipAddress.StartsWith("10.") || ipAddress.StartsWith("172."))
                return "US"; // Private IPs default to US

            // Simple heuristic based on first octet for mock data
            var parts = ipAddress.Split('.');
            if (parts.Length >= 1 && int.TryParse(parts[0], out var firstOctet))
            {
                if (firstOctet >= 0 && firstOctet < 85)
                    return "US";
                else if (firstOctet >= 85 && firstOctet < 170)
                    return "DE"; // EU
                else
                    return "BR"; // South America
            }

            return "US"; // Default
        }

        private string GetCountryName(string countryCode)
        {
            var countryNames = new Dictionary<string, string>
            {
                { "US", "United States" },
                { "GB", "United Kingdom" },
                { "DE", "Germany" },
                { "FR", "France" },
                { "BR", "Brazil" },
                { "ES", "Spain" },
                { "IT", "Italy" },
                { "NL", "Netherlands" },
                { "PT", "Portugal" },
                { "AR", "Argentina" },
                { "CL", "Chile" },
                { "CO", "Colombia" }
            };

            return countryNames.TryGetValue(countryCode, out var name) ? name : countryCode;
        }

        private string GetMockCity(string countryCode)
        {
            var cities = new Dictionary<string, string>
            {
                { "US", "New York" },
                { "GB", "London" },
                { "DE", "Frankfurt" },
                { "FR", "Paris" },
                { "BR", "São Paulo" },
                { "ES", "Madrid" },
                { "IT", "Rome" },
                { "NL", "Amsterdam" },
                { "PT", "Lisbon" }
            };

            return cities.TryGetValue(countryCode, out var city) ? city : "Unknown";
        }

        private string GetContinentCode(string countryCode)
        {
            if (EuCountries.Contains(countryCode) || countryCode == "GB")
                return "EU";

            if (countryCode == "US" || countryCode == "CA" || countryCode == "MX")
                return "NA";

            if (countryCode == "BR" || countryCode == "AR" || countryCode == "CL" ||
                countryCode == "CO" || countryCode == "PE")
                return "SA";

            return "UN";
        }

        private string GetMockTimeZone(string countryCode)
        {
            var timeZones = new Dictionary<string, string>
            {
                { "US", "America/New_York" },
                { "GB", "Europe/London" },
                { "DE", "Europe/Berlin" },
                { "FR", "Europe/Paris" },
                { "BR", "America/Sao_Paulo" },
                { "ES", "Europe/Madrid" },
                { "IT", "Europe/Rome" },
                { "NL", "Europe/Amsterdam" },
                { "PT", "Europe/Lisbon" }
            };

            return timeZones.TryGetValue(countryCode, out var tz) ? tz : "UTC";
        }

        private void SetMockCoordinates(GeoLocation location, string countryCode)
        {
            var coordinates = new Dictionary<string, (double lat, double lon)>
            {
                { "US", (40.7128, -74.0060) },      // New York
                { "GB", (51.5074, -0.1278) },        // London
                { "DE", (50.1109, 8.6821) },         // Frankfurt
                { "FR", (48.8566, 2.3522) },         // Paris
                { "BR", (-23.5505, -46.6333) },      // São Paulo
                { "ES", (40.4168, -3.7038) },        // Madrid
                { "IT", (41.9028, 12.4964) },        // Rome
                { "NL", (52.3676, 4.9041) },         // Amsterdam
                { "PT", (38.7223, -9.1393) } // Lisbon
            };

            if (coordinates.TryGetValue(countryCode, out var coords))
            {
                location.Latitude = coords.lat;
                location.Longitude = coords.lon;
            }
        }
    }
}
