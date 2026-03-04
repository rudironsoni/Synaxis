// <copyright file="GetPreferencesQuery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Queries;

using MediatR;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;

/// <summary>
/// Query to get user chat preferences.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="TenantId">The tenant identifier.</param>
public record GetPreferencesQuery(
    Guid UserId,
    Guid TenantId)
    : IRequest<PreferencesDto?>;

/// <summary>
/// Handler for the <see cref="GetPreferencesQuery"/>.
/// </summary>
public class GetPreferencesQueryHandler : IRequestHandler<GetPreferencesQuery, PreferencesDto?>
{
    private readonly IUserChatPreferencesRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPreferencesQueryHandler"/> class.
    /// </summary>
    /// <param name="repository">The user chat preferences repository.</param>
    public GetPreferencesQueryHandler(IUserChatPreferencesRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<PreferencesDto?> Handle(GetPreferencesQuery request, CancellationToken cancellationToken)
    {
        var preferences = await _repository.GetByTenantAndUserAsync(
            request.TenantId,
            request.UserId,
            cancellationToken);

        if (preferences is null)
        {
            return null;
        }

        return MapToDto(preferences);
    }

    private static PreferencesDto MapToDto(Synaxis.Inference.Domain.Aggregates.UserChatPreferences preferences)
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
