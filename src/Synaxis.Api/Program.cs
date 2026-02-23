// <copyright file="Program.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Synaxis.Api.Extensions;
using Synaxis.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add all Synaxis.Api services
builder.Services.AddSynaxisApi(builder.Configuration);

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
