using Synaplexer.EventBus.Abstractions;
using Synaplexer.EventBus.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Synaplexer.EventBus.Extensions;

public static class EventBusExtensions
{
    public static IHostApplicationBuilder AddRabbitMQEventBus(this IHostApplicationBuilder builder, string connectionName)
    {
        // Simple registration for now, assuming connection string or configuration is handled via Aspire or appsettings
        builder.Services.AddSingleton<IEventBus, RabbitMQEventBus>();
        
        // Register connection factory (placeholder logic, usually depends on Aspire service discovery)
        builder.Services.AddSingleton<IConnectionFactory>(sp =>
        {
            var config = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var connectionString = config.GetConnectionString(connectionName);
            // Parse connection string or use default
            return new ConnectionFactory { HostName = "localhost" }; 
        });

        return builder;
    }
}
