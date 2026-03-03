// <copyright file="Program.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Synaxis.Gateway.Api.Configuration;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection(GatewayRoutingConfig.SectionName));

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "liveness" });

// Configure JWT Authentication
var authConfig = builder.Configuration.GetSection(GatewayRoutingConfig.SectionName).Get<GatewayRoutingConfig>();
if (authConfig?.Authentication?.Enabled == true)
{
    builder.Services.AddAuthentication()
        .AddJwtBearer(options =>
        {
            var tokenValidation = authConfig.Authentication.TokenValidation;
            if (!string.IsNullOrEmpty(tokenValidation.Authority))
            {
                options.Authority = tokenValidation.Authority;
            }

            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = tokenValidation.ValidateIssuer,
                ValidateAudience = tokenValidation.ValidateAudience,
                ValidateLifetime = tokenValidation.ValidateLifetime,
            };

            if (!string.IsNullOrEmpty(tokenValidation.Audience))
            {
                options.TokenValidationParameters.ValidAudience = tokenValidation.Audience;
            }
        });

    builder.Services.AddAuthorization();
}

// Add MediatR for messaging
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// Add controllers and API explorer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Swagger
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? new[] { "http://localhost:8080" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

if (authConfig?.Authentication?.Enabled == true)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("liveness"),
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("readiness"),
});

// Map YARP reverse proxy
app.MapReverseProxy();

// Map controllers
app.MapControllers();

await app.RunAsync().ConfigureAwait(false);

/// <summary>
/// Partial class for test accessibility.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Program"/> class.
    /// </summary>
    protected Program()
    {
    }
}
