// <copyright file="ModelSettingsDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Dtos;

/// <summary>
/// Data transfer object for model settings.
/// </summary>
/// <param name="MaxTokens">The maximum tokens.</param>
/// <param name="Temperature">The temperature.</param>
/// <param name="TopP">The top P.</param>
/// <param name="FrequencyPenalty">The frequency penalty.</param>
/// <param name="PresencePenalty">The presence penalty.</param>
/// <param name="ContextWindow">The context window size.</param>
/// <param name="StopSequences">The stop sequences.</param>
public record ModelSettingsDto(
    int MaxTokens,
    double Temperature,
    double TopP,
    double FrequencyPenalty,
    double PresencePenalty,
    int ContextWindow,
    IReadOnlyList<string> StopSequences);

/// <summary>
/// Data transfer object for model pricing.
/// </summary>
/// <param name="InputPricePer1K">The input token price per 1K tokens.</param>
/// <param name="OutputPricePer1K">The output token price per 1K tokens.</param>
/// <param name="IsFreeTier">Whether this is a free tier model.</param>
/// <param name="FreeTierQuota">The free tier quota.</param>
public record ModelPricingDto(
    decimal InputPricePer1K,
    decimal OutputPricePer1K,
    bool IsFreeTier,
    int? FreeTierQuota);

/// <summary>
/// Data transfer object for model capabilities.
/// </summary>
/// <param name="SupportsStreaming">Whether streaming is supported.</param>
/// <param name="SupportsFunctionCalling">Whether function calling is supported.</param>
/// <param name="SupportsVision">Whether vision is supported.</param>
/// <param name="SupportsJsonMode">Whether JSON mode is supported.</param>
/// <param name="SupportedLanguages">The supported languages.</param>
public record ModelCapabilitiesDto(
    bool SupportsStreaming,
    bool SupportsFunctionCalling,
    bool SupportsVision,
    bool SupportsJsonMode,
    IReadOnlyList<string> SupportedLanguages);

/// <summary>
/// Data transfer object for a model configuration.
/// </summary>
/// <param name="Id">The configuration identifier.</param>
/// <param name="ModelId">The model identifier.</param>
/// <param name="ProviderId">The provider identifier.</param>
/// <param name="DisplayName">The display name.</param>
/// <param name="Description">The description.</param>
/// <param name="Settings">The model settings.</param>
/// <param name="Pricing">The pricing configuration.</param>
/// <param name="Capabilities">The model capabilities.</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="IsActive">Whether the model is active.</param>
/// <param name="CreatedAt">The creation timestamp.</param>
/// <param name="UpdatedAt">The last updated timestamp.</param>
public record ModelConfigDto(
    Guid Id,
    string ModelId,
    string ProviderId,
    string DisplayName,
    string? Description,
    ModelSettingsDto Settings,
    ModelPricingDto Pricing,
    ModelCapabilitiesDto Capabilities,
    Guid TenantId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
