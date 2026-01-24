using Mediator;
using Synaplexer.Application.Dtos;

namespace Synaplexer.Application.Queries;

public record ListAvailableModelsQuery(string? Provider = null) : IQuery<ModelInfo[]>;
