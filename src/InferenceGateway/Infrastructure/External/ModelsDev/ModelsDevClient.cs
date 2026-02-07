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

    public class ModelsDevClient : IModelsDevClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ModelsDevClient>? _logger;

        public ModelsDevClient(HttpClient httpClient, ILogger<ModelsDevClient>? logger = null)
        {
            this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this._logger = logger;
        }

        public async Task<List<ModelDto>> GetAllModelsAsync(CancellationToken ct)
        {
            try
            {
                // Request the correct endpoint which returns JSON
                using var resp = await this._httpClient.GetAsync("/api.json", HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    this._logger?.LogWarning("Models.dev returned non-success status {Status}", resp.StatusCode);
                    return new List<ModelDto>();
                }

                var content = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(content))
                {
                    this._logger?.LogWarning("Models.dev returned empty content");
                    return new List<ModelDto>();
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                ModelsDevResponse? root;
                try
                {
                    root = JsonSerializer.Deserialize<ModelsDevResponse>(content, options);
                }
                catch (JsonException jex)
                {
                    // include a short preview of the response to aid debugging
                    var preview = content.Length > 512 ? content.Substring(0, 512) + "..." : content;
                    this._logger?.LogWarning(jex, "Failed to deserialize models.dev response. Content preview: {Preview}", preview);
                    return new List<ModelDto>();
                }

                var list = new List<ModelDto>();
                if (root == null)
                {
                    return list;
                }

                foreach (var provider in root.Values)
                {
                    if (provider?.models == null)
                    {
                        continue;
                    }

                    foreach (var kv in provider.models)
                    {
                        var model = kv.Value;
                        // ensure id present: if DTO id null, fallback to key
                        if (string.IsNullOrEmpty(model.id))
                        {
                            model.id = kv.Key;
                        }

                        list.Add(model);
                    }
                }

                return list;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Propagate cancellation
                throw;
            }
            catch (Exception ex)
            {
                this._logger?.LogWarning(ex, "Unexpected error while fetching models from models.dev");
                return new List<ModelDto>();
            }
        }
    }
}
