using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Security;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using System.Security.Claims;

namespace Synaxis.InferenceGateway.WebApi.Controllers;

[ApiController]
[Route("projects/{projectId}/keys")]
[Authorize]
[EnableCors("WebApp")]
public class ApiKeysController : ControllerBase
{
    private readonly ControlPlaneDbContext _dbContext;
    private readonly IApiKeyService _apiKeyService;
    private readonly IAuditService _auditService;

    public ApiKeysController(ControlPlaneDbContext dbContext, IApiKeyService apiKeyService, IAuditService auditService)
    {
        _dbContext = dbContext;
        _apiKeyService = apiKeyService;
        _auditService = auditService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateKey(Guid projectId, [FromBody] CreateKeyRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var tenantId = Guid.Parse(User.FindFirstValue("tenantId")!);

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == tenantId, cancellationToken);

        if (project == null) return NotFound("Project not found");

        var rawKey = _apiKeyService.GenerateKey();
        var hash = _apiKeyService.HashKey(rawKey);

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = request.Name,
            KeyHash = hash,
            Status = ApiKeyStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.ApiKeys.Add(apiKey);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(tenantId, userId, "CreateApiKey", new { ApiKeyId = apiKey.Id, ProjectId = projectId }, cancellationToken);

        return Ok(new { Id = apiKey.Id, Key = rawKey, Name = apiKey.Name });
    }

    [HttpDelete("{keyId}")]
    public async Task<IActionResult> RevokeKey(Guid projectId, Guid keyId, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var tenantId = Guid.Parse(User.FindFirstValue("tenantId")!);

        var apiKey = await _dbContext.ApiKeys
            .Include(k => k.Project)
            .FirstOrDefaultAsync(k => k.Id == keyId && k.ProjectId == projectId && k.Project!.TenantId == tenantId, cancellationToken);

        if (apiKey == null) return NotFound("API Key not found");

        apiKey.Status = ApiKeyStatus.Revoked;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(tenantId, userId, "RevokeApiKey", new { ApiKeyId = keyId }, cancellationToken);

        return NoContent();
    }
}

public class CreateKeyRequest
{
    public string Name { get; set; } = string.Empty;
}