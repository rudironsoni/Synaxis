// <copyright file="NvidiaExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;

    public static class NvidiaExtensions
    {
        public static IServiceCollection AddNvidiaClient(this IServiceCollection services, string serviceKey, string apiKey, string modelId)
        {
            services.AddKeyedSingleton<IChatClient>(serviceKey, (_, _) => new GenericOpenAiChatClient(
                apiKey,
                new Uri("https://integrate.api.nvidia.com/v1"),
                modelId));

            return services;
        }
    }
}
