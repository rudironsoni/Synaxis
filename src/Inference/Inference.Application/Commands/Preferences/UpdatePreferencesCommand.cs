// <copyright file="UpdatePreferencesCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands.Preferences;

using MediatR;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Command to update user chat preferences.
/// </summary>
/// <param name="PreferencesId">The preferences identifier.</param>
/// <param name="UserId">The user identifier.</param>
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
public record UpdatePreferencesCommand(
    Guid PreferencesId,
    Guid UserId,
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
    NotificationPreferencesDto Notifications)
    : IRequest<UpdatePreferencesResult>;

/// <summary>
/// Result of updating user chat preferences.
/// </summary>
/// <param name="Preferences">The updated preferences DTO.</param>
public record UpdatePreferencesResult(PreferencesDto Preferences);

/// <summary>
/// Handler for the <see cref="UpdatePreferencesCommand"/>.
/// </summary>
public class UpdatePreferencesCommandHandler : IRequestHandler<UpdatePreferencesCommand, UpdatePreferencesResult>
{
    private readonly IUserChatPreferencesRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatePreferencesCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The user chat preferences repository.</param>
    public UpdatePreferencesCommandHandler(IUserChatPreferencesRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<UpdatePreferencesResult> Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
    {
        // Validate input
        if (request.DefaultTemperature < 0 || request.DefaultTemperature > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(request.DefaultTemperature), "Temperature must be between 0 and 2.");
        }

        if (request.DefaultMaxTokens < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request.DefaultMaxTokens), "Max tokens must be at least 1.");
        }

        if (request.ChatHistoryRetentionDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request.ChatHistoryRetentionDays), "Retention days must be at least 1.");
        }

        // Get existing preferences
        var preferences = await _repository.GetByIdAsync(request.PreferencesId, cancellationToken);
        if (preferences is null)
        {
            throw new InvalidOperationException($"Preferences with ID '{request.PreferencesId}' were not found.");
        }

        // Verify user ownership
        if (preferences.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("You do not have permission to update these preferences.");
        }

        // Update default settings
        preferences.UpdateDefaultSettings(
            request.DefaultSystemPrompt,
            request.DefaultTemperature,
            request.DefaultMaxTokens,
            request.EnableStreamingByDefault,
            request.PreferredResponseFormat);

        // Update custom instructions
        preferences.UpdateCustomInstructions(request.CustomInstructions);

        // Update UI preferences
        preferences.UpdateUiPreferences(request.ThemePreference, request.LanguagePreference);

        // Update chat history settings
        preferences.UpdateChatHistorySettings(request.SaveChatHistory, request.ChatHistoryRetentionDays);

        // Update notification preferences
        var notificationPreferences = new NotificationPreferences
        {
            EnableEmailNotifications = request.Notifications.EnableEmailNotifications,
            NotifyOnQuotaThreshold = request.Notifications.NotifyOnQuotaThreshold,
            QuotaThresholdPercent = request.Notifications.QuotaThresholdPercent,
            NotifyOnLongRunningRequests = request.Notifications.NotifyOnLongRunningRequests,
            LongRunningThresholdSeconds = request.Notifications.LongRunningThresholdSeconds,
            NotifyOnErrors = request.Notifications.NotifyOnErrors,
        };
        preferences.UpdateNotificationPreferences(notificationPreferences);

        // Persist changes
        await _repository.UpdateAsync(preferences, cancellationToken);

        // Map to DTO and return
        var preferencesDto = MapToDto(preferences);
        return new UpdatePreferencesResult(preferencesDto);
    }

    private static PreferencesDto MapToDto(UserChatPreferences preferences)
    {
        var notifications = preferences.Notifications is null
            ? null
            : new NotificationPreferencesDto(
                preferences.Notifications.EnableEmailNotifications,
                preferences.Notifications.NotifyOnQuotaThreshold,
                preferences.Notifications.QuotaThresholdPercent,
                preferences.Notifications.NotifyOnLongRunningRequests,
                preferences.Notifications.LongRunningThresholdSeconds,
                preferences.Notifications.NotifyOnErrors);

        return new PreferencesDto(
            preferences.Id,
            preferences.UserId,
            preferences.TenantId,
            preferences.PreferredModelId,
            preferences.PreferredProviderId,
            preferences.DefaultSystemPrompt,
            preferences.DefaultTemperature,
            preferences.DefaultMaxTokens,
            preferences.EnableStreamingByDefault,
            preferences.PreferredResponseFormat,
            preferences.CustomInstructions,
            preferences.ThemePreference,
            preferences.LanguagePreference,
            preferences.SaveChatHistory,
            preferences.ChatHistoryRetentionDays,
            notifications,
            preferences.CreatedAt,
            preferences.UpdatedAt);
    }
}
