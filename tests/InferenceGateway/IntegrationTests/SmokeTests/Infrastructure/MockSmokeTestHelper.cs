// <copyright file="MockSmokeTestHelper.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Net.Http;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure
{
    /// <summary>
    /// Provides helper methods to configure smoke tests to use mock providers instead of real ones.
    /// This eliminates network dependencies and provides deterministic test results.
    /// </summary>
    public static class MockSmokeTestHelper
    {
        /// <summary>
        /// Creates an HttpClient configured with mock responses for testing.
        /// Use this instead of the real HttpClient to avoid hitting actual providers.
        /// </summary>
        /// <returns>HttpClient with mock handler configured.</returns>
        public static HttpClient CreateMockClient()
        {
            var mockHandler = new MockHttpHandler();
            return new HttpClient(mockHandler)
            {
                BaseAddress = new Uri("http://localhost"),
            };
        }

        /// <summary>
        /// Creates an HttpClient with custom mock responses.
        /// </summary>
        /// <param name="customResponses">Custom mock responses to use.</param>
        /// <returns>HttpClient with custom mock handler configured.</returns>
        public static HttpClient CreateMockClient(MockProviderResponses customResponses)
        {
            var mockHandler = new MockHttpHandler(customResponses);
            return new HttpClient(mockHandler)
            {
                BaseAddress = new Uri("http://localhost"),
            };
        }

        /// <summary>
        /// Creates a test configuration that enables mock mode.
        /// This can be used to conditionally use mocks in CI environments.
        /// </summary>
        /// <param name="useMocks">Whether to use mocks (true) or real providers (false).</param>
        /// <returns>Configuration object indicating mock mode.</returns>
        public static TestConfiguration CreateTestConfiguration(bool useMocks = true)
        {
            return new TestConfiguration
            {
                UseMocks = useMocks,
                EnableDetailedLogging = true,
                DefaultTimeoutMs = 30000,
                EnableCircuitBreaker = false, // Not needed for mocks
            };
        }

        /// <summary>
        /// Gets the current environment-based configuration.
        /// In CI, defaults to mock mode; in development, can be configured.
        /// </summary>
        /// <returns>Test configuration based on environment.</returns>
        public static TestConfiguration GetEnvironmentConfiguration()
        {
            var useMocks = Environment.GetEnvironmentVariable("SYNAPTIC_TEST_MOCKS")?.ToLowerInvariant() switch
            {
                "false" or "0" => false,
                _ => true // Default to mocks for stability
            };

            return CreateTestConfiguration(useMocks);
        }
    }

    /// <summary>
    /// Configuration for smoke test execution.
    /// </summary>
    public class TestConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether whether to use mock providers instead of real ones.
        /// </summary>
        public bool UseMocks { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether whether to enable detailed logging during test execution.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets default timeout for test requests in milliseconds.
        /// </summary>
        public int DefaultTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Gets or sets a value indicating whether whether to enable circuit breaker logic for real providers.
        /// This would only apply when UseMocks is false.
        /// </summary>
        public bool EnableCircuitBreaker { get; set; } = false;
    }
}
