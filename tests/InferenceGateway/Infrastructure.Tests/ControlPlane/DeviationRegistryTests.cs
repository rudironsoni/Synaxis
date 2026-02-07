using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.ControlPlane;

public class DeviationRegistryTests
{
    [Fact]
    public async Task RegisterAsync_PersistsDeviation()
    {
        var tenantId = Guid.NewGuid();
        var dbContext = BuildDbContext();
        var registry = new DeviationRegistry(dbContext);

        var entry = new DeviationEntry
        {
            TenantId = tenantId,
            Endpoint = "/v1/chat/completions",
            Field = "response_format",
            Reason = "Not supported by provider",
            Mitigation = "Fallback to default",
            Status = DeviationStatus.Open,
        };

        await registry.RegisterAsync(entry).ConfigureAwait(false);

        var items = await registry.ListAsync(tenantId).ConfigureAwait(false);

        Assert.Single(items);
        Assert.Equal("/v1/chat/completions", items[0].Endpoint);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesExistingDeviation()
    {
        var tenantId = Guid.NewGuid();
        var dbContext = BuildDbContext();
        var registry = new DeviationRegistry(dbContext);

        var entry = new DeviationEntry
        {
            TenantId = tenantId,
            Endpoint = "/v1/responses",
            Field = "stream_options",
            Reason = "Streaming mismatch",
            Mitigation = "Documented deviation",
            Status = DeviationStatus.Open,
        };

        await registry.RegisterAsync(entry).ConfigureAwait(false);
        await registry.UpdateStatusAsync(entry.Id, DeviationStatus.Mitigated).ConfigureAwait(false);

        var items = await registry.ListAsync(tenantId).ConfigureAwait(false);

        Assert.Single(items);
        Assert.Equal(DeviationStatus.Mitigated, items[0].Status);
    }

    private static ControlPlaneDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ControlPlaneDbContext(options);
    }
}
