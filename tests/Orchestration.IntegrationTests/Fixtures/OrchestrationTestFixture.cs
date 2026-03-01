// <copyright file="OrchestrationTestFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.IntegrationTests.Fixtures;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;

/// <summary>
/// Test fixture for orchestration integration tests, providing a shared
/// WebApplicationFactory and HttpClient for all tests in the collection.
/// </summary>
public class OrchestrationTestFixture : IDisposable
{
    /// <summary>
    /// Gets the HTTP client for making requests to the test server.
    /// </summary>
    public HttpClient Client { get; }

    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationTestFixture"/> class.
    /// Creates a new WebApplicationFactory and HttpClient for testing.
    /// </summary>
    public OrchestrationTestFixture()
    {
        _factory = new WebApplicationFactory<Program>();
        Client = _factory.CreateClient();
    }

    /// <summary>
    /// Disposes the test fixture and releases resources.
    /// </summary>
    public void Dispose()
    {
        Client?.Dispose();
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Collection definition for orchestration integration tests.
/// Tests using this collection share the same <see cref="OrchestrationTestFixture"/> instance.
/// </summary>
[CollectionDefinition("Orchestration")]
public class OrchestrationCollection : ICollectionFixture<OrchestrationTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
