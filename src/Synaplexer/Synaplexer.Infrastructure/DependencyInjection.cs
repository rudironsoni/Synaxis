using Synaplexer.Domain.Interfaces;
using Synaplexer.Infrastructure.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Synaplexer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSynaplexerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Registered Providers
        AddProvider<CloudflareProvider>(services);
        AddProvider<GeminiProvider>(services);
        AddProvider<GroqProvider>(services);
        AddProvider<HuggingFaceProvider>(services);
        AddProvider<OpenRouterProvider>(services);
        AddProvider<PollinationsProvider>(services);
        AddProvider<CohereProvider>(services);
        AddProvider<DeepInfraProvider>(services);
        AddProvider<NVIDIAProvider>(services);

        return services;
    }

    private static void AddProvider<TImplementation>(IServiceCollection services) where TImplementation : class, ILlmProvider
    {
        services.AddHttpClient<TImplementation>()
                .AddStandardResilienceHandler();
        services.AddScoped<ILlmProvider>(sp => sp.GetRequiredService<TImplementation>());
    }
}
