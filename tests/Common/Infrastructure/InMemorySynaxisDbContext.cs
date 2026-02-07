using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

namespace Synaxis.Common.Tests.Infrastructure
{
    /// <summary>
    /// Helper for creating in-memory SynaxisDbContext for testing
    /// </summary>
    public static class InMemorySynaxisDbContext
    {
        public static SynaxisDbContext Create()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new SynaxisDbContext(options);
        }

        public static SynaxisDbContext CreateWithData(Action<SynaxisDbContext> seedData)
        {
            var context = Create();
            seedData(context);
            context.SaveChanges();
            return context;
        }

        public static async Task<SynaxisDbContext> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var context = new SynaxisDbContext(options);
            await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
            return context;
        }

        public static async Task<SynaxisDbContext> CreateWithDataAsync(Func<SynaxisDbContext, Task> seedData)
        {
            var context = await CreateAsync().ConfigureAwait(false);
            await seedData(context).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
            return context;
        }
    }
}
