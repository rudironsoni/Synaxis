// <copyright file="CreateModelConfigCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands.ModelConfigs;

using MediatR;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Command to create a new model configuration.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="ModelId">The model identifier.</param>
/// <param name="ProviderId">The provider identifier.</param>
/// <param name="DisplayName">The display name.</param>
/// <param name="Description">The description.</param>
/// <param name="Settings">The model settings.</param>
/// <param name="Pricing">The pricing configuration.</param>
/// <param name="Capabilities">The model capabilities.</param>
public record CreateModelConfigCommand(
    Guid TenantId,
    string ModelId,
    string ProviderId,
    string DisplayName,
    string? Description,
    ModelSettingsDto Settings,
    ModelPricingDto Pricing,
    ModelCapabilitiesDto Capabilities)
    : IRequest<CreateModelConfigResult>;

/// <summary>
/// Result of creating a model configuration.
/// </summary>
/// <param name="ConfigId">The unique configuration identifier.</param>
/// <param name="Config">The created configuration DTO.</param>
public record CreateModelConfigResult(Guid ConfigId, ModelConfigDto Config);

/// <summary>
/// Handler for the <see cref="CreateModelConfigCommand"/>.
/// </summary>
public class CreateModelConfigCommandHandler : IRequestHandler<CreateModelConfigCommand, CreateModelConfigResult>
{
    private readonly IModelConfigRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModelConfigCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The model configuration repository.</param>
    public CreateModelConfigCommandHandler(IModelConfigRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<CreateModelConfigResult> Handle(CreateModelConfigCommand request, CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.ModelId))
        {
            throw new ArgumentException("Model ID is required.", nameof(request.ModelId));
        }

        if (string.IsNullOrWhiteSpace(request.ProviderId))
        {
            throw new ArgumentException("Provider ID is required.", nameof(request.ProviderId));
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ArgumentException("Display name is required.", nameof(request.DisplayName));
        }

        // Check for duplicate configuration
        bool exists = await _repository.ExistsAsync(
            request.TenantId,
            request.ModelId,
            request.ProviderId,
            cancellationToken: cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException(
                $"A configuration for model '{request.ModelId}' from provider '{request.ProviderId}' already exists.");
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

        // Map DTO pricing to domain pricing
        var pricing = new ModelPricing
        {
            InputPricePer1K = request.Pricing.InputPricePer1K,
            OutputPricePer1K = request.Pricing.OutputPricePer1K,
            IsFreeTier = request.Pricing.IsFreeTier,
            FreeTierQuota = request.Pricing.FreeTierQuota,
        };

        // Map DTO capabilities to domain capabilities
        var capabilities = new ModelCapabilities
        {
            SupportsStreaming = request.Capabilities.SupportsStreaming,
            SupportsFunctionCalling = request.Capabilities.SupportsFunctionCalling,
            SupportsVision = request.Capabilities.SupportsVision,
            SupportsJsonMode = request.Capabilities.SupportsJsonMode,
            SupportedLanguages = request.Capabilities.SupportedLanguages.ToList(),
        };

        // Create the configuration
        var config = ModelConfig.Create(
            Guid.NewGuid(),
            request.ModelId.Trim(),
            request.ProviderId.Trim(),
            request.DisplayName.Trim(),
            request.Description?.Trim(),
            settings,
            pricing,
            capabilities,
            request.TenantId);

        // Persist the configuration
        await _repository.AddAsync(config, cancellationToken);

        // Map to DTO and return
        var configDto = MapToDto(config);
        return new CreateModelConfigResult(config.Id, configDto);
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
