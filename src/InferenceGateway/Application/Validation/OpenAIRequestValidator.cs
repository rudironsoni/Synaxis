using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.InferenceGateway.Application.Validation;

/// <summary>
/// Validator for OpenAI-compatible request DTOs.
/// </summary>
public static class OpenAIRequestValidator
{
    /// <summary>
    /// Validates common OpenAI request parameters.
    /// </summary>
    public static ValidationResult ValidateRequest(string? model, object? messages, double? temperature, int? maxTokens)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(model))
        {
            errors.Add("Model is required and cannot be empty.");
        }

        if (messages == null)
        {
            errors.Add("Messages are required.");
        }

        if (temperature.HasValue && (temperature.Value < 0 || temperature.Value > 2))
        {
            errors.Add($"Temperature must be between 0 and 2. Got: {temperature.Value}");
        }

        if (maxTokens.HasValue && maxTokens.Value <= 0)
        {
            errors.Add($"MaxTokens must be greater than 0. Got: {maxTokens.Value}");
        }

        if (errors.Count > 0)
        {
            return new ValidationResult(string.Join(" ", errors));
        }

        return ValidationResult.Success!;
    }

    /// <summary>
    /// Validates model parameter.
    /// </summary>
    public static bool IsValidModel(string? model, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(model))
        {
            error = "Model is required and cannot be empty.";
            return false;
        }

        // Additional validation can be added here
        // For example, checking against a whitelist of allowed models
        
        return true;
    }

    /// <summary>
    /// Validates temperature parameter.
    /// </summary>
    public static bool IsValidTemperature(double? temperature, out string? error)
    {
        error = null;

        if (!temperature.HasValue)
        {
            return true; // Optional parameter
        }

        if (temperature.Value < 0 || temperature.Value > 2)
        {
            error = $"Temperature must be between 0 and 2. Got: {temperature.Value}";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates maxTokens parameter.
    /// </summary>
    public static bool IsValidMaxTokens(int? maxTokens, out string? error)
    {
        error = null;

        if (!maxTokens.HasValue)
        {
            return true; // Optional parameter
        }

        if (maxTokens.Value <= 0)
        {
            error = $"MaxTokens must be greater than 0. Got: {maxTokens.Value}";
            return false;
        }

        // Check for reasonable upper bound (e.g., 200k tokens)
        if (maxTokens.Value > 200000)
        {
            error = $"MaxTokens exceeds maximum allowed value of 200000. Got: {maxTokens.Value}";
            return false;
        }

        return true;
    }
}
