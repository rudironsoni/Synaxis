using Synaplexer.Application.Commands;
using AppChatCompletionResult = Synaplexer.Application.Dtos.ChatCompletionResult;
using Synaplexer.Domain.ValueObjects;

namespace Synaplexer.Application.Services;

public interface IProviderService
{
    string Name { get; }
    ProviderTier Tier { get; }
    Task<AppChatCompletionResult> CompleteAsync(ChatCompletionCommand command, CancellationToken cancellationToken);
    bool SupportsModel(string model);
}
