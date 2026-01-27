using Microsoft.AspNetCore.Mvc;
using Synaxis.InferenceGateway.Application.Security;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Microsoft.EntityFrameworkCore;

namespace Synaxis.InferenceGateway.WebApi.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IJwtService _jwtService;
    private readonly ControlPlaneDbContext _dbContext;

    public AuthController(IJwtService jwtService, ControlPlaneDbContext dbContext)
    {
        _jwtService = jwtService;
        _dbContext = dbContext;
    }

    [HttpPost("dev-login")]
    public async Task<IActionResult> DevLogin([FromBody] DevLoginRequest request, CancellationToken cancellationToken)
    {
        // Only for dev/MVP
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            // Auto-register for dev
            var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Dev Tenant", Region = TenantRegion.Us, Status = TenantStatus.Active };
            _dbContext.Tenants.Add(tenant);
            
            user = new User 
            { 
                Id = Guid.NewGuid(), 
                TenantId = tenant.Id, 
                Email = request.Email, 
                Role = UserRole.Owner, 
                AuthProvider = "dev", 
                ProviderUserId = request.Email 
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var token = _jwtService.GenerateToken(user);
        return Ok(new { token });
    }
}

public class DevLoginRequest
{
    public string Email { get; set; } = string.Empty;
}