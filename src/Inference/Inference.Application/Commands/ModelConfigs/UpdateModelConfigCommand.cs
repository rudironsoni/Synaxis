// <copyright file="UpdateModelConfigCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands.ModelConfigs;

using MediatR;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Command to update a model configuration.
/// </summary>
/// <param name="ConfigId">The configuration identifier.</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="DisplayName">The display name.</param>
/// <param name="Description">The description.</param>
/// <param name="Settings">The model settings.</param>
/// <param name="Pricing">The pricing configuration.</param>
/// <param name="Capabilities">The model capabilities.</param>
public record UpdateModelConfigCommand(
    Guid ConfigId,
    Guid TenantId,
    string DisplayName,
    string? Description,
    ModelSettingsDto Settings,
    ModelPricingDto Pricing,
    ModelCapabilitiesDto Capabilities)
    : IRequest<UpdateModelConfigResult>;

/// <summary>
/// Result of updating a model configuration.
/// </summary>
/// <param name="Config">The updated configuration DTO.</param>
public record UpdateModelConfigResult(ModelConfigDto Config);

/// <summary>
/// Handler for the <see cref="UpdateModelConfigCommand"/>.
/// </summary>
public class UpdateModelConfigCommandHandler : IRequestHandler<UpdateModelConfigCommand, UpdateModelConfigResult>
{
    private readonly IModelConfigRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateModelConfigCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The model configuration repository.</param>
    public UpdateModelConfigCommandHandler(IModelConfigRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<UpdateModelConfigResult> Handle(UpdateModelConfigCommand request, CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ArgumentException("Display name is required.", nameof(request.DisplayName));
        }

        // Get existing configuration
        var config = await _repository.GetByIdAsync(request.ConfigId, cancellationToken);
        if (config is null)
        {
            throw new InvalidOperationException($"Configuration with ID '{request.ConfigId}' was not found.");
        }

        // Verify tenant ownership
        if (config.TenantId != request.TenantId)
        {
            throw new UnauthorizedAccessException("You do not have permission to update this configuration.");
        }

        // Map DTO settings to domain settings
        var settings = new ModelSettings
        {
            MaxTokens = request.Settings.MaxTokens,
            Temperature = request.Settings.Temperature,
            TopP = request.Settings.TopP,
            FrequencyPenalty = request.Settings.FrequencyPenalty,
            PresencePenalty = request.Settings.PresencePenalty,
            ContextWindow = request.Settings.ContextWindow,
            StopSequences = request.Settings.StopSequences.ToList(),
        };

        // Update the configuration
        config.UpdateSettings(settings);

        // Update pricing
        var pricing = new ModelPricing
        {
            InputPricePer1K = request.Pricing.InputPricePer1K,
            OutputPricePer1K = request.Pricing.OutputPricePer1K,
            IsFreeTier = request.Pricing.IsFreeTier,
            FreeTierQuota = request.Pricing.FreeTierQuota,
        };
        config.UpdatePricing(pricing);

        // Persist changes
        await _repository.UpdateAsync(config, cancellationToken);

        // Map to DTO and return
        var configDto = MapToDto(config);
        return new UpdateModelConfigResult(configDto);
    }

    private static ModelConfigDto MapToDto(ModelConfig config)
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
