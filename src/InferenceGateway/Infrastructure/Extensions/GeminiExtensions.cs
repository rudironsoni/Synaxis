// <copyright file="GeminiExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;

    public static class GeminiExtensions
    {
        public static IServiceCollection AddGeminiClient(this IServiceCollection services, string apiKey, string modelId)
        {
            var client = new Google.GenAI.Client(vertexAI: false, apiKey: apiKey);

            services.AddChatClient(_ => client.AsIChatClient(modelId));

            return services;
        }
    }
}