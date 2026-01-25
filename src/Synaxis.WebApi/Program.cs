using System.Text.Json;
using Microsoft.Extensions.AI;
using Synaxis.Application.Extensions;
using Synaxis.Application.Configuration;
using Synaxis.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.Agents.AI.Hosting.OpenAI;
using Synaxis.WebApi.Agents;
using Synaxis.WebApi.Endpoints;
using Synaxis.WebApi.Middleware;

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseMiddleware<OpenAIErrorHandlerMiddleware>();
app.UseMiddleware<OpenAIMetadataMiddleware>();

var agent = app.Services.GetRequiredService<RoutingAgent>();
app.MapOpenAIChatCompletions(agent, path: "/v1/chat/completions");
app.MapOpenAIResponses(agent, responsesPath: "/v1/responses");

app.MapLegacyCompletions();
app.MapModels();

app.Run();

public partial class Program { }
