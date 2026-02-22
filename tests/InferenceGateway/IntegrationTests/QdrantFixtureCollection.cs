// <copyright file="QdrantFixtureCollection.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests;

/// <summary>
/// Collection definition for Qdrant integration tests.
/// </summary>
[CollectionDefinition("QdrantIntegration", DisableParallelization = true)]
public class QdrantFixtureCollection : ICollectionFixture<Synaxis.Common.Tests.Fixtures.QdrantFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
