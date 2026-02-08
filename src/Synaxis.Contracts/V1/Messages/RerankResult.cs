// <copyright file="RerankResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    /// <summary>
    /// Represents a rerank result with index, relevance score, and document text.
    /// </summary>
    public sealed class RerankResult
    {
        /// <summary>
        /// Gets or initializes the index of the document in the original list.
        /// </summary>
        public int Index { get; init; }

        /// <summary>
        /// Gets or initializes the relevance score assigned to the document.
        /// </summary>
        public double Score { get; init; }

        /// <summary>
        /// Gets or initializes the document text.
        /// </summary>
        public string Document { get; init; } = string.Empty;
    }
}
