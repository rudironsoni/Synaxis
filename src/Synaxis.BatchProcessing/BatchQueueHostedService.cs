namespace Synaxis.BatchProcessing;

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
        this._queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the background service.
    /// </summary>
    /// <param name="stoppingToken">The stopping token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._logger.LogInformation("Batch Queue Hosted Service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var batch = await this._queueService.DequeueAsync(stoppingToken);
                if (batch != null)
                {
                    this._logger.LogInformation("Processing batch {BatchId}", batch.Id);
                    // Process the batch
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing batch");
            }
        }

        this._logger.LogInformation("Batch Queue Hosted Service stopping");
    }
}
