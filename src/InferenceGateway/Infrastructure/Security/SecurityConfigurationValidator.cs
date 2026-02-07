// <copyright file="SecurityConfigurationValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Security
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Synaxis.InferenceGateway.Application.Configuration;

    /// <summary>
    /// Validates critical security configurations at startup.
    /// Fails gracefully if misconfigured, providing clear error messages.
    /// </summary>
    public class SecurityConfigurationValidator
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecurityConfigurationValidator> _logger;
        private readonly bool _isDevelopment;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityConfigurationValidator"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="environmentName">The environment name (e.g., Development, Production).</param>
        public SecurityConfigurationValidator(
            IConfiguration configuration,
            ILogger<SecurityConfigurationValidator> logger,
            string environmentName = "Production")
        {
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._isDevelopment = string.Equals(environmentName, "Development", StringComparison.Ordinal);
        }

        /// <summary>
        /// Validates all security settings and returns validation results.
        /// </summary>
        /// <returns>A <see cref="SecurityValidationResult"/> containing validation errors and warnings.</returns>
        public SecurityValidationResult Validate()
        {
            var result = new SecurityValidationResult();

            this.ValidateJwtSecret(result);
            this.ValidateRateLimiting(result);
            this.ValidateCorsOrigins(result);
            this.ValidateSecurityHeaders();

            if (result.HasErrors)
            {
                this._logger.LogError("Security configuration validation failed with {ErrorCount} errors", result.Errors.Count);
                foreach (var error in result.Errors)
                {
                    this._logger.LogError("Security Error: {Error}", error);
                }
            }
            else
            {
                this._logger.LogInformation("Security configuration validation passed successfully");
            }

            if (result.HasWarnings)
            {
                foreach (var warning in result.Warnings)
                {
                    this._logger.LogWarning("Security Warning: {Warning}", warning);
                }
            }

            return result;
        }

        private void ValidateJwtSecret(SecurityValidationResult result)
        {
            const string defaultSecret = "SynaxisDefaultSecretKeyDoNotUseInProd1234567890";
            var jwtSecret = this._configuration["Synaxis:InferenceGateway:JwtSecret"];

            if (string.IsNullOrWhiteSpace(jwtSecret))
            {
                result.AddError("JWT Secret is not configured. Set 'Synaxis:InferenceGateway:JwtSecret' in configuration.");
                return;
            }

            if (jwtSecret.Length < 32)
            {
                result.AddError($"JWT Secret must be at least 32 characters long. Current length: {jwtSecret.Length}");
            }

            if (string.Equals(jwtSecret, defaultSecret, StringComparison.Ordinal))
            {
                if (this._isDevelopment)
                {
                    result.AddWarning("Default JWT secret detected. This is only acceptable in development.");
                }
                else
                {
                    result.AddError("Default JWT secret detected in non-development environment. This is a critical security risk.");
                }
            }

            // Check for common weak secrets
            var weakSecrets = new[] { "secret", "password", "12345678", "test", "admin" };
            if (weakSecrets.Any(weak => jwtSecret.ToLowerInvariant().Contains(weak)))
            {
                result.AddWarning("JWT Secret appears to contain common weak patterns. Consider using a cryptographically secure random string.");
            }
        }

        private void ValidateRateLimiting(SecurityValidationResult result)
        {
            var providersConfig = this._configuration.GetSection("Synaxis:InferenceGateway:Providers");
            var providers = providersConfig.GetChildren();

            var hasRateLimits = false;
            foreach (var provider in providers)
            {
                var rpmValue = provider["RateLimitRPM"];
                var tpmValue = provider["RateLimitTPM"];

                if (!string.IsNullOrEmpty(rpmValue) || !string.IsNullOrEmpty(tpmValue))
                {
                    hasRateLimits = true;
                    break;
                }
            }

            if (!hasRateLimits && !this._isDevelopment)
            {
                result.AddWarning("No rate limiting configured for any provider. Consider setting RateLimitRPM/RateLimitTPM values.");
            }
        }

        private void ValidateCorsOrigins(SecurityValidationResult result)
        {
            var webAppOrigins = this._configuration["Synaxis:InferenceGateway:Cors:WebAppOrigins"];
            var publicOrigins = this._configuration["Synaxis:InferenceGateway:Cors:PublicOrigins"];

            if (string.IsNullOrWhiteSpace(webAppOrigins) && string.IsNullOrWhiteSpace(publicOrigins))
            {
                if (this._isDevelopment)
                {
                    result.AddWarning("CORS origins not configured. Using development defaults.");
                }
                else
                {
                    result.AddWarning("CORS origins not configured in production. This may cause issues with web clients.");
                }
            }

            // Check for wildcard in production
            if (!this._isDevelopment && (publicOrigins?.Contains("*") == true || webAppOrigins?.Contains("*") == true))
            {
                result.AddError("Wildcard (*) CORS origin detected in non-development environment. This is a security risk.");
            }
        }

        private void ValidateSecurityHeaders()
        {
            // This is validated by the middleware itself, but we can check configuration if needed
            // For now, we'll just log that security headers will be enforced
            this._logger.LogDebug("Security headers enforcement will be handled by SecurityHeadersMiddleware");
        }
    }

    /// <summary>
    /// Result of security configuration validation.
    /// </summary>
    public class SecurityValidationResult
    {
        /// <summary>
        /// Gets the list of validation errors.
        /// </summary>
        public ICollection<string> Errors { get; } = new List<string>();

        /// <summary>
        /// Gets the list of validation warnings.
        /// </summary>
        public ICollection<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// Gets a value indicating whether there are any errors.
        /// </summary>
        public bool HasErrors => this.Errors.Count > 0;

        /// <summary>
        /// Gets a value indicating whether there are any warnings.
        /// </summary>
        public bool HasWarnings => this.Warnings.Count > 0;

        /// <summary>
        /// Gets a value indicating whether the validation passed (no errors).
        /// </summary>
        public bool IsValid => !this.HasErrors;

        /// <summary>
        /// Adds an error message to the validation result.
        /// </summary>
        /// <param name="error">The error message to add.</param>
        public void AddError(string error) => this.Errors.Add(error);

        /// <summary>
        /// Adds a warning message to the validation result.
        /// </summary>
        /// <param name="warning">The warning message to add.</param>
        public void AddWarning(string warning) => this.Warnings.Add(warning);
    }
}
