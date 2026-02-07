// <copyright file="ControlPlaneOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane
{
    /// <summary>
    /// Control plane configuration options.
    /// </summary>
    public sealed class ControlPlaneOptions
    {
        /// <summary>
        /// Gets or sets the database connection string.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        public string Region { get; set; } = "us";

        /// <summary>
        /// Gets or sets a value indicating whether to use in-memory database.
        /// </summary>
        public bool UseInMemory { get; set; }
    }
}
