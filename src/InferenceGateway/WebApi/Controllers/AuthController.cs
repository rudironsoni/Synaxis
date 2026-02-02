using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Synaxis.InferenceGateway.Application.Security;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Microsoft.EntityFrameworkCore;

namespace Synaxis.InferenceGateway.WebApi.Controllers;

[ApiController]
[Route("auth")]
[EnableCors("WebApp")]
public class AuthController : ControllerBase
{
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ControlPlaneDbContext _dbContext;

    public AuthController(IJwtService jwtService, IPasswordHasher passwordHasher, ControlPlaneDbContext dbContext)
    {
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _dbContext = dbContext;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { success = false, message = "Email and password are required" });
        }

        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
        {
            return BadRequest(new { success = false, message = "User already exists" });
        }

        var tenant = new Tenant 
        { 
            Id = Guid.NewGuid(), 
            Name = $"{request.Email} Tenant", 
            Region = TenantRegion.Us, 
            Status = TenantStatus.Active 
        };
        _dbContext.Tenants.Add(tenant);

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = UserRole.Owner,
            AuthProvider = "local",
            ProviderUserId = request.Email,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true, userId = user.Id.ToString() });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { success = false, message = "Email and password are required" });
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return Unauthorized(new { success = false, message = "Invalid credentials" });
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { success = false, message = "Invalid credentials" });
        }

        var token = _jwtService.GenerateToken(user);
        return Ok(new 
        { 
            token, 
            user = new 
            { 
                id = user.Id.ToString(), 
                email = user.Email 
            } 
        });
    }

    [HttpPost("dev-login")]
    public async Task<IActionResult> DevLogin([FromBody] DevLoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
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

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class DevLoginRequest
{
    public string Email { get; set; } = string.Empty;
}
