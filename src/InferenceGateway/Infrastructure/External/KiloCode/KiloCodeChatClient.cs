// <copyright file="KiloCodeChatClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.KiloCode
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Synaxis.InferenceGateway.Infrastructure;

    /// <summary>
    /// KiloCodeChatClient class.
    /// </summary>
    public class KiloCodeChatClient : GenericOpenAiChatClient
    {
        private const string KiloApiUrl = "https://api.kilo.ai/api/openrouter";

        /// <summary>
        /// Initializes a new instance of the <see cref="KiloCodeChatClient"/> class.
        /// </summary>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="modelId">The model identifier to use.</param>
        /// <param name="httpClient">Optional HTTP client instance.</param>
        public KiloCodeChatClient(string apiKey, string modelId, HttpClient? httpClient = null)
            : base(apiKey, new Uri(KiloApiUrl), modelId, GetKiloHeaders(), httpClient)
        {
        }

        private static Dictionary<string, string> GetKiloHeaders()
        {
            return new Dictionary<string, string>
            {
                { "X-KiloCode-EditorName", "Synaxis" },
                { "X-KiloCode-Version", "1.0.0" },
                { "X-KiloCode-TaskId", "synaxis-inference" },
            };
        }
    }
}
