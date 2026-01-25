using System.Text.Json;
using Microsoft.Extensions.AI;
using Synaxis.Application.Extensions;
using Synaxis.Application.Configuration;
using Synaxis.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.Agents.AI.Hosting.OpenAI;
using Microsoft.AspNetCore.OpenApi;
using Synaxis.WebApi.Agents;
using Synaxis.WebApi.Middleware;
using Synaxis.WebApi.Endpoints.OpenAI;

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

app.Run();

public partial class Program { }
