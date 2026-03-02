// <copyright file="SynaxisDbContextFactory.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Data
{
    using System;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;

    /// <summary>
    /// Factory for creating SynaxisDbContext instances at design time (for migrations).
    /// </summary>
    public class SynaxisDbContextFactory : IDesignTimeDbContextFactory<SynaxisDbContext>
    {
        private const string DefaultHost = "localhost";
        private const string DefaultDatabase = "synaxis";
        private const string DefaultUsername = "postgres";

        /// <summary>
        /// Creates a new instance of SynaxisDbContext for design-time tools.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>A new SynaxisDbContext instance.</returns>
        public SynaxisDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SynaxisDbContext>();

            optionsBuilder.UseNpgsql(
                BuildConnectionString(),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly("Synaxis.Infrastructure");
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });

            return new SynaxisDbContext(optionsBuilder.Options);
        }

        private static string BuildConnectionString()
        {
            var host = GetSetting("SYNAXIS_DB_HOST", DefaultHost);
            var database = GetSetting("SYNAXIS_DB_NAME", DefaultDatabase);
            var username = GetSetting("SYNAXIS_DB_USER", DefaultUsername);

            return new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = host,
                Database = database,
                Username = username,
            }.ToString();
        }

        private static string GetSetting(string environmentVariable, string fallback)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariable);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
