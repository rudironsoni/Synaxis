using System.Text.Json;
using Microsoft.Extensions.AI;
using Synaxis.InferenceGateway.Application.Extensions;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.Agents.AI.Hosting.OpenAI;
using Microsoft.AspNetCore.OpenApi;
using Synaxis.InferenceGateway.WebApi.Agents;
using Synaxis.InferenceGateway.WebApi.Middleware;
using Synaxis.InferenceGateway.WebApi.Endpoints.Antigravity;
using Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Services
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSynaxisApplication(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<RoutingAgent>();
builder.AddOpenAIChatCompletions();
builder.AddOpenAIResponses();
builder.AddOpenAIConversations();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseMiddleware<OpenAIErrorHandlerMiddleware>();
app.UseMiddleware<OpenAIMetadataMiddleware>();

var agent = app.Services.GetRequiredService<RoutingAgent>();

app.MapOpenAIEndpoints(agent);
app.MapAntigravityEndpoints();

app.Run();

public partial class Program { }
