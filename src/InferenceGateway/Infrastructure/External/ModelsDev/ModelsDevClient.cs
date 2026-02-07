// <copyright file="ModelsDevClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.ModelsDev
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.Dto;

    /// <summary>
    /// ModelsDevClient class.
    /// </summary>
    public class ModelsDevClient : IModelsDevClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ModelsDevClient>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelsDevClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making requests.</param>
        /// <param name="logger">The optional logger instance.</param>
        public ModelsDevClient(HttpClient httpClient, ILogger<ModelsDevClient>? logger = null)
        {
            this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this._logger = logger;
        }

        /// <inheritdoc/>
        public async Task<IList<ModelDto>> GetAllModelsAsync(CancellationToken ct)
        {
            try
            {
                var content = await this.FetchModelsContentAsync(ct).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return new List<ModelDto>();
                }

                var root = this.DeserializeResponse(content);
                if (root == null)
                {
                    return new List<ModelDto>();
                }

                return ExtractModelDtos(root);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                this._logger?.LogWarning(ex, "Unexpected error while fetching models from models.dev");
                return new List<ModelDto>();
            }
        }

        private async Task<string?> FetchModelsContentAsync(CancellationToken ct)
        {
            using var resp = await this._httpClient.GetAsync("/api.json", HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                this._logger?.LogWarning("Models.dev returned non-success status {Status}", resp.StatusCode);
                return null;
            }

            var content = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(content))
            {
                this._logger?.LogWarning("Models.dev returned empty content");
                return null;
            }

            return content;
        }

        private ModelsDevResponse? DeserializeResponse(string content)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                return JsonSerializer.Deserialize<ModelsDevResponse>(content, options);
            }
            catch (JsonException jex)
            {
                var preview = content.Length > 512 ? content.Substring(0, 512) + "..." : content;
                this._logger?.LogWarning(jex, "Failed to deserialize models.dev response. Content preview: {Preview}", preview);
                return null;
            }
        }

        private static List<ModelDto> ExtractModelDtos(ModelsDevResponse root)
        {
            var list = new List<ModelDto>();

            foreach (var provider in root.Values)
            {
                if (provider?.Models == null)
                {
                    continue;
                }

                foreach (var kv in provider.Models)
                {
                    var model = kv.Value;
                    if (string.IsNullOrEmpty(model.Id))
                    {
                        model.Id = kv.Key;
                    }

                    list.Add(model);
                }
            }

            return list;
        }
    }
}
