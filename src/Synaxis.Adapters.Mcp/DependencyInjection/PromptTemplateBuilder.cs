// <copyright file="PromptTemplateBuilder.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using Synaxis.Adapters.Mcp.Prompts;

    /// <summary>
    /// Builder for configuring prompt templates.
    /// </summary>
    public sealed class PromptTemplateBuilder
    {
        private readonly List<PromptTemplate> _templates = new ();

        /// <summary>
        /// Adds a prompt template.
        /// </summary>
        /// <param name="name">The unique template name.</param>
        /// <param name="description">A description of what the prompt does.</param>
        /// <param name="template">The prompt template with {{variable}} placeholders.</param>
        /// <param name="variables">The list of required variable names.</param>
        /// <returns>The builder for chaining.</returns>
        public PromptTemplateBuilder AddTemplate(
            string name,
            string description,
            string template,
            params string[] variables)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Template name cannot be null or whitespace.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Description cannot be null or whitespace.", nameof(description));
            }

            if (string.IsNullOrWhiteSpace(template))
            {
                throw new ArgumentException("Template cannot be null or whitespace.", nameof(template));
            }

            this._templates.Add(new PromptTemplate(
                Name: name,
                Description: description,
                Template: template,
                Variables: variables ?? Array.Empty<string>()));

            return this;
        }

        /// <summary>
        /// Builds the collection of prompt templates.
        /// </summary>
        /// <returns>A read-only collection of prompt templates.</returns>
        internal IReadOnlyList<PromptTemplate> Build()
        {
            return this._templates.AsReadOnly();
        }
    }
}
