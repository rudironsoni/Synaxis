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

        public SecurityConfigurationValidator(
            IConfiguration configuration,
            ILogger<SecurityConfigurationValidator> logger,
            string environmentName = "Production")
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isDevelopment = environmentName == "Development";
        }

        /// <summary>
        /// Validates all security settings and returns validation results.
        /// </summary>
        public SecurityValidationResult Validate()
        {
            var result = new SecurityValidationResult();

            ValidateJwtSecret(result);
            ValidateRateLimiting(result);
            ValidateCorsOrigins(result);
            ValidateSecurityHeaders(result);

            if (result.HasErrors)
            {
                _logger.LogError("Security configuration validation failed with {ErrorCount} errors", result.Errors.Count);
                foreach (var error in result.Errors)
                {
                    _logger.LogError("Security Error: {Error}", error);
                }
            }
            else
            {
                _logger.LogInformation("Security configuration validation passed successfully");
            }

            if (result.HasWarnings)
            {
                foreach (var warning in result.Warnings)
                {
                    _logger.LogWarning("Security Warning: {Warning}", warning);
                }
            }

            return result;
        }

        private void ValidateJwtSecret(SecurityValidationResult result)
        {
            const string defaultSecret = "SynaxisDefaultSecretKeyDoNotUseInProd1234567890";
            var jwtSecret = _configuration["Synaxis:InferenceGateway:JwtSecret"];

            if (string.IsNullOrWhiteSpace(jwtSecret))
            {
                result.AddError("JWT Secret is not configured. Set 'Synaxis:InferenceGateway:JwtSecret' in configuration.");
                return;
            }

            if (jwtSecret.Length < 32)
            {
                result.AddError($"JWT Secret must be at least 32 characters long. Current length: {jwtSecret.Length}");
            }

            if (jwtSecret == defaultSecret)
            {
                if (_isDevelopment)
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
            var providersConfig = _configuration.GetSection("Synaxis:InferenceGateway:Providers");
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

            if (!hasRateLimits && !_isDevelopment)
            {
                result.AddWarning("No rate limiting configured for any provider. Consider setting RateLimitRPM/RateLimitTPM values.");
            }
        }

        private void ValidateCorsOrigins(SecurityValidationResult result)
        {
            var webAppOrigins = _configuration["Synaxis:InferenceGateway:Cors:WebAppOrigins"];
            var publicOrigins = _configuration["Synaxis:InferenceGateway:Cors:PublicOrigins"];

            if (string.IsNullOrWhiteSpace(webAppOrigins) && string.IsNullOrWhiteSpace(publicOrigins))
            {
                if (_isDevelopment)
                {
                    result.AddWarning("CORS origins not configured. Using development defaults.");
                }
                else
                {
                    result.AddWarning("CORS origins not configured in production. This may cause issues with web clients.");
                }
            }

            // Check for wildcard in production
            if (!_isDevelopment && (publicOrigins?.Contains("*") == true || webAppOrigins?.Contains("*") == true))
            {
                result.AddError("Wildcard (*) CORS origin detected in non-development environment. This is a security risk.");
            }
        }

        private void ValidateSecurityHeaders(SecurityValidationResult result)
        {
            // This is validated by the middleware itself, but we can check configuration if needed
            // For now, we'll just log that security headers will be enforced
            _logger.LogDebug("Security headers enforcement will be handled by SecurityHeadersMiddleware");
        }
    }

    /// <summary>
    /// Result of security configuration validation.
    /// </summary>
    public class SecurityValidationResult
    {
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;
        public bool IsValid => !HasErrors;

        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);
    }
}