// <copyright file="ModelResourceProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp.Resources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides model information as MCP resources.
    /// </summary>
    public sealed class ModelResourceProvider
    {
        private readonly IReadOnlyList<ModelResource> _models;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelResourceProvider"/> class.
        /// </summary>
        /// <param name="models">The collection of available models.</param>
        public ModelResourceProvider(IEnumerable<ModelResource> models)
        {
            if (models is null)
            {
                throw new ArgumentNullException(nameof(models));
            }

            this._models = models.ToList().AsReadOnly();
        }

        /// <summary>
        /// Lists all available models.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the list of models.</returns>
        public Task<IReadOnlyList<ModelResource>> ListModelsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(this._models);
        }

        /// <summary>
        /// Gets a specific model by its ID.
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the model if found.</returns>
        public Task<ModelResource?> GetModelAsync(string modelId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(modelId))
            {
                return Task.FromResult<ModelResource?>(null);
            }

            var model = this._models.FirstOrDefault(m => string.Equals(m.Id, modelId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(model);
        }
    }
}
