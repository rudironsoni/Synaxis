// <copyright file="DependencyInjection.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure
{
    using Billing.Application.Services;
    using Billing.Infrastructure.Data;
    using Billing.Infrastructure.Repositories;
    using Billing.Infrastructure.Services;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension methods for registering billing infrastructure services.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds billing infrastructure services to the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration containing connection strings.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddBillingInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<BillingDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("BillingConnection"),
                    b => b.MigrationsAssembly("Billing.Infrastructure")));

            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<ICostSavingsRepository, CostSavingsRepository>();
            services.AddScoped<IUsageTrackingRepository, UsageTrackingRepository>();
            services.AddScoped<IPaymentGateway, StripePaymentGateway>();

            return services;
        }
    }
}
