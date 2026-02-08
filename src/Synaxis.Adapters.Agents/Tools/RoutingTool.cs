// <copyright file="RoutingTool.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Agents.Tools
{
    using System;
    using System.ComponentModel;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides routing information for selecting AI providers and models.
    /// </summary>
    public class RoutingTool
    {
        /// <summary>
        /// Gets routing information for a specific capability and model.
        /// </summary>
        /// <param name="capability">The AI capability required (e.g., "chat", "embedding", "image-generation").</param>
        /// <param name="model">The preferred model name (optional).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A JSON string containing routing information including provider and model recommendations.</returns>
        [Description("Get routing information for AI provider selection")]
        public Task<string> GetRoutingInfoAsync(
            string capability,
            string? model = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(capability))
            {
                throw new ArgumentException("Capability cannot be null or empty.", nameof(capability));
            }

            var routingInfo = new RoutingInfo
            {
                Capability = capability,
                RecommendedProvider = GetRecommendedProvider(capability, model),
                RecommendedModel = model ?? GetDefaultModel(capability),
                AlternativeProviders = GetAlternativeProviders(capability),
            };

            var json = JsonSerializer.Serialize(routingInfo, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            });

            return Task.FromResult(json);
        }

        /// <summary>
        /// Lists all available AI providers and their capabilities.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A JSON string containing provider information.</returns>
        [Description("List all available AI providers and their capabilities")]
        public Task<string> ListProvidersAsync(CancellationToken cancellationToken = default)
        {
            var providers = new[]
            {
                new ProviderInfo
                {
                    Name = "openai",
                    Capabilities = new[] { "chat", "embedding", "image-generation", "audio-transcription", "audio-synthesis" },
                    Models = new[] { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo", "dall-e-3", "text-embedding-3-large" },
                },
                new ProviderInfo
                {
                    Name = "anthropic",
                    Capabilities = new[] { "chat" },
                    Models = new[] { "claude-3-opus", "claude-3-sonnet", "claude-3-haiku" },
                },
                new ProviderInfo
                {
                    Name = "azure",
                    Capabilities = new[] { "chat", "embedding", "image-generation", "audio-transcription" },
                    Models = new[] { "gpt-4", "gpt-35-turbo", "text-embedding-ada-002" },
                },
            };

            var json = JsonSerializer.Serialize(providers, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            });

            return Task.FromResult(json);
        }

        private static string GetRecommendedProvider(string capability, string? model)
        {
            // Simple routing logic - can be enhanced with sophisticated routing
            return capability.ToLowerInvariant() switch
            {
                "chat" when model?.Contains("claude") == true => "anthropic",
                "chat" when model?.Contains("gpt") == true => "openai",
                "chat" => "openai",
                "embedding" => "openai",
                "image-generation" => "openai",
                "audio-transcription" => "openai",
                "audio-synthesis" => "openai",
                _ => "openai",
            };
        }

        private static string GetDefaultModel(string capability)
        {
            return capability.ToLowerInvariant() switch
            {
                "chat" => "gpt-4",
                "embedding" => "text-embedding-3-large",
                "image-generation" => "dall-e-3",
                "audio-transcription" => "whisper-1",
                "audio-synthesis" => "tts-1",
                _ => "gpt-4",
            };
        }

        private static string[] GetAlternativeProviders(string capability)
        {
            return capability.ToLowerInvariant() switch
            {
                "chat" => new[] { "anthropic", "azure" },
                "embedding" => new[] { "azure" },
                "image-generation" => new[] { "azure" },
                _ => Array.Empty<string>(),
            };
        }

        private sealed class RoutingInfo
        {
            public string Capability { get; set; } = string.Empty;

            public string RecommendedProvider { get; set; } = string.Empty;

            public string RecommendedModel { get; set; } = string.Empty;

            public string[] AlternativeProviders { get; set; } = Array.Empty<string>();
        }

        private sealed class ProviderInfo
        {
            public string Name { get; set; } = string.Empty;

            public string[] Capabilities { get; set; } = Array.Empty<string>();

            public string[] Models { get; set; } = Array.Empty<string>();
        }
    }
}
