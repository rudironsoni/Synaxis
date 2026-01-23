using Mediator;
using ContextSavvy.LlmProviders.Application.Dtos;
using ContextSavvy.LlmProviders.Application.Queries;

namespace ContextSavvy.LlmProviders.Application.Handlers;

public class GetProviderStatusHandler : IQueryHandler<GetProviderStatusQuery, ProviderStatusDto>
{
    public ValueTask<ProviderStatusDto> Handle(GetProviderStatusQuery query, CancellationToken cancellationToken)
    {
        // Mocking status for now
        return new ValueTask<ProviderStatusDto>(new ProviderStatusDto(
            query.ProviderName,
            true,
            "Healthy and active",
            DateTime.UtcNow
        ));
    }
}
