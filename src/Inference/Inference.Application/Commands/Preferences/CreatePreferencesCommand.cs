// <copyright file="CreatePreferencesCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands.Preferences;

using MediatR;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Command to create new user chat preferences.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="PreferredModelId">The preferred model identifier.</param>
/// <param name="PreferredProviderId">The preferred provider identifier.</param>
public record CreatePreferencesCommand(
    Guid UserId,
    Guid TenantId,
    string? PreferredModelId = null,
    string? PreferredProviderId = null)
    : IRequest<CreatePreferencesResult>;

/// <summary>
/// Result of creating user chat preferences.
/// </summary>
/// <param name="PreferencesId">The unique preferences identifier.</param>
/// <param name="Preferences">The created preferences DTO.</param>
public record CreatePreferencesResult(Guid PreferencesId, PreferencesDto Preferences);

/// <summary>
/// Handler for the <see cref="CreatePreferencesCommand"/>.
/// </summary>
public class CreatePreferencesCommandHandler : IRequestHandler<CreatePreferencesCommand, CreatePreferencesResult>
{
    private readonly IUserChatPreferencesRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePreferencesCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The user chat preferences repository.</param>
    public CreatePreferencesCommandHandler(IUserChatPreferencesRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<CreatePreferencesResult> Handle(CreatePreferencesCommand request, CancellationToken cancellationToken)
    {
        // Check if preferences already exist for user
        bool exists = await _repository.ExistsForUserAsync(request.UserId, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Preferences already exist for user '{request.UserId}'.");
        }

        // Create preferences
        var preferences = UserChatPreferences.Create(
            Guid.NewGuid(),
            request.UserId,
            request.TenantId,
            request.PreferredModelId,
            request.PreferredProviderId);

        // Persist preferences
        await _repository.AddAsync(preferences, cancellationToken);

        // Map to DTO and return
        var preferencesDto = MapToDto(preferences);
        return new CreatePreferencesResult(preferences.Id, preferencesDto);
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
