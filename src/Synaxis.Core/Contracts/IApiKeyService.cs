// <copyright file="IApiKeyService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System.Threading.Tasks;

    /// <summary>
    /// Service for validating API keys.
    /// </summary>
    public interface IApiKeyService
    {
        /// <summary>
        /// Validates an API key.
        /// </summary>
        /// <param name="apiKey">The API key to validate.</param>
        /// <returns>The validation result.</returns>
        Task<ApiKeyValidationResult> ValidateApiKeyAsync(string apiKey);
    }

    /// <summary>
    /// Result of API key validation.
    /// </summary>
    public class ApiKeyValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether the API key is valid.
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// Gets the organization ID associated with the API key.
        /// </summary>
        public System.Guid? OrganizationId { get; init; }

        /// <summary>
        /// Gets the API key ID.
        /// </summary>
        public System.Guid? ApiKeyId { get; init; }

        /// <summary>
        /// Gets the error message if validation failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Gets the scopes associated with the API key.
        /// </summary>
        public string[] Scopes { get; init; } = System.Array.Empty<string>();
    }
}
