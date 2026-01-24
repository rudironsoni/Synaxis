using Synaplexer.Application.Commands;
using Synaplexer.Application.Dtos;

namespace Synaplexer.Application.Services;

public interface ITieredProviderRouter
{
    Task<ChatCompletionResult> RouteAsync(ChatCompletionCommand command, CancellationToken cancellationToken = default);
}
