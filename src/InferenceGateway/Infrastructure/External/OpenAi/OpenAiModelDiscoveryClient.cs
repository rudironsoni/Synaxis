// <copyright file="OpenAiModelDiscoveryClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.OpenAi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Infrastructure.External.OpenAi.Dto;

    /// <summary>
    /// OpenAiModelDiscoveryClient class.
    /// </summary>
    public class OpenAiModelDiscoveryClient : IOpenAiModelDiscoveryClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAiModelDiscoveryClient> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAiModelDiscoveryClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making requests.</param>
        /// <param name="logger">The logger instance.</param>
        public OpenAiModelDiscoveryClient(HttpClient httpClient, ILogger<OpenAiModelDiscoveryClient> logger)
        {
            this._httpClient = httpClient;
            this._logger = logger;
        }

        /// <inheritdoc/>
        public async Task<IList<string>> GetModelsAsync(string baseUrl, string apiKey, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException(nameof(baseUrl));
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey));
            }

            var trimmed = baseUrl.TrimEnd('/');
            var url = trimmed + "/v1/models";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                using var resp = await this._httpClient.SendAsync(request, ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    this._logger.LogWarning("Model discovery call to {Url} returned {Status}", url, resp.StatusCode);
                    return new List<string>();
                }

                var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                var dto = await JsonSerializer.DeserializeAsync<OpenAiModelsResponse>(stream, cancellationToken: ct).ConfigureAwait(false);
                if (dto == null || dto.Data == null)
                {
                    return new List<string>();
                }

                return dto.Data.Where(d => !string.IsNullOrEmpty(d.Id)).Select(d => d.Id!).ToList();
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error discovering models from {Url}", url);
                return new List<string>();
            }
        }
    }
}
