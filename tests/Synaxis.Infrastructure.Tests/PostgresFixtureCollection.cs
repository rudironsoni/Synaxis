// <copyright file="PostgresFixtureCollection.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Xunit;

namespace Synaxis.Infrastructure.Tests;

/// <summary>
/// Collection definition for PostgreSQL integration tests.
/// </summary>
[CollectionDefinition("PostgresIntegration", DisableParallelization = true)]
public sealed class PostgresFixtureCollection : ICollectionFixture<Synaxis.Common.Tests.Fixtures.PostgresFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
