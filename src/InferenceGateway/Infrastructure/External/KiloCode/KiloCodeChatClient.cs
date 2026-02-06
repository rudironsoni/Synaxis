// <copyright file="KiloCodeChatClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.KiloCode
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Synaxis.InferenceGateway.Infrastructure;

    public class KiloCodeChatClient : GenericOpenAiChatClient
    {
        private const string KiloApiUrl = "https://api.kilo.ai/api/openrouter";

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
                { "X-KiloCode-TaskId", "synaxis-inference" }
            };
        }
    }
}