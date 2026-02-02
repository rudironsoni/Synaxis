namespace Synaxis.InferenceGateway.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Synaxis.InferenceGateway.Application.ControlPlane;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

/// <summary>
/// Admin controller for provider management.
/// </summary>
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IProviderHealthCheckService _healthCheckService;
    private readonly ILogger<AdminController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminController"/> class.
    /// </summary>
    /// <param name="healthCheckService">The provider health check service</param>
    /// <param name="logger">The logger</param>
    public AdminController(
        IProviderHealthCheckService healthCheckService,
        ILogger<AdminController> logger)
    {
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs a health check for a specific provider.
    /// </summary>
    /// <param name="providerKey">The provider key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    [HttpGet("providers/health/{providerKey}")]
    [ProducesResponseType(typeof(HealthCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HealthCheckResult>> CheckProviderHealth(
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin requested health check for provider '{ProviderKey}'", providerKey);
        
        var result = await _healthCheckService.RunHealthCheckAsync(providerKey, cancellationToken);
        
        return Ok(result);
    }

    /// <summary>
    /// Approves a BYOK provider request.
    /// </summary>
    /// <param name="providerKey">The provider key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Approval result</returns>
    [HttpPost("providers/{providerKey}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApproveProviderRequest(
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin approved provider request '{ProviderKey}'", providerKey);
        
        // TODO: Implement approval logic (update ProviderRequest status to Approved)
        // This is interim implementation
        
        return Ok(new { message = $"Provider {providerKey} approved" });
    }

    /// <summary>
    /// Lists all providers with health status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of providers with health status</returns>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult> ListProviders(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin requested list of all providers");
        
        // TODO: Implement provider listing with health status
        // This is interim implementation
        
        return Ok(new { message = "Provider list not yet implemented" });
    }
}
