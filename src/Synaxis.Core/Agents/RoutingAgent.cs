// <copyright file="RoutingAgent.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Agents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Routes requests to appropriate agent based on capability with load balancing.
    /// </summary>
    public class RoutingAgent : SynaxisInferenceAgent
    {
        private readonly List<SynaxisInferenceAgent> _agents;
        private readonly Dictionary<string, List<SynaxisInferenceAgent>> _capabilityIndex;
        private readonly Dictionary<Guid, int> _agentRequestCounts;
        private readonly Lock _lock = new Lock();
        private readonly Random _random;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoutingAgent"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the routing agent.</param>
        /// <param name="name">The name of the routing agent.</param>
        /// <param name="modelEndpoint">The model endpoint URL (not used for routing).</param>
        /// <param name="capabilities">The routing agent capabilities.</param>
        /// <param name="logger">The logger instance.</param>
        public RoutingAgent(
            Guid id,
            string name,
            string modelEndpoint,
            IEnumerable<string> capabilities,
            ILogger<RoutingAgent> logger)
            : base(id, name, modelEndpoint, capabilities, logger)
        {
            this._agents = new List<SynaxisInferenceAgent>();
            this._capabilityIndex = new Dictionary<string, List<SynaxisInferenceAgent>>(StringComparer.OrdinalIgnoreCase);
            this._agentRequestCounts = new Dictionary<Guid, int>();
            this._random = new Random();
        }

        /// <summary>
        /// Gets the number of registered agents.
        /// </summary>
        public int AgentCount
        {
            get
            {
                lock (this._lock)
                {
                    return this._agents.Count;
                }
            }
        }

        /// <summary>
        /// Registers an agent with the routing agent.
        /// </summary>
        /// <param name="agent">The agent to register.</param>
        public void RegisterAgent(SynaxisInferenceAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            lock (this._lock)
            {
                if (this._agents.Contains(agent))
                {
                    this.Logger.LogWarning("Agent {AgentName} (ID: {AgentId}) is already registered", agent.Name, agent.Id);
                    return;
                }

                this._agents.Add(agent);
                this._agentRequestCounts[agent.Id] = 0;

                // Index by capabilities
                foreach (var capability in agent.Capabilities)
                {
                    if (!this._capabilityIndex.ContainsKey(capability))
                    {
                        this._capabilityIndex[capability] = new List<SynaxisInferenceAgent>();
                    }

                    this._capabilityIndex[capability].Add(agent);
                }

                this.Logger.LogInformation(
                    "Registered agent {AgentName} (ID: {AgentId}) with capabilities: {Capabilities}",
                    agent.Name,
                    agent.Id,
                    string.Join(", ", agent.Capabilities));
            }
        }

        /// <summary>
        /// Unregisters an agent from the routing agent.
        /// </summary>
        /// <param name="agentId">The ID of the agent to unregister.</param>
        public void UnregisterAgent(Guid agentId)
        {
            lock (this._lock)
            {
                var agent = this._agents.FirstOrDefault(a => a.Id == agentId);
                if (agent == null)
                {
                    this.Logger.LogWarning("Agent with ID {AgentId} not found for unregistration", agentId);
                    return;
                }

                this._agents.Remove(agent);
                this._agentRequestCounts.Remove(agentId);

                // Remove from capability index
                foreach (var kvp in this._capabilityIndex.ToList())
                {
                    kvp.Value.Remove(agent);
                    if (kvp.Value.Count == 0)
                    {
                        this._capabilityIndex.Remove(kvp.Key);
                    }
                }

                this.Logger.LogInformation("Unregistered agent {AgentName} (ID: {AgentId})", agent.Name, agent.Id);
            }
        }

        /// <inheritdoc/>
        public override async Task<TResponse> InvokeAsync<TRequest, TResponse>(
            TRequest request,
            CancellationToken cancellationToken = default)
        {
            var agent = this.SelectAgentForRequest();
            if (agent == null)
            {
                throw new InvalidOperationException($"No agent available to handle request of type {typeof(TRequest).Name}");
            }

            this.Logger.LogInformation(
                "Routing request to agent {AgentName} (ID: {AgentId})",
                agent.Name,
                agent.Id);

            try
            {
                var response = await agent.InvokeAsync<TRequest, TResponse>(request, cancellationToken).ConfigureAwait(false);

                lock (this._lock)
                {
                    this._agentRequestCounts[agent.Id]++;
                }

                return response;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(
                    ex,
                    "Error invoking agent {AgentName} (ID: {AgentId})",
                    agent.Name,
                    agent.Id);
                throw new InvalidOperationException($"Error invoking agent {agent.Name} (ID: {agent.Id})", ex);
            }
        }

        /// <inheritdoc/>
        public override async IAsyncEnumerable<TResponse> StreamAsync<TRequest, TResponse>(
            TRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var agent = this.SelectAgentForRequest();
            if (agent == null)
            {
                throw new InvalidOperationException($"No agent available to handle request of type {typeof(TRequest).Name}");
            }

            this.Logger.LogInformation(
                "Routing streaming request to agent {AgentName} (ID: {AgentId})",
                agent.Name,
                agent.Id);

            IAsyncEnumerable<TResponse> stream;
            try
            {
                stream = agent.StreamAsync<TRequest, TResponse>(request, cancellationToken);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(
                    ex,
                    "Error starting stream from agent {AgentName} (ID: {AgentId})",
                    agent.Name,
                    agent.Id);
                throw new InvalidOperationException($"Error starting stream from agent {agent.Name} (ID: {agent.Id})", ex);
            }

            await foreach (var response in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return response;
            }

            lock (this._lock)
            {
                this._agentRequestCounts[agent.Id]++;
            }
        }

        /// <summary>
        /// Selects the best agent for the given request based on capabilities and load balancing.
        /// </summary>
        /// <returns>The selected agent, or null if no suitable agent is found.</returns>
        private SynaxisInferenceAgent SelectAgentForRequest()
        {
            lock (this._lock)
            {
                if (this._agents.Count == 0)
                {
                    this.Logger.LogWarning("No agents registered for routing");
                    return null;
                }

                // For now, use round-robin load balancing
                // In a real implementation, this would analyze the request to determine required capabilities
                var agentIndex = this._random.Next(this._agents.Count);
                return this._agents[agentIndex];
            }
        }

        /// <summary>
        /// Gets the request count for a specific agent.
        /// </summary>
        /// <param name="agentId">The agent ID.</param>
        /// <returns>The number of requests handled by the agent.</returns>
        public int GetAgentRequestCount(Guid agentId)
        {
            lock (this._lock)
            {
                return this._agentRequestCounts.TryGetValue(agentId, out var count) ? count : 0;
            }
        }

        /// <summary>
        /// Gets all registered agents.
        /// </summary>
        /// <returns>A list of all registered agents.</returns>
        public IReadOnlyList<SynaxisInferenceAgent> GetRegisteredAgents()
        {
            lock (this._lock)
            {
                return this._agents.ToList().AsReadOnly();
            }
        }
    }
}
