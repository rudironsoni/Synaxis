// <copyright file="RoutingStrategy.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Routing
{
    using Synaxis.Abstractions.Routing;

    /// <summary>
    /// Base implementation for routing strategies.
    /// </summary>
    public abstract class RoutingStrategy : IRoutingStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RoutingStrategy"/> class.
        /// </summary>
        /// <param name="strategyName">The unique name of the routing strategy.</param>
        protected RoutingStrategy(string strategyName)
        {
            this.StrategyName = strategyName ?? throw new System.ArgumentNullException(nameof(strategyName));
        }

        /// <inheritdoc/>
        public string StrategyName { get; }
    }

    /// <summary>
    /// Round-robin routing strategy implementation.
    /// </summary>
    public sealed class RoundRobinRoutingStrategy : RoutingStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RoundRobinRoutingStrategy"/> class.
        /// </summary>
        public RoundRobinRoutingStrategy()
            : base("RoundRobin")
        {
        }
    }

    /// <summary>
    /// Least-loaded routing strategy implementation.
    /// </summary>
    public sealed class LeastLoadedRoutingStrategy : RoutingStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LeastLoadedRoutingStrategy"/> class.
        /// </summary>
        public LeastLoadedRoutingStrategy()
            : base("LeastLoaded")
        {
        }
    }

    /// <summary>
    /// Priority-based routing strategy implementation.
    /// </summary>
    public sealed class PriorityRoutingStrategy : RoutingStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityRoutingStrategy"/> class.
        /// </summary>
        public PriorityRoutingStrategy()
            : base("Priority")
        {
        }
    }
}
