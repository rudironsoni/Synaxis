using Mediator;
using ContextSavvy.LlmProviders.Application.Dtos;
using ContextSavvy.LlmProviders.Application.Queries;

namespace ContextSavvy.LlmProviders.Application.Handlers;

public class ListAvailableModelsHandler : IQueryHandler<ListAvailableModelsQuery, ModelInfo[]>
{
    public ValueTask<ModelInfo[]> Handle(ListAvailableModelsQuery query, CancellationToken cancellationToken)
    {
        // Mocked model list
        var models = new[]
        {
            new ModelInfo("gpt-4", "GPT-4", "OpenAI", 8192),
            new ModelInfo("claude-3-opus", "Claude 3 Opus", "Anthropic", 200000)
        };

        return new ValueTask<ModelInfo[]>(models);
    }
}
