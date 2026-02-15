// <copyright file="SmartRouterOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

#nullable enable

namespace Synaxis.Routing.SmartRouter
{
    using Synaxis.Routing.CircuitBreaker;

    /// <summary>
    /// Configuration options for the SmartRouter.
    /// </summary>
    public class SmartRouterOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of fallback attempts.
        /// Default is 3.
        /// </summary>
        public int MaxFallbackAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the minimum confidence threshold for routing decisions.
        /// Default is 0.5.
        /// </summary>
        public double MinConfidenceThreshold { get; set; } = 0.5;

        /// <summary>
        /// Gets or sets a value indicating whether to enable circuit breaker integration.
        /// Default is true.
        /// </summary>
        public bool EnableCircuitBreaker { get; set; } = true;

        /// <summary>
        /// Gets or sets the circuit breaker options.
        /// </summary>
        public CircuitBreakerOptions? CircuitBreakerOptions { get; set; }

        /// <summary>
        /// Gets or sets the routing predictor options.
        /// </summary>
        public RoutingPredictorOptions? PredictorOptions { get; set; }

        /// <summary>
        /// Gets or sets the default fallback provider ID.
        /// </summary>
        public string? DefaultFallbackProviderId { get; set; }
    }
}
