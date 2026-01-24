using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Infrastructure.Providers.Tier1;
using ContextSavvy.LlmProviders.Infrastructure.Providers.Tier2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace ContextSavvy.LlmProviders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLlmProvidersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Tier 1 (Free/Fast)
        AddProvider<CloudflareProvider>(services);
        AddProvider<GeminiProvider>(services);
        AddProvider<GroqProvider>(services);
        AddProvider<HuggingFaceProvider>(services);
        AddProvider<OpenRouterProvider>(services);
        AddProvider<PollinationsProvider>(services);

        // Tier 2 (Standard)
        AddProvider<AnthropicProvider>(services);
        AddProvider<CohereAPIProvider>(services);
        AddProvider<CohereProvider>(services);
        AddProvider<DeepInfraProvider>(services);
        AddProvider<LambdaChatProvider>(services);
        AddProvider<NousResearchProvider>(services);
        AddProvider<NVIDIAProvider>(services);
        AddProvider<OpenAIFMProvider>(services);
        AddProvider<PerplexityProvider>(services);
        AddProvider<QwenProvider>(services);
        AddProvider<ReplicateProvider>(services);
        AddProvider<TogetherAIProvider>(services);
        AddProvider<xAIProvider>(services);

        return services;
    }

    private static void AddProvider<TImplementation>(IServiceCollection services) where TImplementation : class, ILlmProvider
    {
        services.AddHttpClient<TImplementation>()
                .AddStandardResilienceHandler();
        services.AddScoped<ILlmProvider>(sp => sp.GetRequiredService<TImplementation>());
    }
}
