using Synaplexer.API.Configuration;
using Synaplexer.API.Endpoints;
using Synaplexer.API.GrpcServices;
using Synaplexer.Application.DependencyInjection;
using Synaplexer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddGrpc();
builder.Services.AddProblemDetails();

// Configuration
builder.Services.Configure<SynaplexerOptions>(
    builder.Configuration.GetSection(SynaplexerOptions.SectionName));

// Application Services
builder.Services.AddSynaplexerApplication();

// Infrastructure Services (Providers)
builder.Services.AddSynaplexerInfrastructure(builder.Configuration);

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

