// <copyright file="Program.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Synaxis.DependencyInjection;
using Synaxis.Providers.OpenAI.DependencyInjection;

namespace Synaxis.Samples.Microservices;

/// <summary>
/// Microservices sample demonstrating Synaxis in a distributed architecture.
/// This example shows how to use Synaxis with background services for
/// asynchronous processing across multiple services.
/// </summary>
public static class Program
{
    public static Task Main(string[] args)
    {
        Console.WriteLine("Synaxis Microservices Sample");
        Console.WriteLine("=============================\n");

        // Build host
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Add Synaxis
                var openAiApiKey = context.Configuration["OpenAI:ApiKey"]
                    ?? throw new InvalidOperationException("OpenAI:ApiKey not configured");

                services.AddSynaxis();
                services.AddOpenAIProvider(openAiApiKey);

                // Add logging
                services.AddLogging();

                // Register microservices
                services.AddSingleton<ChatMicroservice>();
                services.AddSingleton<EmbeddingMicroservice>();
                services.AddSingleton<SummarizationMicroservice>();

                // Register background services
                services.AddHostedService<ChatServiceWorker>();
                services.AddHostedService<EmbeddingServiceWorker>();
                services.AddHostedService<SummarizationServiceWorker>();
            })
            .Build();

        // Start host
        return host.RunAsync();
    }
}
