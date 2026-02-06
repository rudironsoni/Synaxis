// <copyright file="AliasConfig.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Configuration
{
    using System.Collections.Generic;

    /// <summary>
    /// Configuration for an alias.
    /// </summary>
    public class AliasConfig
    {
        /// <summary>
        /// Gets or sets the list of candidate model IDs for this alias.
        /// </summary>
        public IList<string> Candidates { get; set; } = new List<string>();
    }
}
