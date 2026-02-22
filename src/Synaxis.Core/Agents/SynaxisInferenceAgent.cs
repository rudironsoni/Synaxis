// <copyright file="SynaxisInferenceAgent.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Agents
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Abstract base class for all AI inference agents.
    /// </summary>
    public abstract class SynaxisInferenceAgent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SynaxisInferenceAgent"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the agent.</param>
        /// <param name="name">The name of the agent.</param>
        /// <param name="modelEndpoint">The model endpoint URL.</param>
        /// <param name="capabilities">The agent capabilities.</param>
        /// <param name="logger">The logger instance.</param>
        protected SynaxisInferenceAgent(
            Guid id,
            string name,
            string modelEndpoint,
            IEnumerable<string> capabilities,
            ILogger logger)
        {
            this.Id = id;
            ArgumentNullException.ThrowIfNull(name);
            this.Name = name;
            ArgumentNullException.ThrowIfNull(modelEndpoint);
            this.ModelEndpoint = modelEndpoint;
            ArgumentNullException.ThrowIfNull(capabilities);
            this.Capabilities = capabilities;
            ArgumentNullException.ThrowIfNull(logger);
            this.Logger = logger;
            this.IsInitialized = false;
        }

        /// <summary>
        /// Gets the unique identifier for the agent.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the name of the agent.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the model endpoint URL.
        /// </summary>
        public string ModelEndpoint { get; }

        /// <summary>
        /// Gets the agent capabilities.
        /// </summary>
        public IEnumerable<string> Capabilities { get; }

        /// <summary>
        /// Gets a value indicating whether the agent is initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Initializes the agent asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            this.Logger.LogInformation("Initializing agent {AgentName} (ID: {AgentId})", this.Name, this.Id);

            await this.OnInitializeAsync(cancellationToken).ConfigureAwait(false);

            this.IsInitialized = true;
            this.Logger.LogInformation("Agent {AgentName} (ID: {AgentId}) initialized successfully", this.Name, this.Id);
        }

        /// <summary>
        /// Invokes the agent with the specified request asynchronously.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <typeparam name="TResponse">The type of response.</typeparam>
        /// <param name="request">The request to invoke.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The agent response.</returns>
        public abstract Task<TResponse> InvokeAsync<TRequest, TResponse>(
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Invokes the agent with streaming response asynchronously.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <typeparam name="TResponse">The type of response chunk.</typeparam>
        /// <param name="request">The request to invoke.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of response chunks.</returns>
        public abstract IAsyncEnumerable<TResponse> StreamAsync<TRequest, TResponse>(
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Called when the agent is being initialized. Override to provide custom initialization logic.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected virtual Task OnInitializeAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks if the agent has the specified capability.
        /// </summary>
        /// <param name="capability">The capability to check.</param>
        /// <returns>True if the agent has the capability; otherwise, false.</returns>
        public bool HasCapability(string capability)
        {
            if (string.IsNullOrEmpty(capability))
            {
                return false;
            }

            return this.Capabilities.Any(cap => string.Equals(cap, capability, StringComparison.OrdinalIgnoreCase));
        }
    }
}
