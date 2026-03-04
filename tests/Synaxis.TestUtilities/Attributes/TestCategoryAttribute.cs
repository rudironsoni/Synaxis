// <copyright file="TestCategoryAttribute.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Attributes;

/// <summary>
/// Defines test categories for organizing and filtering tests.
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// Unit tests - fast, isolated tests that don't require external dependencies.
    /// </summary>
    public const string Unit = "Unit";

    /// <summary>
    /// Integration tests - tests that verify component interactions with real dependencies.
    /// </summary>
    public const string Integration = "Integration";

    /// <summary>
    /// End-to-end tests - full system tests from user perspective.
    /// </summary>
    public const string E2E = "E2E";

    /// <summary>
    /// Performance tests - tests that measure system performance characteristics.
    /// </summary>
    public const string Performance = "Performance";

    /// <summary>
    /// Smoke tests - quick tests to verify basic functionality.
    /// </summary>
    public const string Smoke = "Smoke";

    /// <summary>
    /// Regression tests - tests that verify bug fixes don't regress.
    /// </summary>
    public const string Regression = "Regression";
}

/// <summary>
/// Marks a test as belonging to a specific category.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class TestCategoryAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestCategoryAttribute"/> class.
    /// </summary>
    /// <param name="category">The test category.</param>
    public TestCategoryAttribute(string category)
    {
        this.Category = category;
    }

    /// <summary>
    /// Gets the test category.
    /// </summary>
    public string Category { get; }
}

/// <summary>
/// Marks a test as a unit test.
/// </summary>
public sealed class UnitTestAttribute : TestCategoryAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitTestAttribute"/> class.
    /// </summary>
    public UnitTestAttribute()
        : base(TestCategories.Unit)
    {
    }
}

/// <summary>
/// Marks a test as an integration test.
/// </summary>
public sealed class IntegrationTestAttribute : TestCategoryAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationTestAttribute"/> class.
    /// </summary>
    public IntegrationTestAttribute()
        : base(TestCategories.Integration)
    {
    }
}

/// <summary>
/// Marks a test as an end-to-end test.
/// </summary>
public sealed class E2ETestAttribute : TestCategoryAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="E2ETestAttribute"/> class.
    /// </summary>
    public E2ETestAttribute()
        : base(TestCategories.E2E)
    {
    }
}

/// <summary>
/// Marks a test as a performance test.
/// </summary>
public sealed class PerformanceTestAttribute : TestCategoryAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceTestAttribute"/> class.
    /// </summary>
    public PerformanceTestAttribute()
        : base(TestCategories.Performance)
    {
    }
}

/// <summary>
/// Marks a test as a smoke test.
/// </summary>
public sealed class SmokeTestAttribute : TestCategoryAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SmokeTestAttribute"/> class.
    /// </summary>
    public SmokeTestAttribute()
        : base(TestCategories.Smoke)
    {
    }
}

/// <summary>
/// Marks a test as a regression test.
/// </summary>
public sealed class RegressionTestAttribute : TestCategoryAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegressionTestAttribute"/> class.
    /// </summary>
    public RegressionTestAttribute()
        : base(TestCategories.Regression)
    {
    }
}
