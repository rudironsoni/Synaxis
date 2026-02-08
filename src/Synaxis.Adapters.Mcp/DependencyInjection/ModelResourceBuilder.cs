// <copyright file="ModelResourceBuilder.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using Synaxis.Adapters.Mcp.Resources;

    /// <summary>
    /// Builder for configuring model resources.
    /// </summary>
    public sealed class ModelResourceBuilder
    {
        private readonly List<ModelResource> _models = new ();

        /// <summary>
        /// Adds a model resource.
        /// </summary>
        /// <param name="id">The unique model identifier.</param>
        /// <param name="name">The display name of the model.</param>
        /// <param name="provider">The provider that hosts this model.</param>
        /// <param name="capabilities">The capabilities supported by this model.</param>
        /// <param name="contextWindow">The maximum context window size in tokens.</param>
        /// <param name="maxOutputTokens">The maximum output tokens the model can generate.</param>
        /// <returns>The builder for chaining.</returns>
        public ModelResourceBuilder AddModel(
            string id,
            string name,
            string provider,
            string[] capabilities,
            int contextWindow,
            int? maxOutputTokens = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Model ID cannot be null or whitespace.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Model name cannot be null or whitespace.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(provider))
            {
                throw new ArgumentException("Provider cannot be null or whitespace.", nameof(provider));
            }

            this._models.Add(new ModelResource(
                Id: id,
                Name: name,
                Provider: provider,
                Capabilities: capabilities ?? Array.Empty<string>(),
                ContextWindow: contextWindow,
                MaxOutputTokens: maxOutputTokens));

            return this;
        }

        /// <summary>
        /// Builds the collection of model resources.
        /// </summary>
        /// <returns>A read-only collection of model resources.</returns>
        internal IReadOnlyList<ModelResource> Build()
        {
            return this._models.AsReadOnly();
        }
    }
}
