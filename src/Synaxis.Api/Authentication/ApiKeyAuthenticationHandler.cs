// <copyright file="ApiKeyAuthenticationHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Synaxis.Core.Contracts;

    /// <summary>
    /// Authentication handler for API key authentication.
    /// </summary>
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private const string ApiKeyPrefix = "synaxis_";
        private readonly IApiKeyService _apiKeyService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiKeyAuthenticationHandler"/> class.
        /// </summary>
        /// <param name="options">The monitor options.</param>
        /// <param name="logger">The logger factory.</param>
        /// <param name="encoder">The encoder.</param>
        /// <param name="apiKeyService">The API key service.</param>
        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IApiKeyService apiKeyService)
            : base(options, logger, encoder)
        {
            ArgumentNullException.ThrowIfNull(apiKeyService);
            this._apiKeyService = apiKeyService;
        }

        /// <inheritdoc/>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Check if Authorization header is present
            if (!this.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            {
                return AuthenticateResult.NoResult();
            }

            var authValue = authorizationHeader.ToString();

            // Check if it's a Bearer token
            if (!authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.NoResult();
            }

            var token = authValue.Substring(7).Trim();

            // Check if it looks like an API key
            if (!token.StartsWith(ApiKeyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.NoResult();
            }

            // Validate the API key
            var validationResult = await this._apiKeyService.ValidateApiKeyAsync(token).ConfigureAwait(false);

            if (!validationResult.IsValid)
            {
                return AuthenticateResult.Fail(validationResult.ErrorMessage ?? "Invalid API key");
            }

            // Create claims identity
            var claims = new List<Claim>
            {
                new Claim("organization_id", validationResult.OrganizationId?.ToString() ?? string.Empty),
                new Claim("api_key_id", validationResult.ApiKeyId?.ToString() ?? string.Empty),
                new Claim(ClaimTypes.AuthenticationMethod, "ApiKey"),
            };

            // Add scopes as claims
            foreach (var scope in validationResult.Scopes)
            {
                claims.Add(new Claim("scope", scope));
            }

            var identity = new ClaimsIdentity(claims, this.Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, this.Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
