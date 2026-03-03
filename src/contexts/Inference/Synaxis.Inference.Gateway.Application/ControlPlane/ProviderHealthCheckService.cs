// <copyright file="ProviderHealthCheckService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Application.ChatClients;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
    using Synaxis.InferenceGateway.Application.Routing;

    /// <summary>
    /// Implements provider health checking.
    /// </summary>
    public class ProviderHealthCheckService : IProviderHealthCheckService
    {
        private readonly IHealthStore healthStore;
        private readonly IChatClientFactory chatClientFactory;
        private readonly ILogger<ProviderHealthCheckService> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderHealthCheckService"/> class.
        /// </summary>
        /// <param name="healthStore">The health store service.</param>
        /// <param name="chatClientFactory">The chat client factory.</param>
        /// <param name="logger">The logger instance.</param>
        public ProviderHealthCheckService(
            IHealthStore healthStore,
            IChatClientFactory chatClientFactory,
            ILogger<ProviderHealthCheckService> logger)
        {
            ArgumentNullException.ThrowIfNull(healthStore);
            this.healthStore = healthStore;
            ArgumentNullException.ThrowIfNull(chatClientFactory);
            this.chatClientFactory = chatClientFactory;
            ArgumentNullException.ThrowIfNull(logger);
            this.logger = logger;
        }

        /// <summary>
        /// Checks the health of a specific provider.
        /// </summary>
        /// <param name="providerKey">The provider key to check.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The health check result.</returns>
        public async Task<HealthCheckResult> CheckProviderHealthAsync(string providerKey, CancellationToken cancellationToken = default)
        {
            this.logger.LogInformation("Checking health of provider '{ProviderKey}'", providerKey);

            bool isHealthy = false;
            string? endpoint = null;
            bool supportsStreaming = false;
            bool supportsChat = false;
            int? latencyMs = null;
            var supportedModels = Array.Empty<string>();
            var errors = Array.Empty<string>();

            try
            {
                var client = this.chatClientFactory.GetClient(providerKey);

                if (client == null)
                {
                    this.logger.LogWarning("Provider '{ProviderKey}' client not found", providerKey);
                    errors = new[] { "Client not found for provider" };
                    return new HealthCheckResult(
                        IsHealthy: false,
                        Endpoint: endpoint,
                        SupportsStreaming: supportsStreaming,
                        SupportsChat: supportsChat,
                        LatencyMs: latencyMs,
                        SupportedModels: supportedModels,
                        Errors: errors);
                }

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var testMessages = new[] { new ChatMessage(ChatRole.User, "test") };
                var options = new ChatOptions { ModelId = "gpt-3.5-turbo" };

                var response = await client.GetResponseAsync(testMessages, options, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                latencyMs = (int)stopwatch.ElapsedMilliseconds;
                isHealthy = response != null;
                supportsStreaming = true;
                supportsChat = true;

                this.logger.LogInformation("Provider '{ProviderKey}' health check passed in {LatencyMs}ms", providerKey, latencyMs);
            }
            catch (Exception ex)
            {
                isHealthy = false;
                errors = new[] { ex.Message };
                this.logger.LogWarning(ex, "Provider '{ProviderKey}' health check failed", providerKey);
            }

            return new HealthCheckResult(
                IsHealthy: isHealthy,
                Endpoint: endpoint,
                SupportsStreaming: supportsStreaming,
                SupportsChat: supportsChat,
                LatencyMs: latencyMs,
                SupportedModels: supportedModels,
                Errors: errors);
        }

        /// <summary>
        /// Runs a health check and updates the health store.
        /// </summary>
        /// <param name="providerKey">The provider key to check.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The health check result.</returns>
        public async Task<HealthCheckResult> RunHealthCheckAsync(string providerKey, CancellationToken cancellationToken = default)
        {
            var result = await this.CheckProviderHealthAsync(providerKey, cancellationToken).ConfigureAwait(false);

            if (result.IsHealthy)
            {
                await this.healthStore.MarkSuccessAsync(providerKey, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await this.healthStore.MarkFailureAsync(providerKey, TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
    }
}
