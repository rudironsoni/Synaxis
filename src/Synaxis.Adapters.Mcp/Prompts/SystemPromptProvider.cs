// <copyright file="SystemPromptProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp.Prompts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides system prompts as MCP prompt resources with template support.
    /// </summary>
    public sealed class SystemPromptProvider
    {
        private readonly IReadOnlyDictionary<string, PromptTemplate> _prompts;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemPromptProvider"/> class.
        /// </summary>
        /// <param name="prompts">The collection of available prompt templates.</param>
        public SystemPromptProvider(IEnumerable<PromptTemplate> prompts)
        {
            ArgumentNullException.ThrowIfNull(prompts);
            this._prompts = prompts.ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);
        }

        /// <summary>
        /// Lists all available prompt templates.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the list of templates.</returns>
        public Task<IReadOnlyCollection<PromptTemplate>> ListPromptsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<PromptTemplate>>(this._prompts.Values.ToList().AsReadOnly());
        }

        /// <summary>
        /// Gets a specific prompt template by name.
        /// </summary>
        /// <param name="name">The prompt template name.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the template if found.</returns>
        public Task<PromptTemplate?> GetPromptAsync(string name, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Task.FromResult<PromptTemplate?>(null);
            }

            this._prompts.TryGetValue(name, out var prompt);
            return Task.FromResult(prompt);
        }

        /// <summary>
        /// Renders a prompt template with the provided variables.
        /// </summary>
        /// <param name="name">The prompt template name.</param>
        /// <param name="variables">The variables to substitute in the template.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the rendered prompt.</returns>
        public Task<string?> RenderPromptAsync(
            string name,
            IReadOnlyDictionary<string, string> variables,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Task.FromResult<string?>(null);
            }

            if (!this._prompts.TryGetValue(name, out var template))
            {
                return Task.FromResult<string?>(null);
            }

            var rendered = template.Template;
            if (variables is not null)
            {
                foreach (var kvp in variables)
                {
                    rendered = rendered.Replace($"{{{{{kvp.Key}}}}}", kvp.Value, StringComparison.Ordinal);
                }
            }

            return Task.FromResult<string?>(rendered);
        }
    }
}
