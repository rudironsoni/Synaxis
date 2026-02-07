// <copyright file="SynaxisDbContextFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;

    /// <summary>
    /// Design-time factory for creating SynaxisDbContext during EF Core migrations.
    /// This enables 'dotnet ef migrations' commands to work without a running application.
    /// </summary>
    /// <summary>
    /// SynaxisDbContextFactory class.
    /// </summary>
    public class SynaxisDbContextFactory : IDesignTimeDbContextFactory<SynaxisDbContext>
    {
        /// <summary>
        /// Creates a new instance of <see cref="SynaxisDbContext"/> for design-time operations.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the factory.</param>
        /// <returns>A configured <see cref="SynaxisDbContext"/> instance.</returns>
        public SynaxisDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SynaxisDbContext>();

            // Use a temporary connection string for design-time operations
            // In production, this comes from configuration/DI
            optionsBuilder.UseNpgsql(
                "Host=localhost;Database=synaxis_design;Username=postgres;Password=postgres",
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });

            return new SynaxisDbContext(optionsBuilder.Options);
        }
    }
}
