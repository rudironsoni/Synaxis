// <copyright file="GetModelConfigQuery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Queries;

using MediatR;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;

/// <summary>
/// Query to get a specific model configuration by ID.
/// </summary>
/// <param name="ConfigId">The configuration identifier.</param>
/// <param name="TenantId">The tenant identifier.</param>
public record GetModelConfigQuery(
    Guid ConfigId,
    Guid TenantId)
    : IRequest<ModelConfigDto?>;

/// <summary>
/// Handler for the <see cref="GetModelConfigQuery"/>.
/// </summary>
public class GetModelConfigQueryHandler : IRequestHandler<GetModelConfigQuery, ModelConfigDto?>
{
    private readonly IModelConfigRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetModelConfigQueryHandler"/> class.
    /// </summary>
    /// <param name="repository">The model configuration repository.</param>
    public GetModelConfigQueryHandler(IModelConfigRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<ModelConfigDto?> Handle(GetModelConfigQuery request, CancellationToken cancellationToken)
    {
        var config = await _repository.GetByIdAsync(request.ConfigId, cancellationToken);

        if (config is null || config.TenantId != request.TenantId)
        {
            return null;
        }

        return MapToDto(config);
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
