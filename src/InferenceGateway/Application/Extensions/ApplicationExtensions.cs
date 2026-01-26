using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.Application.Translation;

namespace Synaxis.InferenceGateway.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddSynaxisApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Register Configuration
        services.Configure<SynaxisConfiguration>(configuration.GetSection("Synaxis:InferenceGateway"));

        // 2. Register Registry
        services.AddSingleton<IProviderRegistry, ProviderRegistry>();
        services.AddScoped<IModelResolver, ModelResolver>();

        services.AddSingleton<ITranslationPipeline, TranslationPipeline>();
        services.AddSingleton<IToolNormalizer, OpenAIToolNormalizer>();
        services.AddSingleton<IRequestTranslator, NoOpRequestTranslator>();
        services.AddSingleton<IResponseTranslator, NoOpResponseTranslator>();
        services.AddSingleton<IStreamingTranslator, NoOpStreamingTranslator>();

        return services;
    }
}