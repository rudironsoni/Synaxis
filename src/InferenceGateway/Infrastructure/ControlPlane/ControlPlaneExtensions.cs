using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.InferenceGateway.Application.ControlPlane;

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane;

public static class ControlPlaneExtensions
{
    public static IServiceCollection AddControlPlane(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ControlPlaneDbContext>((sp, builder) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var options = new ControlPlaneOptions();
            config.GetSection("Synaxis:ControlPlane").Bind(options);

            if (options.UseInMemory || string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                builder.UseInMemoryDatabase("SynaxisControlPlane");
            }
            else
            {
                builder.UseNpgsql(options.ConnectionString);
            }
        });

        // Register SynaxisDbContext for Identity
        services.AddDbContext<SynaxisDbContext>((sp, builder) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var options = new ControlPlaneOptions();
            config.GetSection("Synaxis:ControlPlane").Bind(options);

            if (options.UseInMemory || string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                builder.UseInMemoryDatabase("SynaxisIdentity");
            }
            else
            {
                builder.UseNpgsql(options.ConnectionString);
            }
        });

        services.AddScoped<IDeviationRegistry, DeviationRegistry>();

        return services;
    }
}
