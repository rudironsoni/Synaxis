using System;
using System.Linq;
using Synaxis.Common.Tests.Infrastructure;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.InferenceGateway.Infrastructure.Compliance;
using Xunit;

namespace Synaxis.Tests.Compliance
{
    public class ComplianceProviderFactoryTests : IDisposable
    {
        private readonly ComplianceProviderFactory _factory;
        private readonly InferenceGateway.Infrastructure.ControlPlane.SynaxisDbContext _dbContext;

        public ComplianceProviderFactoryTests()
        {
            _dbContext = InMemorySynaxisDbContext.Create();
            _factory = new ComplianceProviderFactory(_dbContext);
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        [Fact]
        public void Constructor_WithNullDbContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ComplianceProviderFactory(null));
        }

        [Theory]
        [InlineData("EU", "GDPR")]
        [InlineData("eu-west-1", "GDPR")]
        [InlineData("eu-central-1", "GDPR")]
        [InlineData("eu-north-1", "GDPR")]
        [InlineData("eu-south-1", "GDPR")]
        public void GetProvider_WithEURegion_ShouldReturnGdprProvider(string region, string expectedRegulationCode)
        {
            // Act
            var provider = _factory.GetProvider(region);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal(expectedRegulationCode, provider.RegulationCode);
        }

        [Theory]
        [InlineData("BR", "LGPD")]
        [InlineData("sa-east-1", "LGPD")]
        [InlineData("br-south-1", "LGPD")]
        [InlineData("sa-saopaulo-1", "LGPD")]
        public void GetProvider_WithBrazilianRegion_ShouldReturnLgpdProvider(string region, string expectedRegulationCode)
        {
            // Act
            var provider = _factory.GetProvider(region);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal(expectedRegulationCode, provider.RegulationCode);
        }

        [Theory]
        [InlineData("us-east-1")]
        [InlineData("us-west-2")]
        [InlineData("ap-southeast-1")]
        [InlineData("unknown-region")]
        public void GetProvider_WithNonRegisteredRegion_ShouldReturnDefaultProvider(string region)
        {
            // Act
            var provider = _factory.GetProvider(region);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal("GDPR", provider.RegulationCode); // Default is GDPR
        }

        [Fact]
        public void GetProvider_WithNullRegion_ShouldReturnDefaultProvider()
        {
            // Act
            var provider = _factory.GetProvider(null);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal("GDPR", provider.RegulationCode);
        }

        [Fact]
        public void GetProvider_WithEmptyRegion_ShouldReturnDefaultProvider()
        {
            // Act
            var provider = _factory.GetProvider("");

            // Assert
            Assert.NotNull(provider);
            Assert.Equal("GDPR", provider.RegulationCode);
        }

        [Theory]
        [InlineData("EU-WEST-1", "GDPR")]
        [InlineData("Sa-East-1", "LGPD")]
        [InlineData("BR", "LGPD")]
        public void GetProvider_ShouldBeCaseInsensitive(string region, string expectedRegulationCode)
        {
            // Act
            var provider = _factory.GetProvider(region);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal(expectedRegulationCode, provider.RegulationCode);
        }

        [Theory]
        [InlineData("GDPR", "GDPR")]
        [InlineData("gdpr", "GDPR")]
        [InlineData("LGPD", "LGPD")]
        [InlineData("lgpd", "LGPD")]
        public void GetProviderByRegulation_WithValidCode_ShouldReturnCorrectProvider(
            string regulationCode, string expectedCode)
        {
            // Act
            var provider = _factory.GetProviderByRegulation(regulationCode);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal(expectedCode, provider.RegulationCode);
        }

        [Fact]
        public void GetProviderByRegulation_WithInvalidCode_ShouldReturnDefaultProvider()
        {
            // Act
            var provider = _factory.GetProviderByRegulation("CCPA");

            // Assert
            Assert.NotNull(provider);
            Assert.Equal("GDPR", provider.RegulationCode); // Default
        }

        [Fact]
        public void GetProviderByRegulation_WithNullCode_ShouldReturnDefaultProvider()
        {
            // Act
            var provider = _factory.GetProviderByRegulation(null);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal("GDPR", provider.RegulationCode);
        }

        [Fact]
        public void GetAllProviders_ShouldReturnAllUniqueProviders()
        {
            // Act
            var providers = _factory.GetAllProviders().ToList();

            // Assert
            Assert.NotNull(providers);
            Assert.Equal(2, providers.Count); // GDPR and LGPD
            Assert.Contains(providers, p => p.RegulationCode == "GDPR");
            Assert.Contains(providers, p => p.RegulationCode == "LGPD");
        }

        [Fact]
        public void RegisterProvider_WithValidParameters_ShouldRegisterProvider()
        {
            // Arrange
            var customProvider = new GdprComplianceProvider(_dbContext);
            var customRegion = "custom-region-1";

            // Act
            _factory.RegisterProvider(customRegion, customProvider);
            var retrievedProvider = _factory.GetProvider(customRegion);

            // Assert
            Assert.NotNull(retrievedProvider);
            Assert.Same(customProvider, retrievedProvider);
        }

        [Fact]
        public void RegisterProvider_WithNullRegion_ShouldThrowArgumentException()
        {
            // Arrange
            var provider = new GdprComplianceProvider(_dbContext);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _factory.RegisterProvider(null, provider));
        }

        [Fact]
        public void RegisterProvider_WithEmptyRegion_ShouldThrowArgumentException()
        {
            // Arrange
            var provider = new GdprComplianceProvider(_dbContext);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _factory.RegisterProvider("", provider));
        }

        [Fact]
        public void RegisterProvider_WithNullProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _factory.RegisterProvider("test-region", null));
        }

        [Fact]
        public void RegisterProvider_ShouldOverwriteExistingProvider()
        {
            // Arrange
            var originalProvider = _factory.GetProvider("eu-west-1");
            var newProvider = new LgpdComplianceProvider(_dbContext);

            // Act
            _factory.RegisterProvider("eu-west-1", newProvider);
            var retrievedProvider = _factory.GetProvider("eu-west-1");

            // Assert
            Assert.NotSame(originalProvider, retrievedProvider);
            Assert.Same(newProvider, retrievedProvider);
            Assert.Equal("LGPD", retrievedProvider.RegulationCode);
        }

        [Theory]
        [InlineData("eu-west-1", true)]
        [InlineData("sa-east-1", true)]
        [InlineData("EU", true)]
        [InlineData("BR", true)]
        [InlineData("us-east-1", false)]
        [InlineData("unknown-region", false)]
        [InlineData(null, false)]
        [InlineData("", false)]
        public void HasProvider_ShouldReturnCorrectResult(string region, bool expected)
        {
            // Act
            var result = _factory.HasProvider(region);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void HasProvider_AfterRegisteringProvider_ShouldReturnTrue()
        {
            // Arrange
            var customRegion = "custom-region-test";
            var provider = new GdprComplianceProvider(_dbContext);
            
            Assert.False(_factory.HasProvider(customRegion));

            // Act
            _factory.RegisterProvider(customRegion, provider);

            // Assert
            Assert.True(_factory.HasProvider(customRegion));
        }

        [Fact]
        public void GetProvider_WithPrefixMatch_ShouldReturnCorrectProvider()
        {
            // Arrange & Act
            var euProvider = _factory.GetProvider("eu-custom-1");
            var brProvider = _factory.GetProvider("br-custom-1");
            var saProvider = _factory.GetProvider("sa-custom-1");

            // Assert
            Assert.Equal("GDPR", euProvider.RegulationCode);
            Assert.Equal("LGPD", brProvider.RegulationCode);
            Assert.Equal("LGPD", saProvider.RegulationCode);
        }
    }
}
