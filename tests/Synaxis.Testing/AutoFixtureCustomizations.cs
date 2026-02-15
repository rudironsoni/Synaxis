// <copyright file="AutoFixtureCustomizations.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Testing;

using System;
using AutoFixture;
using AutoFixture.Kernel;
using Synaxis.Abstractions.Cloud;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Customizations for AutoFixture to generate test data for Synaxis types.
/// </summary>
public static class AutoFixtureCustomizations
{
    /// <summary>
    /// Configures the fixture with Synaxis-specific customizations.
    /// </summary>
    /// <param name="fixture">The fixture to configure.</param>
    /// <returns>The configured fixture.</returns>
    public static IFixture ConfigureSynaxis(this IFixture fixture)
    {
        fixture.Customizations.Add(new TestDomainEventSpecimenBuilder());
        fixture.Customizations.Add(new TestAggregateSpecimenBuilder());
        fixture.Customize(new DomainEventCustomization());

        return fixture;
    }
}

/// <summary>
/// Specimen builder for <see cref="TestDomainEvent"/>.
/// </summary>
internal class TestDomainEventSpecimenBuilder : ISpecimenBuilder
{
    /// <inheritdoc />
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(TestDomainEvent))
        {
            return new TestDomainEvent(
                $"test-property-{Guid.NewGuid()}",
                new Random().Next(1, 1000));
        }

        return new NoSpecimen();
    }
}

/// <summary>
/// Specimen builder for <see cref="TestAggregate"/>.
/// </summary>
internal class TestAggregateSpecimenBuilder : ISpecimenBuilder
{
    /// <inheritdoc />
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(TestAggregate))
        {
            var aggregate = new TestAggregate($"test-aggregate-{Guid.NewGuid()}");
            var testEvent = context.Create<TestDomainEvent>();
            aggregate.ApplyTestEvent(testEvent);

            return aggregate;
        }

        return new NoSpecimen();
    }
}

/// <summary>
/// Customization for domain events.
/// </summary>
internal class DomainEventCustomization : ICustomization
{
    /// <inheritdoc />
    public void Customize(IFixture fixture)
    {
        fixture.Register<string>(() => $"test-{Guid.NewGuid()}");
        fixture.Register<int>(() => new Random().Next(1, 1000));
    }
}
