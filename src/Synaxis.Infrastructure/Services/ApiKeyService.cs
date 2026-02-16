// <copyright file="ApiKeyService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for validating API keys.
    /// </summary>
    public class ApiKeyService : IApiKeyService
    {
        private readonly SynaxisDbContext _context;
        private readonly ILogger<ApiKeyService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiKeyService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="logger">The logger.</param>
        public ApiKeyService(SynaxisDbContext context, ILogger<ApiKeyService> logger)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<ApiKeyValidationResult> ValidateApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return CreateInvalidResult("API key is required.");
            }

            try
            {
                var matchingKey = await this.FindMatchingKeyAsync(apiKey).ConfigureAwait(false);

                if (matchingKey == null)
                {
                    this._logger.LogWarning("API key validation failed: No matching key found");
                    return CreateInvalidResult("Invalid API key.");
                }

                if (IsKeyExpired(matchingKey))
                {
                    this._logger.LogWarning("API key validation failed: Key expired");
                    return CreateInvalidResult("API key has expired.");
                }

                await this.UpdateLastUsedAsync(matchingKey).ConfigureAwait(false);

                this._logger.LogInformation(
                    "API key validated successfully for organization {OrganizationId}",
                    matchingKey.OrganizationId);

                return CreateValidResult(matchingKey);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error validating API key");
                return CreateInvalidResult("An error occurred while validating the API key.");
            }
        }

        private static ApiKeyValidationResult CreateInvalidResult(string errorMessage)
        {
            return new ApiKeyValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage,
            };
        }

        private static ApiKeyValidationResult CreateValidResult(OrganizationApiKey key)
        {
            var scopes = key.Permissions?.Keys.ToArray() ?? Array.Empty<string>();

            return new ApiKeyValidationResult
            {
                IsValid = true,
                OrganizationId = key.OrganizationId,
                ApiKeyId = key.Id,
                Scopes = scopes,
            };
        }

        private async Task<OrganizationApiKey?> FindMatchingKeyAsync(string apiKey)
        {
            var prefix = ExtractPrefix(apiKey);
            var keyHash = ComputeHash(apiKey);

            var query = this._context.OrganizationApiKeys
                .AsNoTracking()
                .Where(k => k.IsActive && !k.RevokedAt.HasValue);

            if (!string.IsNullOrEmpty(prefix))
            {
                query = query.Where(k => k.KeyPrefix == prefix);
            }

            var potentialKeys = await query.ToListAsync().ConfigureAwait(false);

            return potentialKeys.FirstOrDefault(k => string.Equals(k.KeyHash, keyHash, StringComparison.Ordinal));
        }

        private static string? ExtractPrefix(string apiKey)
        {
            if (apiKey.StartsWith("synaxis_", StringComparison.OrdinalIgnoreCase) && apiKey.Length >= 16)
            {
                return apiKey.Substring(8, 8);
            }

            return null;
        }

        private static bool IsKeyExpired(OrganizationApiKey key)
        {
            return key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow;
        }

        private Task UpdateLastUsedAsync(OrganizationApiKey key)
        {
            key.LastUsedAt = DateTime.UtcNow;
            return this._context.SaveChangesAsync();
        }

        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
