// <copyright file="GetModelConfigsQuery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Queries;

using MediatR;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;

/// <summary>
/// Query to get all model configurations for a tenant.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="IncludeInactive">Whether to include inactive configurations.</param>
/// <param name="ProviderId">Optional provider filter.</param>
public record GetModelConfigsQuery(
    Guid TenantId,
    bool IncludeInactive = false,
    string? ProviderId = null)
    : IRequest<IReadOnlyList<ModelConfigDto>>;

/// <summary>
/// Handler for the <see cref="GetModelConfigsQuery"/>.
/// </summary>
public class GetModelConfigsQueryHandler : IRequestHandler<GetModelConfigsQuery, IReadOnlyList<ModelConfigDto>>
{
    private readonly IModelConfigRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetModelConfigsQueryHandler"/> class.
    /// </summary>
    /// <param name="repository">The model configuration repository.</param>
    public GetModelConfigsQueryHandler(IModelConfigRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ModelConfigDto>> Handle(GetModelConfigsQuery request, CancellationToken cancellationToken)
    {
        var configs = await _repository.GetByTenantAsync(
            request.TenantId,
            request.IncludeInactive,
            cancellationToken);

        if (!string.IsNullOrEmpty(request.ProviderId))
        {
            configs = configs.Where(c => c.ProviderId == request.ProviderId).ToList();
        }

        return configs.Select(MapToDto).ToList().AsReadOnly();
    }

    private static ModelConfigDto MapToDto(Synaxis.Inference.Domain.Aggregates.ModelConfig config)
    {
        var settings = new ModelSettingsDto(
            config.Settings.MaxTokens,
            config.Settings.Temperature,
            config.Settings.TopP,
            config.Settings.FrequencyPenalty,
            config.Settings.PresencePenalty,
            config.Settings.ContextWindow,
            config.Settings.StopSequences.ToList().AsReadOnly());

        var pricing = new ModelPricingDto(
            config.Pricing.InputPricePer1K,
            config.Pricing.OutputPricePer1K,
            config.Pricing.IsFreeTier,
            config.Pricing.FreeTierQuota);

        var capabilities = new ModelCapabilitiesDto(
            config.Capabilities.SupportsStreaming,
            config.Capabilities.SupportsFunctionCalling,
            config.Capabilities.SupportsVision,
            config.Capabilities.SupportsJsonMode,
            config.Capabilities.SupportedLanguages.ToList().AsReadOnly());

        return new ModelConfigDto(
            config.Id,
            config.ModelId,
            config.ProviderId,
            config.DisplayName,
            config.Description,
            settings,
            pricing,
            capabilities,
            config.TenantId,
            config.IsActive,
            config.CreatedAt,
            config.UpdatedAt);
    }
}
