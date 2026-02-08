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
}
