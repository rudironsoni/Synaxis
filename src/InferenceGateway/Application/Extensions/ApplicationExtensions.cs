// <copyright file="ApplicationExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Extensions
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.InferenceGateway.Application.Configuration;
    using Synaxis.InferenceGateway.Application.Routing;
    using Synaxis.InferenceGateway.Application.Translation;

    /// <summary>
    /// Extension methods for registering application services.
    /// </summary>
    public static class ApplicationExtensions
    {
        /// <summary>
        /// Adds Synaxis application services to the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddSynaxisApplication(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Register Configuration
            services.Configure<SynaxisConfiguration>(configuration.GetSection("Synaxis:InferenceGateway"));

            // 2. Register Registry
            services.AddSingleton<IProviderRegistry, ProviderRegistry>();
            services.AddScoped<IModelResolver, ModelResolver>();
            services.AddScoped<ISmartRouter, SmartRouter>();

            services.AddSingleton<ITranslationPipeline, TranslationPipeline>();
            services.AddSingleton<IToolNormalizer, OpenAIToolNormalizer>();
            services.AddSingleton<IRequestTranslator, NoOpRequestTranslator>();
            services.AddSingleton<IResponseTranslator, NoOpResponseTranslator>();
            services.AddSingleton<IStreamingTranslator, NoOpStreamingTranslator>();

            return services;
        }
    }
}
