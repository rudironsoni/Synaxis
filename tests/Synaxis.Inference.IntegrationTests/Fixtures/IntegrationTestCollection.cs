// <copyright file="IntegrationTestCollection.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.IntegrationTests.Fixtures;

using Xunit;

/// <summary>
/// Collection definition for integration tests using the CustomWebApplicationFactory.
/// </summary>
[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    // This class has no code, and is never created.
    // Its purpose is to be the place to apply [CollectionDefinition]
    // and all the ICollectionFixture<> interfaces.
}
