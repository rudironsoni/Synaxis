using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Infrastructure.Providers.Tier1;
using ContextSavvy.LlmProviders.Infrastructure.Providers.Tier2;
using ContextSavvy.LlmProviders.Infrastructure.Providers.Tier3;
using ContextSavvy.LlmProviders.Infrastructure.Providers.Tier4;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContextSavvy.LlmProviders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLlmProvidersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Tier 1 (Free/Fast)
        services.AddScoped<ILlmProvider, CloudflareProvider>();
        services.AddScoped<ILlmProvider, CohereProvider>();
        services.AddScoped<ILlmProvider, DeepInfraProvider>();
        services.AddScoped<ILlmProvider, LambdaChatProvider>();
        services.AddScoped<ILlmProvider, OpenAIFMProvider>();
        services.AddScoped<ILlmProvider, PerplexityProvider>();
        services.AddScoped<ILlmProvider, PollinationsProvider>();
        services.AddScoped<ILlmProvider, QwenProvider>();
        services.AddScoped<ILlmProvider, TogetherAIProvider>();

        // Tier 2 (Standard)
        services.AddScoped<ILlmProvider, AnthropicProvider>();
        services.AddScoped<ILlmProvider, CohereAPIProvider>();
        services.AddScoped<ILlmProvider, GroqProvider>();
        services.AddScoped<ILlmProvider, HuggingFaceProvider>();
        services.AddScoped<ILlmProvider, NousResearchProvider>();
        services.AddScoped<ILlmProvider, NVIDIAProvider>();
        services.AddScoped<ILlmProvider, OpenRouterProvider>();
        services.AddScoped<ILlmProvider, ReplicateProvider>();
        services.AddScoped<ILlmProvider, xAIProvider>();

        // Tier 3 (Ghost/Browser)
        services.AddScoped<ILlmProvider, DesignerProvider>();
        services.AddScoped<ILlmProvider, GeminiProvider>();
        services.AddScoped<ILlmProvider, HuggingChatProvider>();
        services.AddScoped<ILlmProvider, LMArenaProvider>();
        services.AddScoped<ILlmProvider, MetaAIProvider>();
        services.AddScoped<ILlmProvider, NousBrowserProvider>();
        services.AddScoped<ILlmProvider, PuterJSProvider>();
        services.AddScoped<CookieManager>(); // Helper for Tier 3

        // Tier 4 (Experimental/Ghost Driver)
        services.AddScoped<ILlmProvider, GeminiBrowserProvider>();
        services.AddScoped<ILlmProvider, GrokProvider>();

        return services;
    }
}
