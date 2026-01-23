using Mediator;
using ContextSavvy.LlmProviders.Application.Dtos;

namespace ContextSavvy.LlmProviders.Application.Queries;

public record GetProviderStatusQuery(string ProviderName) : IQuery<ProviderStatusDto>;
