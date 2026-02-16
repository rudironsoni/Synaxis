// <copyright file="Program.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Synaxis.BatchProcessing.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        // Configure JWT authentication
        // In production, these should come from configuration
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
    });

// Add authorization
builder.Services.AddAuthorization();

// Register batch processing services
builder.Services.Configure<BatchQueueOptions>(
    builder.Configuration.GetSection("BatchProcessing:Queue"));

builder.Services.Configure<BatchStorageOptions>(
    builder.Configuration.GetSection("BatchProcessing:Storage"));

builder.Services.Configure<BatchProcessingOptions>(
    builder.Configuration.GetSection("BatchProcessing:Processor"));

// Register HttpClient for webhook notifications
builder.Services.AddHttpClient<IWebhookNotificationService, WebhookNotificationService>();

// Register batch services
builder.Services.AddSingleton<IBatchStorageService, BatchStorageService>();
builder.Services.AddSingleton<BatchProcessor>();
builder.Services.AddSingleton<IBatchQueueService, BatchQueueService>();

// Register hosted service for queue processing
builder.Services.AddHostedService<BatchQueueHostedService>();

var app = builder.Build();

// Initialize storage
using (var scope = app.Services.CreateScope())
{
    var storageService = scope.ServiceProvider.GetRequiredService<IBatchStorageService>();
    if (storageService is BatchStorageService batchStorageService)
    {
        await batchStorageService.InitializeAsync().ConfigureAwait(false);
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

try
{
    await this._queueService.StartProcessingAsync(stoppingToken);

    // Keep the service running until stopped
    while (!stoppingToken.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
    }
}
catch (OperationCanceledException)
{
    // Expected when service is stopping
}
catch (Exception ex)
{
    this._logger.LogError(ex, "Batch Queue Hosted Service encountered an error");
}
finally
{
    await this._queueService.StopProcessingAsync(stoppingToken);
    this._logger.LogInformation("Batch Queue Hosted Service stopped");
}
    }
}

