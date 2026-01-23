using ContextSavvy.LlmProviders.Application.Behaviors;
using ContextSavvy.LlmProviders.Application.Services;
using ContextSavvy.Core.Metrics;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ContextSavvy.LlmProviders.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLlmProvidersApplication(this IServiceCollection services)
    {
        services.AddSingleton<IMetricsCollector, MetricsCollector>();
        services.AddSingleton<UsageTracker>();

        services.AddMediator(options =>
        {
            options.Assemblies = [typeof(ServiceCollectionExtensions).Assembly];
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors = [
                typeof(LoggingBehavior<,>),
                typeof(ValidationBehavior<,>)
            ];
        });

        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        
        services.AddScoped<ITieredProviderRouter, TieredProviderRouter>();

        return services;
    }
}
