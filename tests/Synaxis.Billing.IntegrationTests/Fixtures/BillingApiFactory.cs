// <copyright file="BillingApiFactory.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Billing.IntegrationTests.Fixtures;

using System.Security.Claims;
using global::Billing.Infrastructure.Data;
#nullable enable
using global::Billing.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

/// <summary>
/// Factory for creating a test server for Billing API integration tests.
/// </summary>
public sealed class BillingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private string? _connectionString;
    private readonly Mock<IPaymentGateway> _mockPaymentGateway;
    private Guid _organizationId;
    private List<string> _roles = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BillingApiFactory"/> class.
    /// </summary>
    public BillingApiFactory()
    {
        this._mockPaymentGateway = new Mock<IPaymentGateway>();
    }

    /// <summary>
    /// Gets the mock payment gateway for test assertions.
    /// </summary>
    public Mock<IPaymentGateway> MockPaymentGateway => this._mockPaymentGateway;

    /// <summary>
    /// Configures the payment gateway mock for testing.
    /// </summary>
    /// <param name="setup">Setup action for the mock.</param>
    public void ConfigurePaymentGateway(Action<Mock<IPaymentGateway>> setup) => setup(this._mockPaymentGateway);

    /// <summary>
    /// Sets the authentication options for the test.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="roles">The roles.</param>
    public void SetAuthOptions(Guid organizationId, params string[] roles)
    {
        this._organizationId = organizationId;
        this._roles = new List<string>(roles);
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:ApiKey"] = "sk_test_fake_key_for_testing",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<BillingDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add test database context
            services.AddDbContext<BillingDbContext>(options =>
            {
                if (!string.IsNullOrEmpty(this._connectionString))
                {
                    options.UseNpgsql(this._connectionString);
                }
                else
                {
                    options.UseInMemoryDatabase("BillingIntegrationTests");
                }
            });

            // Replace payment gateway with mock
            var gatewayDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPaymentGateway));
            if (gatewayDescriptor != null)
            {
                services.Remove(gatewayDescriptor);
            }

            services.AddSingleton(this._mockPaymentGateway.Object);

            // Configure test authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<TestAuthOptions, TestAuthHandler>("Test", options =>
            {
                options.OrganizationId = this._organizationId;
                options.Roles = this._roles;
            });
        });
    }

    /// <summary>
    /// Sets the connection string for the test database.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    public void SetConnectionString(string connectionString) => this._connectionString = connectionString;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        // Database will be initialized via IAsyncLifetime in the test class
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
