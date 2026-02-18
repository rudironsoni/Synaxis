// <copyright file="SynaxisHealthCheckOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Health.Configuration
{
    /// <summary>
    /// Configuration options for health checks.
    /// </summary>
    public class SynaxisHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether health checks are enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout in seconds for health checks.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the duration in seconds to cache health check results.
        /// </summary>
        public int CacheDurationSeconds { get; set; } = 5;

        /// <summary>
        /// Gets or sets the PostgreSQL connection string name.
        /// </summary>
        public string PostgreSqlConnectionStringName { get; set; } = "DefaultConnection";

        /// <summary>
        /// Gets or sets the Redis connection string name.
        /// </summary>
        public string RedisConnectionStringName { get; set; } = "Redis";

        /// <summary>
        /// Gets or sets a value indicating whether detailed health information requires authentication.
        /// </summary>
        public bool RequireAuthenticationForDetailedHealth { get; set; } = true;
    }
}
