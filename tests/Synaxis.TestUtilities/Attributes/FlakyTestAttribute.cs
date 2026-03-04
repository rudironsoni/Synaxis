// <copyright file="FlakyTestAttribute.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Attributes;

using Xunit;
using Xunit.Sdk;

/// <summary>
/// Marks a test as flaky and allows automatic retries on failure.
/// Use this attribute sparingly and only for tests that have known intermittent failures.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("Xunit.Sdk.FactDiscoverer", "xunit.execution.{Platform}")]
public sealed class FlakyTestAttribute : FactAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlakyTestAttribute"/> class.
    /// </summary>
    public FlakyTestAttribute()
    {
        this.MaxRetries = 3;
        this.RetryDelayMilliseconds = 1000;
    }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the delay between retry attempts in milliseconds.
    /// </summary>
    public int RetryDelayMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the reason why this test is marked as flaky.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Marks a test as flaky with retry capability for theory tests.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("Xunit.Sdk.TheoryDiscoverer", "xunit.execution.{Platform}")]
public sealed class FlakyTheoryAttribute : TheoryAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlakyTheoryAttribute"/> class.
    /// </summary>
    public FlakyTheoryAttribute()
    {
        this.MaxRetries = 3;
        this.RetryDelayMilliseconds = 1000;
    }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the delay between retry attempts in milliseconds.
    /// </summary>
    public int RetryDelayMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the reason why this test is marked as flaky.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Marks a test that should be skipped when running in CI environments.
/// Useful for tests that are known to be flaky in CI but pass locally.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SkipInCIAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SkipInCIAttribute"/> class.
    /// </summary>
    public SkipInCIAttribute()
    {
        this.Reason = "Test is skipped when running in CI environment";
    }

    /// <summary>
    /// Gets or sets the skip reason.
    /// </summary>
    public string Reason { get; set; }

    /// <summary>
    /// Gets a value indicating whether the test should be skipped.
    /// </summary>
    public bool ShouldSkip => IsRunningInCI();

    /// <summary>
    /// Checks if the code is running in a CI environment.
    /// </summary>
    /// <returns>True if running in CI.</returns>
    private static bool IsRunningInCI()
    {
        // Check common CI environment variables
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CIRCLECI")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TRAVIS"));
    }
}
