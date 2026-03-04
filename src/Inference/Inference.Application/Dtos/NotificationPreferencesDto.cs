// <copyright file="NotificationPreferencesDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Dtos;

using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Data transfer object for notification preferences.
/// </summary>
/// <param name="EnableEmailNotifications">Whether email notifications are enabled.</param>
/// <param name="NotifyOnQuotaThreshold">Whether to notify on quota threshold.</param>
/// <param name="QuotaThresholdPercent">The quota threshold percentage.</param>
/// <param name="NotifyOnLongRunningRequests">Whether to notify on long-running requests.</param>
/// <param name="LongRunningThresholdSeconds">The long-running threshold in seconds.</param>
/// <param name="NotifyOnErrors">Whether to notify on errors.</param>
public record NotificationPreferencesDto(
    bool EnableEmailNotifications,
    bool NotifyOnQuotaThreshold,
    int QuotaThresholdPercent,
    bool NotifyOnLongRunningRequests,
    int LongRunningThresholdSeconds,
    bool NotifyOnErrors);

/// <summary>
/// Data transfer object for user chat preferences.
/// </summary>
/// <param name="Id">The preferences identifier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="PreferredModelId">The preferred model identifier.</param>
/// <param name="PreferredProviderId">The preferred provider identifier.</param>
/// <param name="DefaultSystemPrompt">The default system prompt.</param>
/// <param name="DefaultTemperature">The default temperature.</param>
/// <param name="DefaultMaxTokens">The default maximum tokens.</param>
/// <param name="EnableStreamingByDefault">Whether to enable streaming by default.</param>
/// <param name="PreferredResponseFormat">The preferred response format.</param>
/// <param name="CustomInstructions">The custom instructions.</param>
/// <param name="ThemePreference">The theme preference.</param>
/// <param name="LanguagePreference">The language preference.</param>
/// <param name="SaveChatHistory">Whether to save chat history.</param>
/// <param name="ChatHistoryRetentionDays">The chat history retention days.</param>
/// <param name="Notifications">The notification preferences.</param>
/// <param name="CreatedAt">The creation timestamp.</param>
/// <param name="UpdatedAt">The last updated timestamp.</param>
public record PreferencesDto(
    Guid Id,
    Guid UserId,
    Guid TenantId,
    string? PreferredModelId,
    string? PreferredProviderId,
    string? DefaultSystemPrompt,
    double DefaultTemperature,
    int DefaultMaxTokens,
    bool EnableStreamingByDefault,
    ResponseFormat PreferredResponseFormat,
    string? CustomInstructions,
    string ThemePreference,
    string LanguagePreference,
    bool SaveChatHistory,
    int ChatHistoryRetentionDays,
    NotificationPreferencesDto? Notifications,
    DateTime CreatedAt,
    DateTime UpdatedAt);
