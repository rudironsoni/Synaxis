using ContextSavvy.LlmProviders.Application.Commands;
using AppChatCompletionResult = ContextSavvy.LlmProviders.Application.Dtos.ChatCompletionResult;
using ContextSavvy.LlmProviders.Domain.ValueObjects;

namespace ContextSavvy.LlmProviders.Application.Services;

public interface IProviderService
{
    string Name { get; }
    ProviderTier Tier { get; }
    Task<AppChatCompletionResult> CompleteAsync(ChatCompletionCommand command, CancellationToken cancellationToken);
    bool SupportsModel(string model);
}
