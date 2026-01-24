using Mediator;
using Synaplexer.Application.Dtos;

namespace Synaplexer.Application.Queries;

public record GetProviderStatusQuery(string ProviderName) : IQuery<ProviderStatusDto>;
