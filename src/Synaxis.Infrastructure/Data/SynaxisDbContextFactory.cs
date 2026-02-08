// <copyright file="SynaxisDbContextFactory.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Synaxis.Infrastructure.Data
{
    /// <summary>
    /// Factory for creating SynaxisDbContext instances at design time (for migrations).
    /// </summary>
    public class SynaxisDbContextFactory : IDesignTimeDbContextFactory<SynaxisDbContext>
    {
        /// <summary>
        /// Creates a new instance of SynaxisDbContext for design-time tools.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>A new SynaxisDbContext instance.</returns>
        public SynaxisDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SynaxisDbContext>();

            // Use PostgreSQL for migrations
            // In production, this would come from configuration
            optionsBuilder.UseNpgsql(
                "Host=localhost;Database=synaxis;Username=postgres;Password=postgres",
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly("Synaxis.Infrastructure");
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });

            return new SynaxisDbContext(optionsBuilder.Options);
        }
    }
}
