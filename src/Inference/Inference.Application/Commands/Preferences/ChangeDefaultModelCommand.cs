// <copyright file="ChangeDefaultModelCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands.Preferences;

using MediatR;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Command to change the default model for a user.
/// </summary>
/// <param name="PreferencesId">The preferences identifier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="ModelId">The model identifier.</param>
/// <param name="ProviderId">The provider identifier.</param>
public record ChangeDefaultModelCommand(
    Guid PreferencesId,
    Guid UserId,
    string? ModelId,
    string? ProviderId)
    : IRequest<ChangeDefaultModelResult>;

/// <summary>
/// Result of changing the default model.
/// </summary>
/// <param name="Preferences">The updated preferences DTO.</param>
public record ChangeDefaultModelResult(PreferencesDto Preferences);

/// <summary>
/// Handler for the <see cref="ChangeDefaultModelCommand"/>.
/// </summary>
public class ChangeDefaultModelCommandHandler : IRequestHandler<ChangeDefaultModelCommand, ChangeDefaultModelResult>
{
    private readonly IUserChatPreferencesRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeDefaultModelCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The user chat preferences repository.</param>
    public ChangeDefaultModelCommandHandler(IUserChatPreferencesRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<ChangeDefaultModelResult> Handle(ChangeDefaultModelCommand request, CancellationToken cancellationToken)
    {
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

        // Update preferred model
        preferences.UpdatePreferredModel(request.ModelId, request.ProviderId);

        // Persist changes
        await _repository.UpdateAsync(preferences, cancellationToken);

        // Map to DTO and return
        var preferencesDto = MapToDto(preferences);
        return new ChangeDefaultModelResult(preferencesDto);
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
