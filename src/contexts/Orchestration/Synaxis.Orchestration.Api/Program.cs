// <copyright file="Program.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Synaxis.Orchestration.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add controllers and API explorer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Swagger
builder.Services.AddSwaggerGen();

// Add MediatR for messaging (registers handlers from Application assembly)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Synaxis.Orchestration.Application.Services.IWorkflowService>());

// Add Orchestration infrastructure services (Quartz workers, Marten, etc.)
builder.Services.AddOrchestrationWorkers();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "liveness" });

// Configure JWT Authentication
var authEnabled = builder.Configuration.GetValue<bool>("Authentication:Enabled");
if (authEnabled)
{
    builder.Services.AddAuthentication()
        .AddJwtBearer(options =>
        {
            var authority = builder.Configuration["Authentication:Authority"];
            var audience = builder.Configuration["Authentication:Audience"];

            if (!string.IsNullOrEmpty(authority))
            {
                options.Authority = authority;
            }

            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = builder.Configuration.GetValue<bool>("Authentication:ValidateIssuer", true),
                ValidateAudience = builder.Configuration.GetValue<bool>("Authentication:ValidateAudience", true),
                ValidateLifetime = builder.Configuration.GetValue<bool>("Authentication:ValidateLifetime", true),
            };

            if (!string.IsNullOrEmpty(audience))
            {
                options.TokenValidationParameters.ValidAudience = audience;
            }
        });

    builder.Services.AddAuthorization();
}

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

if (authEnabled)
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
