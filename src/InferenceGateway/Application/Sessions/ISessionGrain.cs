// <copyright file="ISessionGrain.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Sessions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for session grain that manages conversation state and memory.
    /// </summary>
    public interface ISessionGrain
    {
        /// <summary>
        /// Adds or updates a memory item with semantic content and optional tags.
        /// </summary>
        /// <param name="memoryId">Unique identifier for the memory.</param>
        /// <param name="content">The semantic content to store.</param>
        /// <param name="tags">Optional tags for categorization.</param>
        /// <returns>The memory ID that was upserted.</returns>
        Task<string> UpsertMemory(string memoryId, string content, string[]? tags = null);

        /// <summary>
        /// Finds semantically similar memories using vector similarity search.
        /// </summary>
        /// <param name="query">Natural language query to search for.</param>
        /// <param name="limit">Maximum number of results to return.</param>
        /// <param name="tags">Optional tags to filter results.</param>
        /// <returns>List of memory content strings that match the query.</returns>
        Task<IReadOnlyList<string>> RecallMemory(string query, int limit = 5, string[]? tags = null);
    }
}
