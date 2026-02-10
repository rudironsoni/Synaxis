// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Synaxis.Api.Middleware;
using Synaxis.Core.Contracts;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")
            ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException("DefaultConnection is required"),
        name: "postgres",
        tags: new[] { "db", "postgres" });

// Configure Database
builder.Services.AddDbContext<SynaxisDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
        ?? throw new InvalidOperationException("DefaultConnection is required");
    options.UseNpgsql(connectionString);
});

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForDevelopmentPurposesOnly123456";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Synaxis";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SynaxisAPI";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

builder.Services.AddAuthorization();

// Register application services
builder.Services.AddScoped<ISuperAdminService, SuperAdminService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserService, UserService>();

// Configure HTTP clients for cross-region communication
var euWest1Endpoint = builder.Configuration["Regions:EuWest1:Endpoint"];
if (!string.IsNullOrEmpty(euWest1Endpoint))
{
    builder.Services.AddHttpClient("eu-west-1", client =>
    {
        client.BaseAddress = new Uri(euWest1Endpoint);
        client.Timeout = TimeSpan.FromSeconds(30);
    });
}

var usEast1Endpoint = builder.Configuration["Regions:UsEast1:Endpoint"];
if (!string.IsNullOrEmpty(usEast1Endpoint))
{
    builder.Services.AddHttpClient("us-east-1", client =>
    {
        client.BaseAddress = new Uri(usEast1Endpoint);
        client.Timeout = TimeSpan.FromSeconds(30);
    });
}

var saEast1Endpoint = builder.Configuration["Regions:SaEast1:Endpoint"];
if (!string.IsNullOrEmpty(saEast1Endpoint))
{
    builder.Services.AddHttpClient("sa-east-1", client =>
    {
        client.BaseAddress = new Uri(saEast1Endpoint);
        client.Timeout = TimeSpan.FromSeconds(30);
    });
}

// Add CORS if needed
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Add Super Admin security middleware
app.UseMiddleware<SuperAdminSecurityMiddleware>();

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

app.MapControllers();

await app.RunAsync().ConfigureAwait(false);
