// <copyright file="OutboxBackgroundService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Messaging;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Background service that continuously processes outbox messages.
/// </summary>
public class OutboxBackgroundService : BackgroundService
{
    private readonly OutboxProcessor _processor;
    private readonly ILogger<OutboxBackgroundService> _logger;
    private readonly OutboxOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxBackgroundService"/> class.
    /// </summary>
    /// <param name="processor">The outbox processor.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The outbox configuration options.</param>
    public OutboxBackgroundService(
        OutboxProcessor processor,
        ILogger<OutboxBackgroundService> logger,
        IOptions<OutboxOptions> options)
    {
        this._processor = processor;
        this._logger = logger;
        this._options = options.Value;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._logger.LogInformation(
            "OutboxBackgroundService started with polling interval of {Interval}s",
            this._options.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await this._processor.ProcessAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(this._options.PollingIntervalSeconds),
                stoppingToken)
                .ConfigureAwait(false);
        }

        this._logger.LogInformation("OutboxBackgroundService stopped");
    }

    /// <inheritdoc/>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation("OutboxBackgroundService stopping...");
        return base.StopAsync(cancellationToken);
    }
}
