// <copyright file="AuthController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    /// <summary>
    /// Controller for authentication operations.
    /// </summary>
    [ApiController]
    [Route("auth")]
    [EnableCors("WebApp")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtService jwtService;
        private readonly IPasswordHasher passwordHasher;
        private readonly ControlPlaneDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="jwtService">The JWT service.</param>
        /// <param name="passwordHasher">The password hasher.</param>
        /// <param name="dbContext">The database context.</param>
        public AuthController(IJwtService jwtService, IPasswordHasher passwordHasher, ControlPlaneDbContext dbContext)
        {
            this.jwtService = jwtService;
            this.passwordHasher = passwordHasher;
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="request">The registration request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return this.BadRequest(new { success = false, message = "Email and password are required" });
            }

            var existingUser = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
                .ConfigureAwait(false);

            if (existingUser != null)
            {
                return this.BadRequest(new { success = false, message = "User already exists" });
            }

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = $"{request.Email} Tenant",
                Region = TenantRegion.Us,
                Status = TenantStatus.Active,
            };
            this.dbContext.Tenants.Add(tenant);

            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Email = request.Email,
                PasswordHash = this.passwordHasher.HashPassword(request.Password),
                Role = UserRole.Owner,
                AuthProvider = "local",
                ProviderUserId = request.Email,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            this.dbContext.Users.Add(user);

            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.Ok(new { success = true, userId = user.Id.ToString() });
        }

        /// <summary>
        /// Logs in a user.
        /// </summary>
        /// <param name="request">The login request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The login result with JWT token.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return this.BadRequest(new { success = false, message = "Email and password are required" });
            }

            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
                .ConfigureAwait(false);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                return this.Unauthorized(new { success = false, message = "Invalid credentials" });
            }

            if (!this.passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return this.Unauthorized(new { success = false, message = "Invalid credentials" });
            }

            var token = this.jwtService.GenerateToken(user);
            return this.Ok(
                new
                {
                    token,
                    user = new
                    {
                        id = user.Id.ToString(),
                        email = user.Email,
                    },
                });
        }

        /// <summary>
        /// Development login endpoint for testing.
        /// </summary>
        /// <param name="request">The dev login request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The login result with JWT token.</returns>
        [HttpPost("dev-login")]
        public async Task<IActionResult> DevLogin([FromBody] DevLoginRequest request, CancellationToken cancellationToken)
        {
            var user = await this.dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                var tenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = "Dev Tenant",
                    Region = TenantRegion.Us,
                    Status = TenantStatus.Active,
                };
                this.dbContext.Tenants.Add(tenant);

                user = new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Email = request.Email,
                    Role = UserRole.Owner,
                    AuthProvider = "dev",
                    ProviderUserId = request.Email,
                };
                this.dbContext.Users.Add(user);
                await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            var token = this.jwtService.GenerateToken(user);
            return this.Ok(new { token });
        }
    }

    /// <summary>
    /// Request to register a new user.
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to log in a user.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for development login.
    /// </summary>
    public class DevLoginRequest
    {
        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }
}
