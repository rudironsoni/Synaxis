// <copyright file="RetryableTestCase.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Execution;

using Xunit.Abstractions;
using Xunit.Sdk;

/// <summary>
/// A test case that supports automatic retries on failure.
/// </summary>
public class RetryableTestCase : XunitTestCase
{
    private int maxRetries = 3;
    private int retryDelayMilliseconds = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryableTestCase"/> class.
    /// Required for deserialization.
    /// </summary>
    public RetryableTestCase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryableTestCase"/> class.
    /// </summary>
    /// <param name="diagnosticMessageSink">The diagnostic message sink.</param>
    /// <param name="defaultMethodDisplay">The default method display.</param>
    /// <param name="defaultMethodDisplayOptions">The default method display options.</param>
    /// <param name="testMethod">The test method.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="retryDelayMilliseconds">Delay between retries in milliseconds.</param>
    public RetryableTestCase(
        IMessageSink diagnosticMessageSink,
        TestMethodDisplay defaultMethodDisplay,
        TestMethodDisplayOptions defaultMethodDisplayOptions,
        ITestMethod testMethod,
        int maxRetries = 3,
        int retryDelayMilliseconds = 1000)
        : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod)
    {
        this.maxRetries = maxRetries;
        this.retryDelayMilliseconds = retryDelayMilliseconds;
    }

    /// <inheritdoc/>
    public override void Deserialize(IXunitSerializationInfo data)
    {
        base.Deserialize(data);
        this.maxRetries = data.GetValue<int>(nameof(this.maxRetries));
        this.retryDelayMilliseconds = data.GetValue<int>(nameof(this.retryDelayMilliseconds));
    }

    /// <inheritdoc/>
    public override void Serialize(IXunitSerializationInfo data)
    {
        base.Serialize(data);
        data.AddValue(nameof(this.maxRetries), this.maxRetries);
        data.AddValue(nameof(this.retryDelayMilliseconds), this.retryDelayMilliseconds);
    }

    /// <summary>
    /// Runs the test case with retry logic.
    /// </summary>
    /// <param name="diagnosticMessageSink">The diagnostic message sink.</param>
    /// <param name="messageBus">The message bus.</param>
    /// <param name="constructorArguments">The constructor arguments.</param>
    /// <param name="aggregator">The exception aggregator.</param>
    /// <param name="cancellationTokenSource">The cancellation token source.</param>
    /// <returns>The run summary.</returns>
    public override async Task<RunSummary> RunAsync(
        IMessageSink diagnosticMessageSink,
        IMessageBus messageBus,
        object[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        var runCount = 0;
        var lastResult = new RunSummary();

        while (runCount <= this.maxRetries)
        {
            runCount++;

            // Run the test
            lastResult = await base.RunAsync(
                diagnosticMessageSink,
                messageBus,
                constructorArguments,
                aggregator,
                cancellationTokenSource);

            // If the test passed, return immediately
            if (lastResult.Failed == 0)
            {
                if (runCount > 1)
                {
                    diagnosticMessageSink.OnMessage(new DiagnosticMessage(
                        $"Test '{this.DisplayName}' passed on attempt {runCount} after {runCount - 1} retry(s)."));
                }

                return lastResult;
            }

            // If we've reached max retries, return the failure
            if (runCount > this.maxRetries)
            {
                diagnosticMessageSink.OnMessage(new DiagnosticMessage(
                    $"Test '{this.DisplayName}' failed after {runCount} attempts (including {this.maxRetries} retries)."));

                return lastResult;
            }

            // Wait before retrying
            diagnosticMessageSink.OnMessage(new DiagnosticMessage(
                $"Test '{this.DisplayName}' failed on attempt {runCount}. Retrying in {this.retryDelayMilliseconds}ms..."));

            await Task.Delay(this.retryDelayMilliseconds, cancellationTokenSource.Token);
        }

        return lastResult;
    }
}
