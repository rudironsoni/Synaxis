using Mediator;
using ContextSavvy.LlmProviders.Application.Commands;
using ContextSavvy.LlmProviders.Application.Dtos;
using ContextSavvy.LlmProviders.Application.Services;

namespace ContextSavvy.LlmProviders.Application.Handlers;

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
