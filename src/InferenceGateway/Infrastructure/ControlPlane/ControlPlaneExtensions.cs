using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.InferenceGateway.Application.ControlPlane;

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane;

public static class ControlPlaneExtensions
{
    public static IServiceCollection AddControlPlane(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new ControlPlaneOptions();
        configuration.GetSection("Synaxis:ControlPlane").Bind(options);

        if (options.UseInMemory || string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            services.AddDbContext<ControlPlaneDbContext>(builder =>
                builder.UseInMemoryDatabase("SynaxisControlPlane"));
        }
        else
        {
            services.AddDbContext<ControlPlaneDbContext>(builder =>
                builder.UseNpgsql(options.ConnectionString));
        }

        services.AddScoped<IDeviationRegistry, DeviationRegistry>();

        return services;
    }
}
