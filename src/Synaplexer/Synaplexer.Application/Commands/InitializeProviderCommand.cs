using Mediator;

namespace Synaplexer.Application.Commands;

public record InitializeProviderCommand(string ProviderType, bool ForceReinitialize = false) : ICommand<bool>;
