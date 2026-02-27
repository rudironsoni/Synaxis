// <copyright file="AzureClient.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Azure
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Azure.Core;
    using global::Azure.Identity;
    using Microsoft.Extensions.Options;
    using Synaxis.Providers.Azure.Configuration;

    /// <summary>
    /// HTTP client wrapper for Azure OpenAI Service endpoints.
    /// </summary>
    public sealed class AzureClient : IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly AzureOpenAIOptions options;
        private readonly global::Azure.Core.TokenCredential? credential;
        private readonly SemaphoreSlim tokenLock;
        private string? cachedToken;
        private DateTimeOffset tokenExpiry;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use.</param>
        /// <param name="options">Azure OpenAI configuration options.</param>
        public AzureClient(HttpClient httpClient, IOptions<AzureOpenAIOptions> options)
        {
            this.httpClient = httpClient!;
            ArgumentNullException.ThrowIfNull(options);
            this.options = options.Value;
            this.options.Validate();

            this.tokenLock = new SemaphoreSlim(1, 1);
            this.tokenExpiry = DateTimeOffset.MinValue;

            if (this.options.UseAzureAd)
            {
                this.credential = new DefaultAzureCredential();
            }
        }

        /// <summary>
        /// Sends a POST request to the Azure OpenAI endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint path (e.g., "chat/completions").</param>
        /// <param name="requestBody">The request body as an object.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The response as an object.</returns>
        public async Task<object> PostAsync(
            string endpoint,
            object requestBody,
            CancellationToken cancellationToken = default)
        {
            var url = this.BuildUrl(endpoint);
            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            await this.SetAuthenticationHeaderAsync(request, cancellationToken).ConfigureAwait(false);

            var json = JsonSerializer.Serialize(requestBody);

            // Content is owned by the request and will be disposed when request is disposed
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await this.httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<object>(responseJson) ?? new { };
        }

        private string BuildUrl(string endpoint)
        {
            var baseUrl = this.options.Endpoint.TrimEnd('/');
            return $"{baseUrl}/openai/deployments/{this.options.DeploymentId}/{endpoint}?api-version={this.options.ApiVersion}";
        }

        private async Task SetAuthenticationHeaderAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (this.options.UseAzureAd)
            {
                var token = await this.GetAzureAdTokenAsync(cancellationToken).ConfigureAwait(false);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                request.Headers.Add("api-key", this.options.ApiKey);
            }
        }

        private async Task<string> GetAzureAdTokenAsync(CancellationToken cancellationToken)
        {
            if (this.credential == null)
            {
                throw new InvalidOperationException("Azure AD credential is not configured.");
            }

            await this.tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Check if cached token is still valid (with 5-minute buffer)
                if (this.cachedToken != null && DateTimeOffset.UtcNow < this.tokenExpiry.AddMinutes(-5))
                {
                    return this.cachedToken;
                }

                // Request new token
                var tokenContext = new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" });
                var token = await this.credential.GetTokenAsync(tokenContext, cancellationToken).ConfigureAwait(false);

                this.cachedToken = token.Token;
                this.tokenExpiry = token.ExpiresOn;

                return this.cachedToken;
            }
            finally
            {
                this.tokenLock.Release();
            }
        }

        /// <summary>
        /// Disposes the resources used by the Azure client.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.tokenLock.Dispose();
                this.disposed = true;
            }
        }
    }
}
