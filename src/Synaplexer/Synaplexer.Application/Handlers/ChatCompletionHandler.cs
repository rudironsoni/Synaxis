using Mediator;
using Synaplexer.Application.Commands;
using Synaplexer.Application.Dtos;
using Synaplexer.Application.Services;

namespace Synaplexer.Application.Handlers;

public class ChatCompletionHandler : ICommandHandler<ChatCompletionCommand, ChatCompletionResult>
{
    private readonly ITieredProviderRouter _router;

    public ChatCompletionHandler(ITieredProviderRouter router)
    {
        _router = router;
    }

    public ValueTask<ChatCompletionResult> Handle(ChatCompletionCommand command, CancellationToken cancellationToken)
    {
        return new ValueTask<ChatCompletionResult>(_router.RouteAsync(command, cancellationToken));
    }
}
