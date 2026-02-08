// <copyright file="IToolRegistry.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines a contract for registering and managing MCP tools.
    /// </summary>
    public interface IToolRegistry
    {
        /// <summary>
        /// Registers a tool with the registry.
        /// </summary>
        /// <param name="tool">The tool to register.</param>
        void Register(IMcpTool tool);

        /// <summary>
        /// Gets all registered tools.
        /// </summary>
        /// <returns>A read-only collection of registered tools.</returns>
        IReadOnlyCollection<IMcpTool> GetAll();

        /// <summary>
        /// Gets a tool by its name.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>The tool if found; otherwise, null.</returns>
        IMcpTool? GetByName(string name);
    }
}
