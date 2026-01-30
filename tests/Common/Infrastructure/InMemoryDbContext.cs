using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

namespace Synaxis.Common.Tests.Infrastructure;

public static class InMemoryDbContext
{
    public static ControlPlaneDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ControlPlaneDbContext(options);
    }

    public static ControlPlaneDbContext CreateWithData(Action<ControlPlaneDbContext> seedData)
    {
        var context = Create();
        seedData(context);
        context.SaveChanges();
        return context;
    }

    public static async Task<ControlPlaneDbContext> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ControlPlaneDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    public static async Task<ControlPlaneDbContext> CreateWithDataAsync(Func<ControlPlaneDbContext, Task> seedData)
    {
        var context = await CreateAsync();
        await seedData(context);
        await context.SaveChangesAsync();
        return context;
    }
}
