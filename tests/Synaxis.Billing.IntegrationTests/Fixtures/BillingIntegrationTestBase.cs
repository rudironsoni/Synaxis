// <copyright file="BillingIntegrationTestBase.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Billing.IntegrationTests.Fixtures;

using global::Billing.Infrastructure.Data;
using Xunit;

/// <summary>
/// Base class for billing integration tests providing common setup.
/// </summary>
[Collection("BillingIntegration")]
public abstract class BillingIntegrationTestBase : IAsyncLifetime
{
    /// <summary>
    /// Gets the database fixture.
    /// </summary>
    protected BillingDatabaseFixture DatabaseFixture { get; }

    /// <summary>
    /// Gets the API factory.
    /// </summary>
    protected BillingApiFactory ApiFactory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BillingIntegrationTestBase"/> class.
    /// </summary>
    protected BillingIntegrationTestBase(BillingDatabaseFixture databaseFixture)
    {
        DatabaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        ApiFactory = new BillingApiFactory();
    }

    /// <inheritdoc />
    public virtual async Task InitializeAsync()
    {
        await DatabaseFixture.ResetDatabaseAsync();
        ApiFactory.SetConnectionString(DatabaseFixture.ConnectionString);
    }

    /// <inheritdoc />
    public virtual Task DisposeAsync()
    {
        return ApiFactory.DisposeAsync();
    }

    /// <summary>
    /// Resets the database to a clean state.
    /// </summary>
    protected async Task ResetDatabaseAsync()
    {
        await DatabaseFixture.ResetDatabaseAsync();
    }

    /// <summary>
    /// Creates a new database context.
    /// </summary>
    protected BillingDbContext CreateDbContext() => DatabaseFixture.CreateDbContext();
}
