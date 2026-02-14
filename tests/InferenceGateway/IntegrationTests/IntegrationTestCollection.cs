// <copyright file="IntegrationTestCollection.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests;

/// <summary>
/// Collection fixture for integration tests to share one web application factory instance.
/// The factory owns and shares infrastructure containers across the collection.
/// </summary>
[CollectionDefinition("Integration", DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<SynaxisWebApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
