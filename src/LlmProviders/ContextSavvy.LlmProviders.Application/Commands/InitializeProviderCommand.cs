using Mediator;

namespace ContextSavvy.LlmProviders.Application.Commands;

public record InitializeProviderCommand(string ProviderType, bool ForceReinitialize = false) : ICommand<bool>;
