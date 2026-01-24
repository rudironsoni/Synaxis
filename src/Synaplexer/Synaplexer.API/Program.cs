using Serilog;
using Serilog.Enrichers.Sensitive;
using Synaplexer.API.Configuration;
using Synaplexer.API.Endpoints;
using Synaplexer.API.GrpcServices;
using Synaplexer.Application.DependencyInjection;
using Synaplexer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .Enrich.WithSensitiveDataMasking(options =>
    {
        options.Mode = MaskingMode.Globally;
        options.MaskProperties.Add(new MaskProperty { Name = "*Key" });
        options.MaskProperties.Add(new MaskProperty { Name = "*Token" });
        options.MaskProperties.Add(new MaskProperty { Name = "Authorization" });
        options.MaskProperties.Add(new MaskProperty { Name = "Password" });
        options.MaskProperties.Add(new MaskProperty { Name = "Secret" });
    }));

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

