using ContextSavvy.LlmProviders.Application.Commands;
using ContextSavvy.LlmProviders.Application.Dtos;

namespace ContextSavvy.LlmProviders.Application.Services;

public interface ITieredProviderRouter
{
    Task<ChatCompletionResult> RouteAsync(ChatCompletionCommand command, CancellationToken cancellationToken = default);
}
