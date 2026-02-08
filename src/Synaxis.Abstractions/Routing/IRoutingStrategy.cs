// <copyright file="IRoutingStrategy.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Routing
{
    /// <summary>
    /// Marker interface for routing strategy implementations.
    /// </summary>
    public interface IRoutingStrategy
    {
        /// <summary>
        /// Gets the unique name of the routing strategy.
        /// </summary>
        string StrategyName { get; }
    }
}
