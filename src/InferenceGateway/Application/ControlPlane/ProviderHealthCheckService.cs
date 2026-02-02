namespace Synaxis.InferenceGateway.Application.ControlPlane;

using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.ChatClients;
using Synaxis.InferenceGateway.Application.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implements provider health checking.
/// </summary>
public class ProviderHealthCheckService : IProviderHealthCheckService
{
    private readonly IHealthStore _healthStore;
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ILogger<ProviderHealthCheckService> _logger;

    public ProviderHealthCheckService(
        IHealthStore healthStore,
        IChatClientFactory chatClientFactory,
        ILogger<ProviderHealthCheckService> logger)
    {
        _healthStore = healthStore ?? throw new ArgumentNullException(nameof(healthStore));
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckProviderHealthAsync(string providerKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking health of provider '{ProviderKey}'", providerKey);

        var startTime = DateTime.UtcNow;
        bool isHealthy = false;
        string? endpoint = null;
        bool supportsStreaming = false;
        bool supportsChat = false;
        int? latencyMs = null;
        var supportedModels = Array.Empty<string>();
        var errors = Array.Empty<string>();

        try
        {
            var client = _chatClientFactory.GetClient(providerKey);
            
            if (client == null)
            {
                _logger.LogWarning("Provider '{ProviderKey}' client not found", providerKey);
                errors = new[] { "Client not found for provider" };
                return new HealthCheckResult(
                    IsHealthy: false,
                    Endpoint: endpoint,
                    SupportsStreaming: supportsStreaming,
                    SupportsChat: supportsChat,
                    LatencyMs: latencyMs,
                    SupportedModels: supportedModels,
                    Errors: errors
                );
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var testMessages = new[] { new ChatMessage(ChatRole.User, "test") };
            var options = new ChatOptions { ModelId = "gpt-3.5-turbo" };
            
            var response = await client.GetResponseAsync(testMessages, options, cancellationToken);
            stopwatch.Stop();
            
            latencyMs = (int)stopwatch.ElapsedMilliseconds;
            isHealthy = response != null;
            supportsStreaming = true;
            supportsChat = true;
            
            _logger.LogInformation("Provider '{ProviderKey}' health check passed in {LatencyMs}ms", providerKey, latencyMs);
        }
        catch (Exception ex)
        {
            isHealthy = false;
            errors = new[] { ex.Message };
            _logger.LogWarning(ex, "Provider '{ProviderKey}' health check failed", providerKey);
        }

        return new HealthCheckResult(
            IsHealthy: isHealthy,
            Endpoint: endpoint,
            SupportsStreaming: supportsStreaming,
            SupportsChat: supportsChat,
            LatencyMs: latencyMs,
            SupportedModels: supportedModels,
            Errors: errors
        );
    }

    public async Task<HealthCheckResult> RunHealthCheckAsync(string providerKey, CancellationToken cancellationToken = default)
    {
        var result = await CheckProviderHealthAsync(providerKey, cancellationToken);
        
        if (result.IsHealthy)
        {
            await _healthStore.MarkSuccessAsync(providerKey, cancellationToken);
        }
        else
        {
            await _healthStore.MarkFailureAsync(providerKey, TimeSpan.FromMinutes(5), cancellationToken);
        }
        
        return result;
    }
}
