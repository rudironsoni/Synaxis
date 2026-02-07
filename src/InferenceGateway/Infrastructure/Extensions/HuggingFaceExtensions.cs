// <copyright file="HuggingFaceExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;

    public static class HuggingFaceExtensions
    {
        public static IServiceCollection AddHuggingFaceClient(this IServiceCollection services, string serviceKey, string apiKey, string modelId)
        {
            services.AddKeyedSingleton<IChatClient>(serviceKey, (_, _) => new GenericOpenAiChatClient(
                apiKey,
                new Uri("https://router.huggingface.co/v1/"),
                modelId));

            return services;
        }
    }
}
