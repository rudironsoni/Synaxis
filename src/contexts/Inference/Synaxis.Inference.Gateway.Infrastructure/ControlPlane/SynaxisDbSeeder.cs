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
    /// - Platform providers (OpenAI, Anthropic, Google, etc.).
    /// - Platform models for each provider.
    /// - System roles (SystemAdmin, OrganizationOwner, OrganizationAdmin, Member, Guest).
    /// </summary>
    public static class SynaxisDbSeeder
    {
        /// <summary>
        /// Seeds initial data into the database asynchronously.
        /// </summary>
        /// <param name="context">The database context to seed data into.</param>
        /// <returns>A task representing the asynchronous seeding operation.</returns>
        public static async Task SeedAsync(SynaxisDbContext context)
        {
            await SeedProvidersAsync(context).ConfigureAwait(false);
            await SeedModelsAsync(context).ConfigureAwait(false);
            await SeedSystemRolesAsync(context).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        private static async Task SeedProvidersAsync(SynaxisDbContext context)
        {
            if (await context.Providers.AnyAsync().ConfigureAwait(false))
            {
                return;
            }

            var providers = CreateProvidersList();
            await context.Providers.AddRangeAsync(providers).ConfigureAwait(false);
        }

        private static Provider[] CreateProvidersList()
        {
            return new[]
            {
                CreateProvider("openai", "OpenAI", "OpenAI", "https://api.openai.com/v1", "OPENAI_API_KEY", true, true, true, false),
                CreateProvider("anthropic", "Anthropic", "Anthropic", "https://api.anthropic.com/v1", "ANTHROPIC_API_KEY", true, true, true, false),
                CreateProvider("google", "Google AI (Gemini)", "Google", "https://generativelanguage.googleapis.com/v1", "GOOGLE_API_KEY", true, true, true, true),
                CreateProvider("cohere", "Cohere", "Cohere", "https://api.cohere.ai/v1", "COHERE_API_KEY", true, true, false, true),
                CreateProvider("azure-openai", "Azure OpenAI", "Azure", null, "AZURE_OPENAI_API_KEY", true, true, true, false),
                CreateProvider("aws-bedrock", "AWS Bedrock", "AWS", null, "AWS_ACCESS_KEY_ID", true, true, true, false),
                CreateProvider("cloudflare", "Cloudflare Workers AI", "Cloudflare", "https://api.cloudflare.com/client/v4", "CLOUDFLARE_API_KEY", true, false, false, true),
            };
        }

        private static Provider CreateProvider(
            string key,
            string displayName,
            string providerType,
            string? baseEndpoint,
            string apiKeyEnvVar,
            bool supportsStreaming,
            bool supportsTools,
            bool supportsVision,
            bool isFreeTier)
        {
            return new Provider
            {
                Id = Guid.NewGuid(),
                Key = key,
                DisplayName = displayName,
                ProviderType = providerType,
                BaseEndpoint = baseEndpoint,
                DefaultApiKeyEnvironmentVariable = apiKeyEnvVar,
                SupportsStreaming = supportsStreaming,
                SupportsTools = supportsTools,
                SupportsVision = supportsVision,
                IsActive = true,
                IsPublic = true,
                IsFreeTier = isFreeTier,
            };
        }

        private static async Task SeedModelsAsync(SynaxisDbContext context)
        {
            if (await context.Models.AnyAsync().ConfigureAwait(false))
            {
                return;
            }

            var openai = await context.Providers.FirstAsync(p => p.Key == "openai").ConfigureAwait(false);
            var anthropic = await context.Providers.FirstAsync(p => p.Key == "anthropic").ConfigureAwait(false);
            var google = await context.Providers.FirstAsync(p => p.Key == "google").ConfigureAwait(false);
            var cohere = await context.Providers.FirstAsync(p => p.Key == "cohere").ConfigureAwait(false);

            var models = CreateModelsList(openai.Id, anthropic.Id, google.Id, cohere.Id);
            await context.Models.AddRangeAsync(models).ConfigureAwait(false);
        }

        private static Model[] CreateModelsList(Guid openaiId, Guid anthropicId, Guid googleId, Guid cohereId)
        {
            return new[]
            {
                // OpenAI Models
                CreateModel(openaiId, "gpt-4o", "GPT-4o", "Most advanced GPT-4 model with vision", 128000, 16384, true, true, true),
                CreateModel(openaiId, "gpt-4o-mini", "GPT-4o Mini", "Smaller, faster, cheaper GPT-4o", 128000, 16384, true, true, true),
                CreateModel(openaiId, "gpt-3.5-turbo", "GPT-3.5 Turbo", "Fast and cost-effective model", 16385, 4096, true, true, false),

                // Anthropic Models
                CreateModel(anthropicId, "claude-3-5-sonnet-20241022", "Claude 3.5 Sonnet", "Most intelligent Claude model", 200000, 8192, true, true, true),
                CreateModel(anthropicId, "claude-3-5-haiku-20241022", "Claude 3.5 Haiku", "Fastest and most compact Claude model", 200000, 8192, true, true, true),

                // Google Models
                CreateModel(googleId, "gemini-2.0-flash-exp", "Gemini 2.0 Flash", "Latest experimental Gemini model", 1000000, 8192, true, true, true),
                CreateModel(googleId, "gemini-1.5-pro", "Gemini 1.5 Pro", "Advanced reasoning with long context", 2000000, 8192, true, true, true),

                // Cohere Models
                CreateModel(cohereId, "command-r-plus", "Command R+", "Advanced retrieval-augmented generation", 128000, 4096, true, true, false),
            };
        }

        private static Model CreateModel(
            Guid providerId,
            string canonicalId,
            string displayName,
            string description,
            int contextWindowTokens,
            int maxOutputTokens,
            bool supportsStreaming,
            bool supportsTools,
            bool supportsVision)
        {
            return new Model
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                CanonicalId = canonicalId,
                DisplayName = displayName,
                Description = description,
                ContextWindowTokens = contextWindowTokens,
                MaxOutputTokens = maxOutputTokens,
                SupportsStreaming = supportsStreaming,
                SupportsTools = supportsTools,
                SupportsVision = supportsVision,
                IsActive = true,
                IsPublic = true,
            };
        }

        private static async Task SeedSystemRolesAsync(SynaxisDbContext context)
        {
            if (await context.Roles.AnyAsync().ConfigureAwait(false))
            {
                return;
            }

            var systemRoles = new[]
            {
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "SystemAdmin",
                    NormalizedName = "SYSTEMADMIN",
                    IsSystemRole = true,
                    OrganizationId = null,
                    Description = "Global system administrator with full access",
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "OrganizationOwner",
                    NormalizedName = "ORGANIZATIONOWNER",
                    IsSystemRole = true,
                    OrganizationId = null,
                    Description = "Organization owner with full administrative rights",
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "OrganizationAdmin",
                    NormalizedName = "ORGANIZATIONADMIN",
                    IsSystemRole = true,
                    OrganizationId = null,
                    Description = "Organization administrator with management rights",
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Member",
                    NormalizedName = "MEMBER",
                    IsSystemRole = true,
                    OrganizationId = null,
                    Description = "Standard organization member",
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Guest",
                    NormalizedName = "GUEST",
                    IsSystemRole = true,
                    OrganizationId = null,
                    Description = "Guest user with limited access",
                },
            };

            await context.Roles.AddRangeAsync(systemRoles).ConfigureAwait(false);
        }
    }
}
