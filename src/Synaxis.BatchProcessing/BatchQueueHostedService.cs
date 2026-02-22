// <copyright file="BatchQueueHostedService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synaxis.BatchProcessing.Services;

/// <summary>
/// Hosted service for managing the batch queue processor.
/// </summary>
public class BatchQueueHostedService : BackgroundService
{
    private readonly IBatchQueueService _queueService;
    private readonly ILogger<BatchQueueHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchQueueHostedService"/> class.
    /// </summary>
    /// <param name="queueService">The batch queue service.</param>
    /// <param name="logger">The logger.</param>
    public BatchQueueHostedService(
        IBatchQueueService queueService,
        ILogger<BatchQueueHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(queueService);
        this._queueService = queueService;
        ArgumentNullException.ThrowIfNull(logger);
        this._logger = logger;
    }

    /// <summary>
    /// Executes the background service.
    /// </summary>
    /// <param name="stoppingToken">The stopping token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._logger.LogInformation("Batch Queue Hosted Service starting");

        try
        {
            await this._queueService.StartProcessingAsync(stoppingToken).ConfigureAwait(false);

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
            await this._queueService.StopProcessingAsync(stoppingToken).ConfigureAwait(false);
            this._logger.LogInformation("Batch Queue Hosted Service stopped");
        }
    }
}
