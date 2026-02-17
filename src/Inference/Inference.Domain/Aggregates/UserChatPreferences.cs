// <copyright file="UserChatPreferences.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

using Synaxis.Inference.Domain.Events;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Aggregate root representing user chat preferences.
/// </summary>
public class UserChatPreferences : AggregateRoot
{
    /// <summary>
    /// Gets the preferences identifier.
    /// </summary>
    public new Guid Id { get; private set; }

    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the preferred model identifier.
    /// </summary>
    public string? PreferredModelId { get; private set; }

    /// <summary>
    /// Gets the preferred provider identifier.
    /// </summary>
    public string? PreferredProviderId { get; private set; }

    /// <summary>
    /// Gets the default system prompt.
    /// </summary>
    public string? DefaultSystemPrompt { get; private set; }

    /// <summary>
    /// Gets the default temperature.
    /// </summary>
    public double DefaultTemperature { get; private set; } = 0.7;

    /// <summary>
    /// Gets the default maximum tokens.
    /// </summary>
    public int DefaultMaxTokens { get; private set; } = 4096;

    /// <summary>
    /// Gets a value indicating whether to enable streaming by default.
    /// </summary>
    public bool EnableStreamingByDefault { get; private set; } = true;

    /// <summary>
    /// Gets the preferred response format.
    /// </summary>
    public ResponseFormat PreferredResponseFormat { get; private set; } = ResponseFormat.Text;

    /// <summary>
    /// Gets the custom instructions.
    /// </summary>
    public string? CustomInstructions { get; private set; }

    /// <summary>
    /// Gets the theme preference.
    /// </summary>
    public string ThemePreference { get; private set; } = "system";

    /// <summary>
    /// Gets the language preference.
    /// </summary>
    public string LanguagePreference { get; private set; } = "en";

    /// <summary>
    /// Gets a value indicating whether to save chat history.
    /// </summary>
    public bool SaveChatHistory { get; private set; } = true;

    /// <summary>
    /// Gets the retention days for chat history.
    /// </summary>
    public int ChatHistoryRetentionDays { get; private set; } = 30;

    /// <summary>
    /// Gets the notification preferences.
    /// </summary>
    public NotificationPreferences Notifications { get; private set; } = new();

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Creates new user chat preferences.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="preferredModelId">The preferred model identifier.</param>
    /// <param name="preferredProviderId">The preferred provider identifier.</param>
    /// <returns>A new user chat preferences instance.</returns>
    public static UserChatPreferences Create(
        Guid id,
        Guid userId,
        Guid tenantId,
        string? preferredModelId = null,
        string? preferredProviderId = null)
    {
        var preferences = new UserChatPreferences();
        var @event = new UserChatPreferencesCreated
        {
            PreferencesId = id,
            UserId = userId,
            TenantId = tenantId,
            PreferredModelId = preferredModelId,
            PreferredProviderId = preferredProviderId,
            Timestamp = DateTime.UtcNow,
        };

        preferences.ApplyEvent(@event);
        return preferences;
    }

    /// <summary>
    /// Updates the preferred model.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    public void UpdatePreferredModel(string? modelId, string? providerId)
    {
        var @event = new PreferredModelUpdated
        {
            PreferencesId = this.Id,
            ModelId = modelId,
            ProviderId = providerId,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Updates the default settings.
    /// </summary>
    /// <param name="systemPrompt">The system prompt.</param>
    /// <param name="temperature">The temperature value.</param>
    /// <param name="maxTokens">The maximum number of tokens.</param>
    /// <param name="enableStreaming">Whether to enable streaming.</param>
    /// <param name="responseFormat">The response format.</param>
    public void UpdateDefaultSettings(
        string? systemPrompt,
        double temperature,
        int maxTokens,
        bool enableStreaming,
        ResponseFormat responseFormat)
    {
        var @event = new DefaultSettingsUpdated
        {
            PreferencesId = this.Id,
            SystemPrompt = systemPrompt,
            Temperature = temperature,
            MaxTokens = maxTokens,
            EnableStreaming = enableStreaming,
            ResponseFormat = responseFormat,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Updates the custom instructions.
    /// </summary>
    /// <param name="instructions">The custom instructions.</param>
    public void UpdateCustomInstructions(string? instructions)
    {
        var @event = new CustomInstructionsUpdated
        {
            PreferencesId = this.Id,
            CustomInstructions = instructions,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Updates the UI preferences.
    /// </summary>
    /// <param name="theme">The theme.</param>
    /// <param name="language">The language.</param>
    public void UpdateUiPreferences(string theme, string language)
    {
        var @event = new UiPreferencesUpdated
        {
            PreferencesId = this.Id,
            Theme = theme,
            Language = language,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Updates the chat history settings.
    /// </summary>
    /// <param name="saveHistory">Whether to save chat history.</param>
    /// <param name="retentionDays">The retention days.</param>
    public void UpdateChatHistorySettings(bool saveHistory, int retentionDays)
    {
        var @event = new ChatHistorySettingsUpdated
        {
            PreferencesId = this.Id,
            SaveHistory = saveHistory,
            RetentionDays = retentionDays,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Updates the notification preferences.
    /// </summary>
    /// <param name="preferences">The notification preferences.</param>
    public void UpdateNotificationPreferences(NotificationPreferences preferences)
    {
        var @event = new NotificationPreferencesUpdated
        {
            PreferencesId = this.Id,
            Notifications = preferences,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Resets preferences to defaults.
    /// </summary>
    public void ResetToDefaults()
    {
        var @event = new PreferencesReset
        {
            PreferencesId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <inheritdoc/>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case UserChatPreferencesCreated created:
                this.ApplyCreated(created);
                break;
            case PreferredModelUpdated modelUpdated:
                this.ApplyModelUpdated(modelUpdated);
                break;
            case DefaultSettingsUpdated settingsUpdated:
                this.ApplySettingsUpdated(settingsUpdated);
                break;
            case CustomInstructionsUpdated instructionsUpdated:
                this.ApplyInstructionsUpdated(instructionsUpdated);
                break;
            case UiPreferencesUpdated uiUpdated:
                this.ApplyUiUpdated(uiUpdated);
                break;
            case ChatHistorySettingsUpdated historyUpdated:
                this.ApplyHistoryUpdated(historyUpdated);
                break;
            case NotificationPreferencesUpdated notificationsUpdated:
                this.ApplyNotificationsUpdated(notificationsUpdated);
                break;
            case PreferencesReset:
                this.ApplyReset();
                break;
        }
    }

    private void ApplyCreated(UserChatPreferencesCreated @event)
    {
        this.Id = @event.PreferencesId;
        this.UserId = @event.UserId;
        this.TenantId = @event.TenantId;
        this.PreferredModelId = @event.PreferredModelId;
        this.PreferredProviderId = @event.PreferredProviderId;
        this.DefaultTemperature = 0.7;
        this.DefaultMaxTokens = 4096;
        this.EnableStreamingByDefault = true;
        this.PreferredResponseFormat = ResponseFormat.Text;
        this.ThemePreference = "system";
        this.LanguagePreference = "en";
        this.SaveChatHistory = true;
        this.ChatHistoryRetentionDays = 30;
        this.Notifications = new NotificationPreferences();
        this.CreatedAt = @event.Timestamp;
        this.UpdatedAt = @event.Timestamp;
    }

    private void ApplyModelUpdated(PreferredModelUpdated @event)
    {
        this.PreferredModelId = @event.ModelId;
        this.PreferredProviderId = @event.ProviderId;
        this.UpdatedAt = @event.Timestamp;
    }

    private void ApplySettingsUpdated(DefaultSettingsUpdated @event)
    {
        this.DefaultSystemPrompt = @event.SystemPrompt;
        this.DefaultTemperature = @event.Temperature;
        this.DefaultMaxTokens = @event.MaxTokens;
        this.EnableStreamingByDefault = @event.EnableStreaming;
        this.PreferredResponseFormat = @event.ResponseFormat;
        this.UpdatedAt = @event.Timestamp;
    }

    private void ApplyInstructionsUpdated(CustomInstructionsUpdated @event)
    {
        this.CustomInstructions = @event.CustomInstructions;
        this.UpdatedAt = @event.Timestamp;
    }

    private void ApplyUiUpdated(UiPreferencesUpdated @event)
    {
        this.ThemePreference = @event.Theme;
        this.LanguagePreference = @event.Language;
        this.UpdatedAt = @event.Timestamp;
    }

    private void ApplyHistoryUpdated(ChatHistorySettingsUpdated @event)
    {
        this.SaveChatHistory = @event.SaveHistory;
        this.ChatHistoryRetentionDays = @event.RetentionDays;
        this.UpdatedAt = @event.Timestamp;
    }

    private void ApplyNotificationsUpdated(NotificationPreferencesUpdated @event)
    {
        this.Notifications = @event.Notifications;
        this.UpdatedAt = @event.Timestamp;
    }

    private void ApplyReset()
    {
        this.PreferredModelId = null;
        this.PreferredProviderId = null;
        this.DefaultSystemPrompt = null;
        this.DefaultTemperature = 0.7;
        this.DefaultMaxTokens = 4096;
        this.EnableStreamingByDefault = true;
        this.PreferredResponseFormat = ResponseFormat.Text;
        this.ThemePreference = "system";
        this.LanguagePreference = "en";
        this.SaveChatHistory = true;
        this.ChatHistoryRetentionDays = 30;
        this.CustomInstructions = null;
        this.Notifications = new NotificationPreferences();
        this.UpdatedAt = DateTime.UtcNow;
    }
}
