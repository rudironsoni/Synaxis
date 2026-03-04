// <copyright file="PreferencesController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Api.Controllers;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for user preferences management.
/// </summary>
[ApiController]
[Route("api/preferences")]
public class PreferencesController : ControllerBase
{
    private readonly ILogger<PreferencesController> logger;
    private static readonly Dictionary<string, UserPreferences> UserPreferencesStore = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PreferencesController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public PreferencesController(ILogger<PreferencesController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Gets user preferences by user ID.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user preferences.</returns>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(UserPreferences), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetPreferencesAsync(
        [FromRoute, Required] string userId,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Getting preferences for user {UserId}", userId);

        if (!UserPreferencesStore.TryGetValue(userId, out var preferences))
        {
            return Task.FromResult<IActionResult>(this.NotFound(new { error = "Preferences not found" }));
        }

        return Task.FromResult<IActionResult>(this.Ok(preferences));
    }

    /// <summary>
    /// Creates user preferences.
    /// </summary>
    /// <param name="request">The preferences creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created preferences.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserPreferences), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> CreatePreferencesAsync(
        [FromBody] CreatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Creating preferences for user {UserId}", request.UserId);

        if (UserPreferencesStore.ContainsKey(request.UserId))
        {
            return Task.FromResult<IActionResult>(this.Conflict(new { error = "Preferences already exist for this user" }));
        }

        var preferences = new UserPreferences
        {
            UserId = request.UserId,
            DefaultModel = request.DefaultModel,
            Temperature = request.Temperature ?? 0.7,
            MaxTokens = request.MaxTokens,
            StreamingEnabled = request.StreamingEnabled ?? true,
            Theme = request.Theme ?? "system",
            Language = request.Language ?? "en",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        UserPreferencesStore[request.UserId] = preferences;

        return Task.FromResult<IActionResult>(this.Created($"/api/preferences/{request.UserId}", preferences));
    }

    /// <summary>
    /// Updates user preferences.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="request">The preferences update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated preferences.</returns>
    [HttpPut("{userId}")]
    [ProducesResponseType(typeof(UserPreferences), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> UpdatePreferencesAsync(
        [FromRoute, Required] string userId,
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Updating preferences for user {UserId}", userId);

        if (!UserPreferencesStore.TryGetValue(userId, out var preferences))
        {
            return Task.FromResult<IActionResult>(this.NotFound(new { error = "Preferences not found" }));
        }

        // Update only provided fields
        if (request.DefaultModel is not null)
        {
            preferences.DefaultModel = request.DefaultModel;
        }

        if (request.Temperature.HasValue)
        {
            preferences.Temperature = request.Temperature.Value;
        }

        if (request.MaxTokens.HasValue)
        {
            preferences.MaxTokens = request.MaxTokens.Value;
        }

        if (request.StreamingEnabled.HasValue)
        {
            preferences.StreamingEnabled = request.StreamingEnabled.Value;
        }

        if (request.Theme is not null)
        {
            preferences.Theme = request.Theme;
        }

        if (request.Language is not null)
        {
            preferences.Language = request.Language;
        }

        preferences.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<IActionResult>(this.Ok(preferences));
    }

    /// <summary>
    /// Changes the default model for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="request">The default model change request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated preferences.</returns>
    [HttpPut("{userId}/default-model")]
    [ProducesResponseType(typeof(UserPreferences), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> ChangeDefaultModelAsync(
        [FromRoute, Required] string userId,
        [FromBody] ChangeDefaultModelRequest request,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Changing default model for user {UserId} to {ModelId}", userId, request.ModelId);

        if (string.IsNullOrWhiteSpace(request.ModelId))
        {
            return Task.FromResult<IActionResult>(this.BadRequest(new { error = "Model ID is required" }));
        }

        if (!UserPreferencesStore.TryGetValue(userId, out var preferences))
        {
            return Task.FromResult<IActionResult>(this.NotFound(new { error = "Preferences not found" }));
        }

        preferences.DefaultModel = request.ModelId;
        preferences.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<IActionResult>(this.Ok(preferences));
    }
}

/// <summary>
/// Represents user preferences.
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default model.
    /// </summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// Gets or sets the default temperature.
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Gets or sets the default max tokens.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether streaming is enabled by default.
    /// </summary>
    public bool StreamingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the UI theme.
    /// </summary>
    public string Theme { get; set; } = "system";

    /// <summary>
    /// Gets or sets the preferred language.
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request to create user preferences.
/// </summary>
public class CreatePreferencesRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default model.
    /// </summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// Gets or sets the default temperature.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Gets or sets the default max tokens.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether streaming is enabled by default.
    /// </summary>
    public bool? StreamingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the UI theme.
    /// </summary>
    public string? Theme { get; set; }

    /// <summary>
    /// Gets or sets the preferred language.
    /// </summary>
    public string? Language { get; set; }
}

/// <summary>
/// Request to update user preferences.
/// </summary>
public class UpdatePreferencesRequest
{
    /// <summary>
    /// Gets or sets the default model.
    /// </summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// Gets or sets the default temperature.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Gets or sets the default max tokens.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether streaming is enabled by default.
    /// </summary>
    public bool? StreamingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the UI theme.
    /// </summary>
    public string? Theme { get; set; }

    /// <summary>
    /// Gets or sets the preferred language.
    /// </summary>
    public string? Language { get; set; }
}

/// <summary>
/// Request to change the default model.
/// </summary>
public class ChangeDefaultModelRequest
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
}
