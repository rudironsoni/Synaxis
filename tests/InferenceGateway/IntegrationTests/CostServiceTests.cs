using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.Routing;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests
{
    public class CostServiceTests
    {
        private readonly ITestOutputHelper _output;

        public CostServiceTests(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_ForNullDbContext()
        {
            Assert.Throws<ArgumentNullException>(() => new CostService(null!));
        }

        [Fact]
        public void Constructor_ShouldInitializeSuccessfully_WithValidDbContext()
        {
            using var dbContext = CreateInMemoryDbContext();
            var service = new CostService(dbContext);

            Assert.NotNull(service);
        }

        [Fact]
        public async Task GetCostAsync_ShouldReturnNull_WhenNoMatchingCostFound()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var service = new CostService(dbContext);

            // Act
            var result = await service.GetCostAsync("Provider", "model-name");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCostAsync_ShouldReturnCost_WhenExactMatchFound()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var expectedCost = new ModelCost
            {
                Provider = "Groq",
                Model = "llama-3.3-70b-versatile",
                CostPerToken = 0.0001m,
                FreeTier = false
            };
            dbContext.ModelCosts.Add(expectedCost);
            await dbContext.SaveChangesAsync();

            var service = new CostService(dbContext);

            // Act
            var result = await service.GetCostAsync("Groq", "llama-3.3-70b-versatile");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Groq", result.Provider);
            Assert.Equal("llama-3.3-70b-versatile", result.Model);
            Assert.Equal(0.0001m, result.CostPerToken);
            Assert.False(result.FreeTier);
        }

        [Fact]
        public async Task GetCostAsync_ShouldReturnFreeTierCost_WhenFreeTierIsTrue()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var freeCost = new ModelCost
            {
                Provider = "DeepSeek",
                Model = "deepseek-chat",
                CostPerToken = 0m,
                FreeTier = true
            };
            dbContext.ModelCosts.Add(freeCost);
            await dbContext.SaveChangesAsync();

            var service = new CostService(dbContext);

            // Act
            var result = await service.GetCostAsync("DeepSeek", "deepseek-chat");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("DeepSeek", result.Provider);
            Assert.Equal("deepseek-chat", result.Model);
            Assert.Equal(0m, result.CostPerToken);
            Assert.True(result.FreeTier);
        }

        [Fact]
        public async Task GetCostAsync_ShouldUseCaseSensitiveMatching()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var cost1 = new ModelCost { Provider = "Groq", Model = "llama-3.3-70b-versatile", CostPerToken = 0.0001m };
            var cost2 = new ModelCost { Provider = "groq", Model = "llama-3.3-70b-versatile", CostPerToken = 0.0002m };
            dbContext.ModelCosts.Add(cost1);
            dbContext.ModelCosts.Add(cost2);
            await dbContext.SaveChangesAsync();

            var service = new CostService(dbContext);

            // Act
            var result = await service.GetCostAsync("Groq", "llama-3.3-70b-versatile");

            // Assert - Should match exact case
            Assert.NotNull(result);
            Assert.Equal("Groq", result.Provider);
            Assert.Equal(0.0001m, result.CostPerToken);
        }

        [Fact]
        public async Task GetCostAsync_ShouldReturnFirstMatch_WhenMultipleCostsExist()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var cost1 = new ModelCost { Provider = "Provider1", Model = "model1", CostPerToken = 0.0001m };
            var cost2 = new ModelCost { Provider = "Provider2", Model = "model2", CostPerToken = 0.0002m };
            dbContext.ModelCosts.Add(cost1);
            dbContext.ModelCosts.Add(cost2);
            await dbContext.SaveChangesAsync();

            var service = new CostService(dbContext);

            // Act
            var result = await service.GetCostAsync("Provider1", "model1");

            // Assert - Should return exact match
            Assert.NotNull(result);
            Assert.Equal(0.0001m, result.CostPerToken);
        }

        [Fact]
        public async Task GetCostAsync_ShouldRespectCancellationToken()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var service = new CostService(dbContext);
            var cancellationToken = new CancellationToken(true); // Already cancelled

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await service.GetCostAsync("Provider", "model", cancellationToken));
        }

        [Fact]
        public async Task GetCostAsync_ShouldHandleEmptyProviderAndModel()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var service = new CostService(dbContext);

            // Act
            var result = await service.GetCostAsync("", "");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCostAsync_ShouldHandleNullProviderAndModel()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var service = new CostService(dbContext);

            // Act
            var result = await service.GetCostAsync(null!, null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCostAsync_ShouldHandleVeryLongProviderAndModelNames()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var longProvider = new string('a', 100);
            var longModel = new string('b', 200);
            var cost = new ModelCost { Provider = longProvider, Model = longModel, CostPerToken = 0.0001m };
            dbContext.ModelCosts.Add(cost);
            await dbContext.SaveChangesAsync();

            var service = new CostService(dbContext);

            // Act
            var result = await service.GetCostAsync(longProvider, longModel);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(longProvider, result.Provider);
            Assert.Equal(longModel, result.Model);
            Assert.Equal(0.0001m, result.CostPerToken);
        }

        [Fact]
        public async Task GetCostAsync_ShouldHandleSpecialCharactersInProviderAndModel()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var provider = "Provider-With-Dashes";
            var model = "model_with_underscores_and.dots";
            var cost = new ModelCost { Provider = provider, Model = model, CostPerToken = 0.0001m };
            dbContext.ModelCosts.Add(cost);
            await dbContext.SaveChangesAsync();

            var service = new CostService(dbContext);

            // Act
            var result = await service.GetCostAsync(provider, model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(provider, result.Provider);
            Assert.Equal(model, result.Model);
        }

        [Fact]
        public async Task GetCostAsync_ShouldReturnDifferentCosts_ForDifferentProviderModelCombinations()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var cost1 = new ModelCost { Provider = "Provider1", Model = "model1", CostPerToken = 0.0001m };
            var cost2 = new ModelCost { Provider = "Provider2", Model = "model2", CostPerToken = 0.0002m };
            dbContext.ModelCosts.Add(cost1);
            dbContext.ModelCosts.Add(cost2);
            await dbContext.SaveChangesAsync();

            var service = new CostService(dbContext);

            // Act
            var result1 = await service.GetCostAsync("Provider1", "model1");
            var result2 = await service.GetCostAsync("Provider2", "model2");

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(0.0001m, result1.CostPerToken);
            Assert.Equal(0.0002m, result2.CostPerToken);
            Assert.NotEqual(result1.CostPerToken, result2.CostPerToken);
        }

        private ControlPlaneDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ControlPlaneDbContext(options);
        }
    }
}
