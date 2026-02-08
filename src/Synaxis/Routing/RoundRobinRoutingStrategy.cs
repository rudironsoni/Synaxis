// <copyright file="RoundRobinRoutingStrategy.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Routing
{
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
}
