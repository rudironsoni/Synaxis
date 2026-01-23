using Mediator;
using ContextSavvy.LlmProviders.Application.Dtos;

namespace ContextSavvy.LlmProviders.Application.Queries;

public record ListAvailableModelsQuery(string? Provider = null) : IQuery<ModelInfo[]>;
