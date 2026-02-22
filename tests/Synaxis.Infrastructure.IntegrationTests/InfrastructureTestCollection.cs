// <copyright file="InfrastructureTestCollection.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using Xunit;

/// <summary>
/// Collection definition for infrastructure integration tests.
/// Uses collection fixture to share TestContainers across tests.
/// </summary>
[CollectionDefinition("Infrastructure", DisableParallelization = true)]
public class InfrastructureTestCollection : ICollectionFixture<InfrastructureTestFixture>
{
}
