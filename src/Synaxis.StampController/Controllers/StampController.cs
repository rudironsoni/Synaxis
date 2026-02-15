// <copyright file="StampController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.StampController.Controllers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaxis.StampController.Services;

/// <summary>
/// Background service that watches and processes stamp ConfigMaps.
/// </summary>
public sealed class StampController : BackgroundService
{
    private readonly StampLifecycleService _lifecycleService;
    private readonly ILogger<StampController> _logger;
    private readonly StampControllerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="StampController"/> class.
    /// </summary>
    /// <param name="lifecycleService">The stamp lifecycle service.</param>
    /// <param name="options">The stamp controller options.</param>
    /// <param name="logger">The logger instance.</param>
    public StampController(
        StampLifecycleService lifecycleService,
        IOptions<StampControllerOptions> options,
        ILogger<StampController> logger)
    {
        this._lifecycleService = lifecycleService;
        this._logger = logger;
        this._options = options.Value;
    }

    /// <summary>
    /// Executes the background service to process stamps.
    /// </summary>
    /// <param name="stoppingToken">The stopping token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._logger.LogInformation("Stamp Controller starting");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                this._logger.LogDebug("Processing stamps...");

                await this._lifecycleService.ProcessStampsAsync(stoppingToken).ConfigureAwait(false);

                this._logger.LogDebug("Stamp processing cycle completed. Waiting for {Interval}ms", this._options.ProcessingIntervalMs);

                await Task.Delay(this._options.ProcessingIntervalMs, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex)
        {
            this._logger.LogInformation(ex, "Stamp Controller stopping");
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Stamp Controller encountered an error");
            throw new InvalidOperationException("Stamp Controller encountered an error", ex);
        }
        finally
        {
            this._logger.LogInformation("Stamp Controller stopped");
        }
    }

    /// <summary>
    /// Called when the service is starting.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Stamp Controller is starting");
        return base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Called when the service is stopping.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Stamp Controller is stopping");
        return base.StopAsync(cancellationToken);
    }
}
