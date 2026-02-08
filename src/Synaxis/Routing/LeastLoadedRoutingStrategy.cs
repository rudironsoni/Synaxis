// <copyright file="LeastLoadedRoutingStrategy.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Routing
{
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
}
