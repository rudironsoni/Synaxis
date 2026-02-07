// <copyright file="SynaxisDbSeeder.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane
{
    using Microsoft.EntityFrameworkCore;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Platform;

    /// <summary>
    /// Seeds initial data for the Synaxis database:
    /// - Platform providers (OpenAI, Anthropic, Google, etc.)
    /// - Platform models for each provider
    /// - System roles (SystemAdmin, OrganizationOwner, OrganizationAdmin, Member, Guest)
    /// </summary>
    public static class SynaxisDbSeeder
    {
        public static async Task SeedAsync(SynaxisDbContext context)
        {
            await SeedProvidersAsync(context);
            await SeedModelsAsync(context);
            await SeedSystemRolesAsync(context);
            await context.SaveChangesAsync();
        }

        private static async Task SeedProvidersAsync(SynaxisDbContext context)
        {
            if (await context.Providers.AnyAsync())
                return;

            var providers = new[]
            {
                new Provider
                {
                    Id = Guid.NewGuid(),
                    Key = "openai",
                    DisplayName = "OpenAI",
                    ProviderType = "OpenAI",
                    BaseEndpoint = "https://api.openai.com/v1",
                    DefaultApiKeyEnvironmentVariable = "OPENAI_API_KEY",
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsVision = true,
                    IsActive = true,
                    IsPublic = true,
                    IsFreeTier = false
                },
                new Provider
                {
                    Id = Guid.NewGuid(),
                    Key = "anthropic",
                    DisplayName = "Anthropic",
                    ProviderType = "Anthropic",
                    BaseEndpoint = "https://api.anthropic.com/v1",
                    DefaultApiKeyEnvironmentVariable = "ANTHROPIC_API_KEY",
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsVision = true,
                    IsActive = true,
                    IsPublic = true,
                    IsFreeTier = false
                },
                new Provider
                {
                    Id = Guid.NewGuid(),
                    Key = "google",
                    DisplayName = "Google AI (Gemini)",
                    ProviderType = "Google",
                    BaseEndpoint = "https://generativelanguage.googleapis.com/v1",
                    DefaultApiKeyEnvironmentVariable = "GOOGLE_API_KEY",
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsVision = true,
                    IsActive = true,
                    IsPublic = true,
                    IsFreeTier = true
                },
                new Provider
                {
                    Id = Guid.NewGuid(),
                    Key = "cohere",
                    DisplayName = "Cohere",
                    ProviderType = "Cohere",
                    BaseEndpoint = "https://api.cohere.ai/v1",
                    DefaultApiKeyEnvironmentVariable = "COHERE_API_KEY",
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsVision = false,
                    IsActive = true,
                    IsPublic = true,
                    IsFreeTier = true
                },
                new Provider
                {
                    Id = Guid.NewGuid(),
                    Key = "azure-openai",
                    DisplayName = "Azure OpenAI",
                    ProviderType = "Azure",
                    BaseEndpoint = null, // Custom per organization
                    DefaultApiKeyEnvironmentVariable = "AZURE_OPENAI_API_KEY",
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsVision = true,
                    IsActive = true,
                    IsPublic = true,
                    IsFreeTier = false
                },
                new Provider
                {
                    Id = Guid.NewGuid(),
                    Key = "aws-bedrock",
                    DisplayName = "AWS Bedrock",
                    ProviderType = "AWS",
                    BaseEndpoint = null, // Region-specific
                    DefaultApiKeyEnvironmentVariable = "AWS_ACCESS_KEY_ID",
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsVision = true,
                    IsActive = true,
                    IsPublic = true,
                    IsFreeTier = false
                },
                new Provider
                {
                    Id = Guid.NewGuid(),
                    Key = "cloudflare",
                    DisplayName = "Cloudflare Workers AI",
                    ProviderType = "Cloudflare",
                    BaseEndpoint = "https://api.cloudflare.com/client/v4",
                    DefaultApiKeyEnvironmentVariable = "CLOUDFLARE_API_KEY",
                    SupportsStreaming = true,
                    SupportsTools = false,
                    SupportsVision = false,
                    IsActive = true,
                    IsPublic = true,
                    IsFreeTier = true
                }
            };

            await context.Providers.AddRangeAsync(providers);
        }

        private static async Task SeedModelsAsync(SynaxisDbContext context)
        {
            if (await context.Models.AnyAsync())
                return;

            // Get providers
            var openai = await context.Providers.FirstAsync(p => p.Key == "openai");
            var anthropic = await context.Providers.FirstAsync(p => p.Key == "anthropic");
            var google = await context.Providers.FirstAsync(p => p.Key == "google");
            var cohere = await context.Providers.FirstAsync(p => p.Key == "cohere");

            var models = new[]
            {
                // OpenAI Models
                new Model
                {
                    Id = Guid.NewGuid(),
                    ProviderId = openai.Id,
                    CanonicalId = "gpt-4o",
                    DisplayName = "GPT-4o",
                    Description = "Most advanced GPT-4 model with vision",
                    ContextWindowTokens = 128000,
                    MaxOutputTokens = 16384,
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsVision = true,
                    IsActive = true,
                    IsPublic = true
                },
                new Model
                {
                    Id = Guid.NewGuid(),
                    ProviderId = openai.Id,
                    CanonicalId = "gpt-4o-mini",
                    DisplayName = "GPT-4o Mini",
                    Description = "Smaller, faster, cheaper GPT-4o",
                    ContextWindowTokens = 128000,
                    MaxOutputTokens = 16384,
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsVision = true,
                    IsActive = true,
                    IsPublic = true
                },
                new Model
                {
                    Id = Guid.NewGuid(),
                    ProviderId = openai.Id,
                    CanonicalId = "gpt-3.5-turbo",
                    DisplayName = "GPT-3.5 Turbo",
                    Description = "Fast and cost-effective model",
                    ContextWindowTokens = 16385,
                    MaxOutputTokens = 4096,
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsVision = false,
                    IsActive = true,
                    IsPublic = true
                },

                // Anthropic Models
                new Model
                {
                    Id = Guid.NewGuid(),
                    ProviderId = anthropic.Id,
                    CanonicalId = "claude-3-5-sonnet-20241022",
                    DisplayName = "Claude 3.5 Sonnet",
                    Description = "Most intelligent Claude model",
                    ContextWindowTokens = 200000,
                    MaxOutputTokens = 8192,
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsVision = true,
                    IsActive = true,
                    IsPublic = true
                },
                new Model
                {
                    Id = Guid.NewGuid(),
                    ProviderId = anthropic.Id,
                    CanonicalId = "claude-3-5-haiku-20241022",
                    DisplayName = "Claude 3.5 Haiku",
                    Description = "Fastest and most compact Claude model",
                    ContextWindowTokens = 200000,
                    MaxOutputTokens = 8192,
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsVision = true,
                    IsActive = true,
                    IsPublic = true
                },

                // Google Models
                new Model
                {
                    Id = Guid.NewGuid(),
                    ProviderId = google.Id,
                    CanonicalId = "gemini-2.0-flash-exp",
                    DisplayName = "Gemini 2.0 Flash",
                    Description = "Latest experimental Gemini model",
                    ContextWindowTokens = 1000000,
                    MaxOutputTokens = 8192,
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsVision = true,
                    IsActive = true,
                    IsPublic = true
                },
                new Model
                {
                    Id = Guid.NewGuid(),
                    ProviderId = google.Id,
                    CanonicalId = "gemini-1.5-pro",
                    DisplayName = "Gemini 1.5 Pro",
                    Description = "Advanced reasoning with long context",
                    ContextWindowTokens = 2000000,
                    MaxOutputTokens = 8192,
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsVision = true,
                    IsActive = true,
                    IsPublic = true
                },

                // Cohere Models
                new Model
                {
                    Id = Guid.NewGuid(),
                    ProviderId = cohere.Id,
                    CanonicalId = "command-r-plus",
                    DisplayName = "Command R+",
                    Description = "Advanced retrieval-augmented generation",
                    ContextWindowTokens = 128000,
                    MaxOutputTokens = 4096,
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsVision = false,
                    IsActive = true,
                    IsPublic = true
                }
            };

            await context.Models.AddRangeAsync(models);
        }

        private static async Task SeedSystemRolesAsync(SynaxisDbContext context)
        {
            if (await context.Roles.AnyAsync())
                return;

            var systemRoles = new[]
            {
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "SystemAdmin",
                    NormalizedName = "SYSTEMADMIN",
                    IsSystemRole = true,
                    OrganizationId = null,
                    Description = "Global system administrator with full access"
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "OrganizationOwner",
                    NormalizedName = "ORGANIZATIONOWNER",
                    IsSystemRole = true,
                    OrganizationId = null,
                    Description = "Organization owner with full administrative rights"
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "OrganizationAdmin",
                    NormalizedName = "ORGANIZATIONADMIN",
                    IsSystemRole = true,
                    OrganizationId = null,
                    Description = "Organization administrator with management rights"
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Member",
                    NormalizedName = "MEMBER",
                    IsSystemRole = true,
                    OrganizationId = null,
                    Description = "Standard organization member"
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Guest",
                    NormalizedName = "GUEST",
                    IsSystemRole = true,
                    OrganizationId = null,
                    Description = "Guest user with limited access"
                }
            };

            await context.Roles.AddRangeAsync(systemRoles);
        }
    }
}
