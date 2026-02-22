// <copyright file="ToolRegistry.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Default implementation of <see cref="IToolRegistry"/> for managing MCP tools.
    /// </summary>
    public sealed class ToolRegistry : IToolRegistry
    {
        private readonly Dictionary<string, IMcpTool> _tools = new (StringComparer.Ordinal);

        /// <inheritdoc/>
        public void Register(IMcpTool tool)
        {
            ArgumentNullException.ThrowIfNull(tool);
            if (string.IsNullOrWhiteSpace(tool.Name))
            {
                throw new ArgumentException("Tool name cannot be null or whitespace.", nameof(tool));
            }

            if (this._tools.ContainsKey(tool.Name))
            {
                throw new InvalidOperationException($"A tool with the name '{tool.Name}' is already registered.");
            }

            this._tools[tool.Name] = tool;
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<IMcpTool> GetAll()
        {
            return this._tools.Values.ToList().AsReadOnly();
        }

        /// <inheritdoc/>
        public IMcpTool? GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return this._tools.TryGetValue(name, out var tool) ? tool : null;
        }
    }
}
