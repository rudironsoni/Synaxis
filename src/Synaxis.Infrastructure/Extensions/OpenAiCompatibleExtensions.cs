using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Synaxis.Infrastructure.Extensions;

public static class OpenAiCompatibleExtensions
{
    public static IServiceCollection AddOpenAiCompatibleClient(
        this IServiceCollection services,
        string key,
        string baseUrl,
        string apiKey,
        string? modelId = null,
        Dictionary<string, string>? customHeaders = null)
    {
        services.AddKeyedSingleton<IChatClient>(key, (sp, obj) =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(key);
            return new GenericOpenAiChatClient(apiKey, new Uri(baseUrl), modelId ?? "default", customHeaders, httpClient);
        });

        return services;
    }
}
