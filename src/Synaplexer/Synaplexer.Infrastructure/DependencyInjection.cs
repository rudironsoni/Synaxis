using Synaplexer.Domain.Interfaces;
using Synaplexer.Infrastructure.Providers;
using Synaplexer.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Synaplexer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSynaplexerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ProvidersOptions>(configuration.GetSection("Providers"));

        // Registered Providers
        AddProvider<GeminiProvider>(services);
        AddProvider<GroqProvider>(services);
        AddProvider<OpenRouterProvider>(services);
        AddProvider<PollinationsProvider>(services);
        AddProvider<CohereProvider>(services);
        AddProvider<NVIDIAProvider>(services);

        return services;
    }

    private static void AddProvider<TImplementation>(IServiceCollection services) where TImplementation : class, ILlmProvider
    {
        services.AddHttpClient<TImplementation>()
                .AddStandardResilienceHandler().Configure(o =>
                {
                    o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
                    o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(120);
                    o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(240);
                });
        services.AddScoped<ILlmProvider>(sp => sp.GetRequiredService<TImplementation>());
    }
}
