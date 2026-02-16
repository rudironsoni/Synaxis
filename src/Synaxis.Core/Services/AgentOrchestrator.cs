// <copyright file="AgentOrchestrator.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Orchestrates multi-agent coordination and manages agent lifecycle.
    /// </summary>
    public class AgentOrchestrator
    {
        private readonly Dictionary<Guid, Agents.SynaxisInferenceAgent> _agents;
        private readonly Agents.RoutingAgent _routingAgent;
        private readonly ILogger<AgentOrchestrator> _logger;
        private readonly Lock _lock = new Lock();
        private bool _isStarted;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentOrchestrator"/> class.
        /// </summary>
        /// <param name="routingAgent">The routing agent for request distribution.</param>
        /// <param name="logger">The logger instance.</param>
        public AgentOrchestrator(
            Agents.RoutingAgent routingAgent,
            ILogger<AgentOrchestrator> logger)
        {
            this._routingAgent = routingAgent ?? throw new ArgumentNullException(nameof(routingAgent));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._agents = new Dictionary<Guid, Agents.SynaxisInferenceAgent>();
            this._isStarted = false;
        }

        /// <summary>
        /// Gets a value indicating whether the orchestrator is started.
        /// </summary>
        public bool IsStarted => this._isStarted;

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
        /// Starts the orchestrator and initializes all registered agents.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (this._isStarted)
            {
                this._logger.LogWarning("Agent orchestrator is already started");
                return;
            }

            this._logger.LogInformation("Starting agent orchestrator");

            // Initialize the routing agent
            await this._routingAgent.InitializeAsync(cancellationToken).ConfigureAwait(false);

            // Initialize all registered agents
            var initializationTasks = new List<Task>();
            lock (this._lock)
            {
                foreach (var agent in this._agents.Values)
                {
                    initializationTasks.Add(agent.InitializeAsync(cancellationToken));
                }
            }

            await Task.WhenAll(initializationTasks).ConfigureAwait(false);

            this._isStarted = true;
            this._logger.LogInformation("Agent orchestrator started with {AgentCount} agents", this._agents.Count);
        }

        /// <summary>
        /// Stops the orchestrator.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (!this._isStarted)
            {
                this._logger.LogWarning("Agent orchestrator is not started");
                return;
            }

            this._logger.LogInformation("Stopping agent orchestrator");

            this._isStarted = false;

            this._logger.LogInformation("Agent orchestrator stopped");
        }

        /// <summary>
        /// Registers an agent with the orchestrator.
        /// </summary>
        /// <param name="agent">The agent to register.</param>
        public void RegisterAgent(Agents.SynaxisInferenceAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            lock (this._lock)
            {
                if (this._agents.ContainsKey(agent.Id))
                {
                    this._logger.LogWarning(
                        "Agent {AgentName} (ID: {AgentId}) is already registered",
                        agent.Name,
                        agent.Id);
                    return;
                }

                this._agents[agent.Id] = agent;

                // Also register with the routing agent
                this._routingAgent.RegisterAgent(agent);

                this._logger.LogInformation(
                    "Registered agent {AgentName} (ID: {AgentId}) with orchestrator",
                    agent.Name,
                    agent.Id);
            }
        }

        /// <summary>
        /// Unregisters an agent from the orchestrator.
        /// </summary>
        /// <param name="agentId">The ID of the agent to unregister.</param>
        public void UnregisterAgent(Guid agentId)
        {
            lock (this._lock)
            {
                if (!this._agents.TryGetValue(agentId, out var agent))
                {
                    this._logger.LogWarning("Agent with ID {AgentId} not found for unregistration", agentId);
                    return;
                }

                this._agents.Remove(agentId);

                // Also unregister from the routing agent
                this._routingAgent.UnregisterAgent(agentId);

                this._logger.LogInformation(
                    "Unregistered agent {AgentName} (ID: {AgentId}) from orchestrator",
                    agent.Name,
                    agent.Id);
            }
        }

        /// <summary>
        /// Gets an agent by ID.
        /// </summary>
        /// <param name="agentId">The agent ID.</param>
        /// <returns>The agent, or null if not found.</returns>
        public Agents.SynaxisInferenceAgent? GetAgent(Guid agentId)
        {
            lock (this._lock)
            {
                return this._agents.TryGetValue(agentId, out var agent) ? agent : null;
            }
        }

        /// <summary>
        /// Gets all registered agents.
        /// </summary>
        /// <returns>A list of all registered agents.</returns>
        public IReadOnlyList<Agents.SynaxisInferenceAgent> GetAllAgents()
        {
            lock (this._lock)
            {
                return this._agents.Values.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets agents by capability.
        /// </summary>
        /// <param name="capability">The capability to filter by.</param>
        /// <returns>A list of agents with the specified capability.</returns>
        public IReadOnlyList<Agents.SynaxisInferenceAgent> GetAgentsByCapability(string capability)
        {
            lock (this._lock)
            {
                return this._agents.Values
                    .Where(a => a.HasCapability(capability))
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>
        /// Coordinates multiple agents to complete a task.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <typeparam name="TResponse">The type of response.</typeparam>
        /// <param name="agentIds">The IDs of the agents to coordinate.</param>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A dictionary of agent IDs to their responses.</returns>
        public async Task<IDictionary<Guid, TResponse>> CoordinateAgentsAsync<TRequest, TResponse>(
            IEnumerable<Guid> agentIds,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            if (!this._isStarted)
            {
                throw new InvalidOperationException("Agent orchestrator is not started");
            }

            var agents = this.ResolveAgents(agentIds);

            if (agents.Count == 0)
            {
                this._logger.LogWarning("No valid agents found for coordination");
                return new Dictionary<Guid, TResponse>();
            }

            this._logger.LogInformation(
                "Coordinating {AgentCount} agents for request of type {RequestType}",
                agents.Count,
                typeof(TRequest).Name);

            return await this.ExecuteAgentCoordinationAsync<TRequest, TResponse>(agents, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Resolves agent IDs to agent instances.
        /// </summary>
        /// <param name="agentIds">The agent IDs to resolve.</param>
        /// <returns>A list of resolved agents.</returns>
        private IList<Agents.SynaxisInferenceAgent> ResolveAgents(IEnumerable<Guid> agentIds)
        {
            var agents = new List<Agents.SynaxisInferenceAgent>();
            lock (this._lock)
            {
                foreach (var agentId in agentIds)
                {
                    if (this._agents.TryGetValue(agentId, out var agent))
                    {
                        agents.Add(agent);
                    }
                    else
                    {
                        this._logger.LogWarning("Agent with ID {AgentId} not found for coordination", agentId);
                    }
                }
            }

            return agents;
        }

        /// <summary>
        /// Executes agent coordination and collects responses.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <typeparam name="TResponse">The type of response.</typeparam>
        /// <param name="agents">The agents to coordinate.</param>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A dictionary of agent IDs to their responses.</returns>
        private async Task<IDictionary<Guid, TResponse>> ExecuteAgentCoordinationAsync<TRequest, TResponse>(
            IList<Agents.SynaxisInferenceAgent> agents,
            TRequest request,
            CancellationToken cancellationToken)
            where TRequest : class
            where TResponse : class
        {
            var tasks = agents.Select(async agent =>
            {
                try
                {
                    var response = await agent.InvokeAsync<TRequest, TResponse>(request, cancellationToken).ConfigureAwait(false);
                    return new { AgentId = agent.Id, Response = response };
                }
                catch (Exception ex)
                {
                    this._logger.LogError(
                        ex,
                        "Error coordinating agent {AgentName} (ID: {AgentId})",
                        agent.Name,
                        agent.Id);
                    return null;
                }
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            var responseDict = new Dictionary<Guid, TResponse>();
            foreach (var result in results)
            {
                if (result != null && result.Response != null)
                {
                    responseDict[result.AgentId] = result.Response;
                }
            }

            this._logger.LogInformation(
                "Completed coordination with {SuccessCount}/{TotalCount} successful responses",
                responseDict.Count,
                agents.Count);

            return responseDict;
        }

        /// <summary>
        /// Gets the health status of all agents.
        /// </summary>
        /// <returns>A dictionary of agent IDs to their health status.</returns>
        public IDictionary<Guid, AgentHealthStatus> GetAgentHealthStatus()
        {
            var statusDict = new Dictionary<Guid, AgentHealthStatus>();

            lock (this._lock)
            {
                foreach (var kvp in this._agents)
                {
                    statusDict[kvp.Key] = new AgentHealthStatus
                    {
                        AgentId = kvp.Value.Id,
                        AgentName = kvp.Value.Name,
                        IsInitialized = kvp.Value.IsInitialized,
                        Capabilities = kvp.Value.Capabilities.ToList(),
                        Status = kvp.Value.IsInitialized ? "Healthy" : "Not Initialized",
                    };
                }
            }

            return statusDict;
        }

        /// <summary>
        /// Represents the health status of an agent.
        /// </summary>
        public class AgentHealthStatus
        {
            /// <summary>
            /// Gets or sets the agent ID.
            /// </summary>
            public Guid AgentId { get; set; }

            /// <summary>
            /// Gets or sets the agent name.
            /// </summary>
            public string AgentName { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets a value indicating whether the agent is initialized.
            /// </summary>
            public bool IsInitialized { get; set; }

            /// <summary>
            /// Gets or sets the agent capabilities.
            /// </summary>
            public IList<string> Capabilities { get; set; } = new List<string>();

            /// <summary>
            /// Gets or sets the agent status.
            /// </summary>
            public string Status { get; set; } = string.Empty;
        }
    }
}
