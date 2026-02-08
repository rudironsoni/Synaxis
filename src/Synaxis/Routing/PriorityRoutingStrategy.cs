// <copyright file="PriorityRoutingStrategy.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Routing
{
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
