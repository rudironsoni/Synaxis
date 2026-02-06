// <copyright file="BespokeExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using System;
    using System.Net.Http;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;

    public static class BespokeExtensions
    {
        public static IServiceCollection AddCohereClient(this IServiceCollection services, string key, string apiKey, string modelId)
        {
            services.AddKeyedSingleton<IChatClient>(key, (sp, obj) =>
            {
                var httpClient = new HttpClient();
                return new CohereChatClient(httpClient, modelId, apiKey);
            });
            return services;
        }

        public static IServiceCollection AddPollinationsClient(this IServiceCollection services, string key, string modelId)
        {
            services.AddKeyedSingleton<IChatClient>(key, (sp, obj) =>
            {
                var httpClient = new HttpClient();
                return new PollinationsChatClient(httpClient, modelId);
            });
            return services;
        }
    }
}