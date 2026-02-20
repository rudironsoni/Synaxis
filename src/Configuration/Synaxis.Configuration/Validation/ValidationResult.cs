// <copyright file="ValidationResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Validation;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    private ValidationResult()
    {
        this.Errors = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class with errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    private ValidationResult(IEnumerable<string> errors)
    {
        this.Errors = errors?.ToList() ?? [];
    }

    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid => this.Errors.Count == 0;

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Gets the first validation error, or null if validation succeeded.
    /// </summary>
    public string? FirstError => this.Errors.Count > 0 ? this.Errors[0] : null;

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A successful validation result.</returns>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with the specified error.
    /// </summary>
    /// <param name="error">The validation error message.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(string error) => new([error]);

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation error messages.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(IEnumerable<string> errors) => new(errors);

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation error messages.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(params string[] errors) => new(errors);

    /// <summary>
    /// Combines multiple validation results into a single result.
    /// </summary>
    /// <param name="results">The validation results to combine.</param>
    /// <returns>A combined validation result.</returns>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var allErrors = results.SelectMany(r => r.Errors).ToList();
        return allErrors.Count == 0 ? Success() : new ValidationResult(allErrors);
    }
}
