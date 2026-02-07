// <copyright file="IOpenAiModelDiscoveryClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.OpenAi
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IOpenAiModelDiscoveryClient
    {
        Task<List<string>> GetModelsAsync(string baseUrl, string apiKey, CancellationToken ct);
    }
}
