// <copyright file="InMemoryDbContext.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests.Infrastructure
{
    using Microsoft.EntityFrameworkCore;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

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
            await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
            return context;
        }

        public static async Task<ControlPlaneDbContext> CreateWithDataAsync(Func<ControlPlaneDbContext, Task> seedData)
        {
            var context = await CreateAsync().ConfigureAwait(false);
            await seedData(context).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
            return context;
        }
    }
}
