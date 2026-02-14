// <copyright file="RedisFixtureCollection.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests;

/// <summary>
/// Collection definition for Redis integration tests.
/// </summary>
[CollectionDefinition("RedisIntegration")]
public class RedisFixtureCollection : ICollectionFixture<Synaxis.Common.Tests.Fixtures.RedisFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
