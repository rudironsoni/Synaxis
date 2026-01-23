using Mediator;
using Microsoft.Extensions.Logging;

namespace ContextSavvy.LlmProviders.Application.Behaviors;

public sealed class LoggingBehavior<TMessage, TResponse> 
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly ILogger<LoggingBehavior<TMessage, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var messageType = typeof(TMessage).Name;
        _logger.LogInformation("Processing {MessageType}", messageType);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await next(message, cancellationToken);
            stopwatch.Stop();
            _logger.LogInformation("{MessageType} handled successfully in {Duration}ms", messageType, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "{MessageType} failed after {Duration}ms", messageType, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
