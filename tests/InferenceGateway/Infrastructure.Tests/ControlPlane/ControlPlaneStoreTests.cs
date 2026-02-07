
namespace Synaxis.InferenceGateway.Infrastructure.Tests.ControlPlane;

using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

public class ControlPlaneStoreTests
{
    [Fact]
    public async Task GetAliasAsync_ReturnsAlias_WhenExists()
    {
        var tenantId = Guid.NewGuid();
        var dbContext = BuildDbContext();
        var store = new ControlPlaneStore(dbContext);

        var alias = new ModelAlias
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Alias = "my-alias",
            TargetModel = "gpt-4",
        };

        await dbContext.ModelAliases.AddAsync(alias).ConfigureAwait(false);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        var result = await store.GetAliasAsync(tenantId, "my-alias").ConfigureAwait(false);

        Assert.NotNull(result);
        Assert.Equal(alias.Id, result.Id);
        Assert.Equal("my-alias", result.Alias);
        Assert.Equal("gpt-4", result.TargetModel);
    }

    [Fact]
    public async Task GetAliasAsync_ReturnsNull_WhenNotFound()
    {
        var tenantId = Guid.NewGuid();
        var dbContext = BuildDbContext();
        var store = new ControlPlaneStore(dbContext);

        var result = await store.GetAliasAsync(tenantId, "non-existent-alias").ConfigureAwait(false);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetComboAsync_ReturnsCombo_WhenExists()
    {
        var tenantId = Guid.NewGuid();
        var dbContext = BuildDbContext();
        var store = new ControlPlaneStore(dbContext);

        var combo = new ModelCombo
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "my-combo",
            OrderedModelsJson = "[\"gpt-4\", \"gpt-3.5-turbo\"]",
        };

        await dbContext.ModelCombos.AddAsync(combo).ConfigureAwait(false);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        var result = await store.GetComboAsync(tenantId, "my-combo").ConfigureAwait(false);

        Assert.NotNull(result);
        Assert.Equal(combo.Id, result.Id);
        Assert.Equal("my-combo", result.Name);
        Assert.Equal("[\"gpt-4\", \"gpt-3.5-turbo\"]", result.OrderedModelsJson);
    }

    [Fact]
    public async Task GetComboAsync_ReturnsNull_WhenNotFound()
    {
        var tenantId = Guid.NewGuid();
        var dbContext = BuildDbContext();
        var store = new ControlPlaneStore(dbContext);

        var result = await store.GetComboAsync(tenantId, "non-existent-combo").ConfigureAwait(false);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetGlobalModelAsync_ReturnsModelWithProviderModels_WhenExists()
    {
        var dbContext = BuildDbContext();
        var store = new ControlPlaneStore(dbContext);

        var globalModel = new GlobalModel
        {
            Id = "gpt-4",
            Name = "GPT-4",
            Family = "GPT",
            Description = "Advanced model",
            ContextWindow = 8192,
            MaxOutputTokens = 4096,
            InputPrice = 0.03m,
            OutputPrice = 0.06m,
            IsOpenWeights = false,
            SupportsTools = true,
            SupportsReasoning = true,
            SupportsVision = true,
            SupportsAudio = false,
            SupportsStructuredOutput = true,
        };

        var providerModel1 = new ProviderModel
        {
            Id = 1,
            ProviderId = "openai",
            GlobalModelId = "gpt-4",
            ProviderSpecificId = "gpt-4",
            IsAvailable = true,
            OverrideInputPrice = null,
            OverrideOutputPrice = null,
            RateLimitRPM = 3500,
            RateLimitTPM = 90000,
        };

        var providerModel2 = new ProviderModel
        {
            Id = 2,
            ProviderId = "azure",
            GlobalModelId = "gpt-4",
            ProviderSpecificId = "gpt-4-deployment",
            IsAvailable = true,
            OverrideInputPrice = 0.02m,
            OverrideOutputPrice = 0.04m,
            RateLimitRPM = 200,
            RateLimitTPM = 40000,
        };

        globalModel.ProviderModels = new List<ProviderModel> { providerModel1, providerModel2 };

        await dbContext.GlobalModels.AddAsync(globalModel).ConfigureAwait(false);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        var result = await store.GetGlobalModelAsync("gpt-4").ConfigureAwait(false);

        Assert.NotNull(result);
        Assert.Equal("gpt-4", result.Id);
        Assert.Equal("GPT-4", result.Name);
        Assert.Equal("GPT", result.Family);
        Assert.NotNull(result.ProviderModels);
        Assert.Equal(2, result.ProviderModels.Count);
        Assert.Contains(result.ProviderModels, pm => pm.ProviderId == "openai", StringComparison.Ordinal);
        Assert.Contains(result.ProviderModels, pm => pm.ProviderId == "azure", StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetGlobalModelAsync_ReturnsNull_WhenNotFound()
    {
        var dbContext = BuildDbContext();
        var store = new ControlPlaneStore(dbContext);

        var result = await store.GetGlobalModelAsync("non-existent-model").ConfigureAwait(false);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAliasAsync_UsesNoTracking_Performance()
    {
        var tenantId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseInMemoryDatabase("shared-db-for-no-tracking-test")
            .Options;

        var dbContextForSeeding = new ControlPlaneDbContext(options);
        var alias = new ModelAlias
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Alias = "perf-test-alias",
            TargetModel = "llama-2",
        };

        await dbContextForSeeding.ModelAliases.AddAsync(alias).ConfigureAwait(false);
        await dbContextForSeeding.SaveChangesAsync().ConfigureAwait(false);

        var dbContextForQuery = new ControlPlaneDbContext(options);
        var store = new ControlPlaneStore(dbContextForQuery);
        var result = await store.GetAliasAsync(tenantId, "perf-test-alias").ConfigureAwait(false);

        Assert.NotNull(result);
        var trackedEntry = dbContextForQuery.ChangeTracker.Entries()
            .FirstOrDefault(e => e.Entity is ModelAlias);
        Assert.Null(trackedEntry);
    }

    [Fact]
    public async Task GetGlobalModelAsync_IncludesRelatedEntities()
    {
        var dbContext = BuildDbContext();
        var store = new ControlPlaneStore(dbContext);

        var globalModel = new GlobalModel
        {
            Id = "llama-3.3-70b",
            Name = "Llama 3.3 70B",
            Family = "Llama",
            Description = "Open-weights model",
            ContextWindow = 128000,
            MaxOutputTokens = 8192,
            InputPrice = 0.001m,
            OutputPrice = 0.002m,
            IsOpenWeights = true,
            SupportsTools = true,
            SupportsReasoning = false,
            SupportsVision = false,
            SupportsAudio = false,
            SupportsStructuredOutput = false,
        };

        var providerModel = new ProviderModel
        {
            Id = 1,
            ProviderId = "groq",
            GlobalModelId = "llama-3.3-70b",
            ProviderSpecificId = "llama-3.3-70b-versatile",
            IsAvailable = true,
            RateLimitRPM = 3000,
            RateLimitTPM = 600000,
        };

        globalModel.ProviderModels = new List<ProviderModel> { providerModel };

        await dbContext.GlobalModels.AddAsync(globalModel).ConfigureAwait(false);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        var result = await store.GetGlobalModelAsync("llama-3.3-70b").ConfigureAwait(false);

        Assert.NotNull(result);
        Assert.NotNull(result.ProviderModels);
        Assert.Single(result.ProviderModels);
        Assert.Equal("groq", result.ProviderModels[0].ProviderId);
        Assert.Equal("llama-3.3-70b-versatile", result.ProviderModels[0].ProviderSpecificId);
    }

    private static ControlPlaneDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ControlPlaneDbContext(options);
    }
}
