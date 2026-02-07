// <copyright file="OpenRouterExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;

    public static class OpenRouterExtensions
    {
        public static IServiceCollection AddOpenRouterClient(this IServiceCollection services, string serviceKey, string apiKey, string modelId = "auto", string? siteUrl = null, string? siteName = null)
        {
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(siteUrl)) headers.Add("HTTP-Referer", siteUrl);
            if (!string.IsNullOrEmpty(siteName)) headers.Add("X-Title", siteName);

            services.AddKeyedSingleton<IChatClient>(serviceKey, (_, _) => new GenericOpenAiChatClient(
                apiKey,
                new Uri("https://openrouter.ai/api/v1/"),
                modelId,
                headers));

            return services;
        }
    }
}
