using System.Threading.Tasks;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.Core.Contracts;
using Synaxis.Infrastructure.Services;
using Xunit;

namespace Synaxis.Tests.Unit
{
    public class GeoIPServiceTests
    {
        private readonly IGeoIPService _service;
        
        public GeoIPServiceTests()
        {
            _service = new GeoIPService();
        }
        
        [Fact]
        public async Task GetLocationAsync_ValidIpAddress_ReturnsLocation()
        {
            // Arrange
            var ipAddress = "127.0.0.1";
            
            // Act
            var result = await _service.GetLocationAsync(ipAddress);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(ipAddress, result.IpAddress);
            Assert.NotNull(result.CountryCode);
            Assert.NotNull(result.CountryName);
        }
        
        [Theory]
        [InlineData("127.0.0.1", "US")]
        [InlineData("10.0.0.1", "US")]
        [InlineData("192.168.1.1", "US")]
        public async Task GetLocationAsync_LocalIpAddress_ReturnsUSLocation(string ipAddress, string expectedCountry)
        {
            // Act
            var result = await _service.GetLocationAsync(ipAddress);
            
            // Assert
            Assert.Equal(expectedCountry, result.CountryCode);
        }
        
        [Theory]
        [InlineData("DE", "eu-west-1")]
        [InlineData("FR", "eu-west-1")]
        [InlineData("ES", "eu-west-1")]
        [InlineData("IT", "eu-west-1")]
        [InlineData("NL", "eu-west-1")]
        [InlineData("BR", "sa-east-1")]
        [InlineData("AR", "sa-east-1")]
        [InlineData("CL", "sa-east-1")]
        [InlineData("US", "us-east-1")]
        [InlineData("CA", "us-east-1")]
        public async Task GetRegionForCountryAsync_ValidCountry_ReturnsCorrectRegion(
            string countryCode, string expectedRegion)
        {
            // Act
            var result = await _service.GetRegionForCountryAsync(countryCode);
            
            // Assert
            Assert.Equal(expectedRegion, result);
        }
        
        [Fact]
        public async Task GetRegionForIpAsync_ValidIp_ReturnsRegion()
        {
            // Arrange
            var ipAddress = "127.0.0.1";
            
            // Act
            var result = await _service.GetRegionForIpAsync(ipAddress);
            
            // Assert
            Assert.NotNull(result);
            Assert.Contains(result, new[] { "eu-west-1", "us-east-1", "sa-east-1" });
        }
        
        [Theory]
        [InlineData("DE")]
        [InlineData("FR")]
        [InlineData("ES")]
        [InlineData("IT")]
        [InlineData("NL")]
        [InlineData("PT")]
        public async Task RequiresDataResidencyAsync_EuCountry_ReturnsTrue(string countryCode)
        {
            // Act
            var result = await _service.RequiresDataResidencyAsync(countryCode);
            
            // Assert
            Assert.True(result);
        }
        
        [Theory]
        [InlineData("BR")]
        [InlineData("RU")]
        [InlineData("CN")]
        [InlineData("IN")]
        public async Task RequiresDataResidencyAsync_DataResidencyCountry_ReturnsTrue(string countryCode)
        {
            // Act
            var result = await _service.RequiresDataResidencyAsync(countryCode);
            
            // Assert
            Assert.True(result);
        }
        
        [Theory]
        [InlineData("US")]
        [InlineData("CA")]
        [InlineData("AU")]
        [InlineData("JP")]
        public async Task RequiresDataResidencyAsync_NoDataResidency_ReturnsFalse(string countryCode)
        {
            // Act
            var result = await _service.RequiresDataResidencyAsync(countryCode);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public async Task GetLocationAsync_InvalidIpAddress_ThrowsException()
        {
            // Arrange
            string ipAddress = null;
            
            // Act & Assert
            await Assert.ThrowsAsync<System.ArgumentException>(
                () => _service.GetLocationAsync(ipAddress));
        }
        
        [Fact]
        public async Task GetRegionForCountryAsync_InvalidCountry_ThrowsException()
        {
            // Arrange
            string countryCode = null;
            
            // Act & Assert
            await Assert.ThrowsAsync<System.ArgumentException>(
                () => _service.GetRegionForCountryAsync(countryCode));
        }
    }
}
