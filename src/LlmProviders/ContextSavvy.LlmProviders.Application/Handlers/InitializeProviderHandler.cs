using Mediator;
using ContextSavvy.LlmProviders.Application.Commands;

namespace ContextSavvy.LlmProviders.Application.Handlers;

public class InitializeProviderHandler : ICommandHandler<InitializeProviderCommand, bool>
{
    public ValueTask<bool> Handle(InitializeProviderCommand command, CancellationToken cancellationToken)
    {
        // Initialization logic would go here
        return new ValueTask<bool>(true);
    }
}
