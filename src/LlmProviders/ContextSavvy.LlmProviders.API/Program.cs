using ContextSavvy.LlmProviders.API.Configuration;
using ContextSavvy.LlmProviders.API.Endpoints;
using ContextSavvy.LlmProviders.API.GrpcServices;
using ContextSavvy.LlmProviders.Application.DependencyInjection;
using ContextSavvy.LlmProviders.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddGrpc();
builder.Services.AddProblemDetails();

// Configuration
builder.Services.Configure<LlmProvidersOptions>(
    builder.Configuration.GetSection(LlmProvidersOptions.SectionName));

// Application Services
builder.Services.AddLlmProvidersApplication();

// Infrastructure Services (Providers)
builder.Services.AddLlmProvidersInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map Endpoints
app.MapChatEndpoints();
app.MapModelsEndpoints();
app.MapProviderEndpoints();

// Map gRPC
app.MapGrpcService<LlmGrpcService>();

app.Run();

public partial class Program { }

