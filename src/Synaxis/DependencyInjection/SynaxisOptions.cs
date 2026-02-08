// <copyright file="SynaxisOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.DependencyInjection
{
    /// <summary>
    /// Configuration options for Synaxis.
    /// </summary>
    public sealed class SynaxisOptions
    {
        /// <summary>
        /// Gets or sets the default routing strategy name.
        /// </summary>
        public string DefaultRoutingStrategy { get; set; } = "RoundRobin";

        /// <summary>
        /// Gets or sets a value indicating whether to enable pipeline behaviors.
        /// </summary>
        public bool EnablePipelineBehaviors { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable request validation.
        /// </summary>
        public bool EnableValidation { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable metrics collection.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;
    }
}
