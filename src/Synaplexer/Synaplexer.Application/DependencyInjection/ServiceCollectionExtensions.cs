using Synaplexer.Application.Behaviors;
using Synaplexer.Application.Services;
using Synaplexer.Core.Metrics;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Synaplexer.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSynaplexerApplication(this IServiceCollection services)
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
