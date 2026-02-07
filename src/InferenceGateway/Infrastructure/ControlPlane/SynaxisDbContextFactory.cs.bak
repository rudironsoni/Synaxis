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
    public class SynaxisDbContextFactory : IDesignTimeDbContextFactory<SynaxisDbContext>
    {
        public SynaxisDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SynaxisDbContext>();

            // Use a temporary connection string for design-time operations
            // In production, this comes from configuration/DI
            optionsBuilder.UseNpgsql("Host=localhost;Database=synaxis_design;Username=postgres;Password=postgres",
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });

            return new SynaxisDbContext(optionsBuilder.Options);
        }
    }
}
