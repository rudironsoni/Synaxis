# Summary

|||
|:---|:---|
| Generated on: | 1/30/2026 - 3:23:25 PM |
| Coverage date: | 1/30/2026 - 2:51:13 PM - 1/30/2026 - 3:19:10 PM |
| Parser: | MultiReport (6x Cobertura) |
| Assemblies: | 4 |
| Classes: | 175 |
| Files: | 151 |
| **Line coverage:** | 65.5% (7133 of 10876) |
| Covered lines: | 7133 |
| Uncovered lines: | 3743 |
| Coverable lines: | 10876 |
| Total lines: | 16002 |
| **Branch coverage:** | 38.5% (1032 of 2678) |
| Covered branches: | 1032 |
| Total branches: | 2678 |
| **Method coverage:** | [Feature is only available for sponsors](https://reportgenerator.io/pro) |

# Risk Hotspots

| **Assembly** | **Class** | **Method** | **Crap Score** | **Cyclomatic complexity** |
|:---|:---|:---|---:|---:|
| Synaxis.Common.Tests | Microsoft.AspNetCore.OpenApi.Generated | TransformAsync(...) | 8930 | 94 || Synaxis.InferenceGateway.WebApi | Microsoft.AspNetCore.OpenApi.Generated | TransformAsync(...) | 8930 | 94 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.CohereChatClient | GetStreamingResponseAsync() | 4692 | 68 || Synaxis.InferenceGateway.Application | Synaxis.InferenceGateway.Application.ChatClients.SmartRoutingChatClient | RecordFailureAsync() | 3195 | 80 || Synaxis.Common.Tests | Microsoft.AspNetCore.OpenApi.Generated | TransformAsync(...) | 1190 | 34 || Synaxis.InferenceGateway.WebApi | Microsoft.AspNetCore.OpenApi.Generated | TransformAsync(...) | 1190 | 34 || Synaxis.Common.Tests | Microsoft.AspNetCore.OpenApi.Generated | GetTypeDocId(...) | 812 | 28 || Synaxis.InferenceGateway.WebApi | Microsoft.AspNetCore.OpenApi.Generated | GetTypeDocId(...) | 812 | 28 || Synaxis.InferenceGateway.WebApi | Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI.LegacyCompletionsEndpoint | TryParsePrompt(...) | 787 | 38 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Extensions.InfrastructureExtensions | AddSynaxisInfrastructure(...) | 740 | 136 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.External.Google.GoogleChatClient | GetResponseAsync() | 600 | 24 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.GitHub.GhConfigWriter | WriteTokenAsync() | 600 | 24 || Synaxis.Common.Tests | Microsoft.AspNetCore.OpenApi.Generated | CreateDocumentationId(...) | 342 | 18 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.External.GitHub.CopilotSdkAdapter | GetStreamingResponseAsync() | 342 | 18 || Synaxis.InferenceGateway.WebApi | Microsoft.AspNetCore.OpenApi.Generated | CreateDocumentationId(...) | 342 | 18 || Synaxis.InferenceGateway.WebApi | Synaxis.InferenceGateway.WebApi.Agents.RoutingService | LogRoutingContext(...) | 342 | 18 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Jobs.ProviderDiscoveryJob | GetEffectiveBaseUrl(...) | 274 | 90 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.External.GitHub.CopilotSdkAdapter | GetResponseAsync() | 272 | 16 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Auth.AntigravityAuthManager | CompleteAuthFlowAsync() | 210 | 14 || Synaxis.InferenceGateway.WebApi | Synaxis.InferenceGateway.WebApi.Helpers.OpenAIRequestMapper | ToChatMessages(...) | 198 | 30 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Auth.AntigravityAuthManager | ExtractProjectId(...) | 156 | 12 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Identity.Core.IdentityManager | RefreshLoopAsync() | 156 | 12 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.Google.GoogleAuthStrategy | ExtractProjectId(...) | 156 | 12 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Security.AesGcmTokenVault | RotateKeyAsync() | 156 | 12 || Synaxis.Common.Tests | Microsoft.AspNetCore.OpenApi.Generated | CreateDocumentationId(...) | 110 | 10 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.AntigravityJsonContext | ExpandConverter(...) | 110 | 10 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Auth.AntigravityAuthManager | FetchProjectIdAsync() | 110 | 10 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.External.GitHub.CopilotSdkAdapter | EnsureStartedAsync() | 110 | 10 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.External.Google.GoogleChatClient | CreateRequest(...) | 110 | 10 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.GitHub.GitHubAuthStrategy | RefreshTokenAsync() | 110 | 10 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.Google.GoogleAuthStrategy | FetchProjectIdAsync() | 110 | 10 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.Google.GoogleAuthStrategy | RefreshTokenAsync() | 110 | 10 || Synaxis.InferenceGateway.WebApi | Microsoft.AspNetCore.OpenApi.Generated | CreateDocumentationId(...) | 110 | 10 || Synaxis.InferenceGateway.WebApi | Synaxis.InferenceGateway.WebApi.Helpers.OpenAIRequestMapper | MapStopSequences(...) | 86 | 12 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.ModelsDevClient | GetAllModelsAsync() | 79 | 26 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.AntigravityJsonContext | AntigravityResponseSerializeHandler(...) | 72 | 8 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Auth.AntigravityAuthManager | ExchangeCodeForTokenAsync() | 72 | 8 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.Google.GoogleAuthStrategy | CompleteFlowAsync() | 72 | 8 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.Google.GoogleAuthStrategy | ExchangeCodeForTokenAsync() | 72 | 8 || Synaxis.InferenceGateway.WebApi | Mediator.Internals.NotificationHandlerWrapper<T> | Handle(...) | 72 | 8 || Synaxis.InferenceGateway.WebApi | Mediator.Mediator | CreateStream(...) | 72 | 8 || Synaxis.InferenceGateway.WebApi | Mediator.Mediator | Send() | 72 | 8 || Synaxis.InferenceGateway.WebApi | Synaxis.InferenceGateway.WebApi.Helpers.OpenAIRequestMapper | MapTools(...) | 64 | 10 || Synaxis.InferenceGateway.WebApi | Synaxis.InferenceGateway.WebApi.Middleware.OpenAIErrorHandlerMiddleware | InvokeAsync() | 62 | 52 || Synaxis.InferenceGateway.WebApi | Synaxis.InferenceGateway.WebApi.Health.ProviderConnectivityHealthCheck | GetDefaultEndpoint(...) | 55 | 48 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Jobs.ModelsDevSyncJob | Execute() | 52 | 50 || Synaxis.InferenceGateway.WebApi | Synaxis.InferenceGateway.WebApi.Endpoints.Antigravity.AntigravityEndpoints | ResolveAuthCompletion(...) | 48 | 14 || Synaxis.InferenceGateway.Application | Synaxis.InferenceGateway.Application.Routing.ModelResolver | ResolveCandidates(...) | 44 | 44 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Jobs.ProviderDiscoveryJob | Execute() | 44 | 28 || Synaxis.InferenceGateway.WebApi | Synaxis.InferenceGateway.WebApi.Helpers.OpenAIRequestParser | ParseAsync() | 43 | 20 || Synaxis.Common.Tests | Microsoft.AspNetCore.OpenApi.Generated | NormalizeDocId(...) | 42 | 6 || Synaxis.Common.Tests | Microsoft.AspNetCore.OpenApi.Generated | UnwrapOpenApiParameter(...) | 42 | 6 || Synaxis.Common.Tests | Synaxis.Common.Tests.TestBase | CreateMockProviderRegistry(...) | 42 | 6 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.AntigravityJsonContext | CandidateSerializeHandler(...) | 42 | 6 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Auth.AntigravityAuthManager | DecodeState(...) | 42 | 6 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Auth.AntigravityAuthManager | FetchUserEmailAsync() | 42 | 6 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Auth.AntigravityAuthManager | InteractiveLoginAsync() | 42 | 6 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.External.DuckDuckGo.DuckDuckGoChatClient | ReturnSingleAsync() | 42 | 6 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.External.GitHub.CopilotSdkAdapter | GetService(...) | 42 | 6 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.External.GitHub.CopilotSdkAdapter | Dispose() | 42 | 6 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.External.GitHub.CopilotSdkAdapter | .ctor(...) | 42 | 6 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.External.Google.GoogleChatClient | .ctor(...) | 42 | 6 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.Google.GoogleAuthStrategy | DecodeState(...) | 42 | 6 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.Google.GoogleAuthStrategy | FetchUserEmailAsync() | 42 | 6 || Synaxis.InferenceGateway.WebApi | Mediator.Internals.CommandHandlerWrapper<T1, T2> | Handle(...) | 42 | 6 || Synaxis.InferenceGateway.WebApi | Mediator.Internals.QueryHandlerWrapper<T1, T2> | Handle(...) | 42 | 6 || Synaxis.InferenceGateway.WebApi | Mediator.Internals.StreamCommandHandlerWrapper<T1, T2> | Handle(...) | 42 | 6 || Synaxis.InferenceGateway.WebApi | Mediator.Internals.StreamCommandHandlerWrapper<T1, T2> | Handle() | 42 | 6 || Synaxis.InferenceGateway.WebApi | Mediator.Internals.StreamQueryHandlerWrapper<T1, T2> | Handle(...) | 42 | 6 || Synaxis.InferenceGateway.WebApi | Mediator.Internals.StreamQueryHandlerWrapper<T1, T2> | Handle() | 42 | 6 || Synaxis.InferenceGateway.WebApi | Mediator.Internals.StreamRequestHandlerWrapper<T1, T2> | Handle() | 42 | 6 || Synaxis.InferenceGateway.WebApi | Microsoft.AspNetCore.OpenApi.Generated | NormalizeDocId(...) | 42 | 6 || Synaxis.InferenceGateway.WebApi | Microsoft.AspNetCore.OpenApi.Generated | UnwrapOpenApiParameter(...) | 42 | 6 || Synaxis.InferenceGateway.WebApi | Synaxis.InferenceGateway.WebApi.Agents.RoutingService | HandleAsync() | 42 | 6 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.AntigravityJsonContext | System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver.GetTypeInfo(...) | 36 | 36 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.AntigravityChatClient | GetStreamingResponseAsync() | 28 | 28 || Synaxis.InferenceGateway.WebApi | Synaxis.InferenceGateway.WebApi.Agents.RoutingAgent | RunCoreAsync() | 29 | 28 || Synaxis.InferenceGateway.Application | Synaxis.InferenceGateway.Application.Routing.ModelResolver | ResolveAsync() | 26 | 26 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.AntigravityChatClient | BuildRequest(...) | 27 | 26 || Synaxis.InferenceGateway.WebApi | Synaxis.InferenceGateway.WebApi.Agents.RoutingAgent | RunCoreStreamingAsync() | 26 | 26 || Synaxis.InferenceGateway.WebApi | Synaxis.InferenceGateway.WebApi.Health.ProviderConnectivityHealthCheck | CheckHealthAsync() | 24 | 24 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Auth.AntigravityAuthManager | GetTokenAsync() | 25 | 20 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.External.DuckDuckGo.DuckDuckGoChatClient | GetResponseAsync() | 29 | 20 || Synaxis.InferenceGateway.Application | Synaxis.InferenceGateway.Application.ChatClients.SmartRoutingChatClient | GetStreamingResponseAsync() | 23 | 18 || Synaxis.InferenceGateway.Application | Synaxis.InferenceGateway.Application.ProviderRegistry | GetCandidates(...) | 18 | 18 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.Identity.Core.IdentityManager | GetToken() | 18 | 18 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.AntigravityChatClient | MapResponse(...) | 16 | 16 || Synaxis.InferenceGateway.Infrastructure | Synaxis.InferenceGateway.Infrastructure.External.AiHorde.AiHordeChatClient | GetResponseAsync() | 16 | 16 |
# Coverage

| **Name** | **Covered** | **Uncovered** | **Coverable** | **Total** | **Line coverage** | **Covered** | **Total** | **Branch coverage** |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| **Synaxis.Common.Tests** | **30** | **564** | **594** | **964** | **5%** | **0** | **228** | **0%** |
| AutoGeneratedProgram | 0 | 1 | 1 | 4 | 0% | 0 | 0 |  |
| Microsoft.AspNetCore.OpenApi.Generated | 0 | 374 | 374 | 592 | 0% | 0 | 206 | 0% |
| Synaxis.Common.Tests.Factories.TestDataFactory | 0 | 85 | 85 | 133 | 0% | 0 | 12 | 0% |
| Synaxis.Common.Tests.Infrastructure.InMemoryDbContext | 0 | 26 | 26 | 43 | 0% | 0 | 0 |  |
| Synaxis.Common.Tests.TestBase | 30 | 75 | 105 | 169 | 28.5% | 0 | 10 | 0% |
| System.Runtime.CompilerServices | 0 | 3 | 3 | 23 | 0% | 0 | 0 |  |
| **Synaxis.InferenceGateway.Application** | **671** | **145** | **816** | **2154** | **82.2%** | **197** | **332** | **59.3%** |
| Synaxis.InferenceGateway.Application.ChatClients.SmartRoutingChatClient | 151 | 83 | 234 | 358 | 64.5% | 58 | 164 | 35.3% |
| Synaxis.InferenceGateway.Application.ChatClients.UsageTrackingChatClient | 35 | 4 | 39 | 69 | 89.7% | 12 | 18 | 66.6% |
| Synaxis.InferenceGateway.Application.Configuration.AliasConfig | 1 | 0 | 1 | 49 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Configuration.AntigravitySettings | 2 | 0 | 2 | 7 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Configuration.CanonicalModelConfig | 8 | 0 | 8 | 49 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Configuration.ProviderConfig | 10 | 0 | 10 | 49 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Configuration.SynaxisConfiguration | 9 | 0 | 9 | 49 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.ApiKey | 7 | 0 | 7 | 13 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.AuditLog | 6 | 2 | 8 | 14 | 75% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.DeviationEntry | 8 | 1 | 9 | 15 | 88.8% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.GlobalModel | 16 | 0 | 16 | 34 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.ModelAlias | 4 | 1 | 5 | 11 | 80% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.ModelCombo | 4 | 1 | 5 | 11 | 80% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.ModelCost | 4 | 0 | 4 | 9 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.OAuthAccount | 8 | 1 | 9 | 15 | 88.8% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.Project | 7 | 1 | 8 | 14 | 87.5% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.ProviderAccount | 6 | 1 | 7 | 13 | 85.7% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.ProviderModel | 9 | 1 | 10 | 28 | 90% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.QuotaSnapshot | 5 | 0 | 5 | 10 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.RequestLog | 11 | 3 | 14 | 20 | 78.5% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.RoutingPolicy | 0 | 6 | 6 | 12 | 0% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.Tenant | 9 | 1 | 10 | 16 | 90% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.TenantModelLimit | 5 | 1 | 6 | 21 | 83.3% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.TokenUsage | 9 | 3 | 12 | 18 | 75% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ControlPlane.Entities.User | 8 | 1 | 9 | 15 | 88.8% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Extensions.ApplicationExtensions | 12 | 0 | 12 | 29 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.ProviderRegistry | 32 | 1 | 33 | 67 | 96.9% | 26 | 34 | 76.4% |
| Synaxis.InferenceGateway.Application.Routing.CanonicalModelId | 7 | 2 | 9 | 18 | 77.7% | 3 | 4 | 75% |
| Synaxis.InferenceGateway.Application.Routing.EnrichedCandidate | 4 | 0 | 4 | 11 | 100% | 4 | 4 | 100% |
| Synaxis.InferenceGateway.Application.Routing.ModelResolver | 158 | 3 | 161 | 243 | 98.1% | 73 | 78 | 93.5% |
| Synaxis.InferenceGateway.Application.Routing.RequiredCapabilities | 5 | 1 | 6 | 11 | 83.3% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Routing.ResolutionResult | 1 | 0 | 1 | 6 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Routing.SmartRouter | 40 | 0 | 40 | 68 | 100% | 10 | 10 | 100% |
| Synaxis.InferenceGateway.Application.Translation.CanonicalChunk | 0 | 1 | 1 | 5 | 0% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Translation.CanonicalRequest | 4 | 4 | 8 | 13 | 50% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Translation.CanonicalResponse | 1 | 0 | 1 | 5 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Translation.NoOpRequestTranslator | 1 | 1 | 2 | 24 | 50% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Translation.NoOpResponseTranslator | 1 | 1 | 2 | 24 | 50% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Translation.NoOpStreamingTranslator | 1 | 1 | 2 | 24 | 50% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Translation.OpenAIFunction | 0 | 3 | 3 | 96 | 0% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Translation.OpenAIFunctionCall | 0 | 2 | 2 | 96 | 0% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Translation.OpenAIMessage | 4 | 1 | 5 | 96 | 80% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Translation.OpenAIRequest | 10 | 0 | 10 | 96 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Translation.OpenAITool | 0 | 2 | 2 | 96 | 0% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Translation.OpenAIToolCall | 0 | 3 | 3 | 96 | 0% | 0 | 0 |  |
| Synaxis.InferenceGateway.Application.Translation.OpenAIToolNormalizer | 8 | 8 | 16 | 46 | 50% | 2 | 8 | 25% |
| Synaxis.InferenceGateway.Application.Translation.TranslationPipeline | 40 | 0 | 40 | 65 | 100% | 9 | 12 | 75% |
| **Synaxis.InferenceGateway.Infrastructure** | **5412** | **1903** | **7315** | **14190** | **73.9%** | **606** | **1366** | **44.3%** |
| Synaxis.InferenceGateway.Infrastructure.AntigravityChatClient | 138 | 14 | 152 | 305 | 90.7% | 49 | 78 | 62.8% |
| Synaxis.InferenceGateway.Infrastructure.AntigravityJsonContext | 879 | 245 | 1124 | 1887 | 78.2% | 111 | 198 | 56% |
| Synaxis.InferenceGateway.Infrastructure.AntigravityRequest | 3 | 0 | 3 | 305 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.AntigravityResponse | 3 | 0 | 3 | 305 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.AntigravityResponseWrapper | 1 | 0 | 1 | 305 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Auth.AccountInfo | 1 | 0 | 1 | 13 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Auth.AntigravityAccount | 3 | 0 | 3 | 579 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Auth.AntigravityAuthManager | 87 | 298 | 385 | 579 | 22.5% | 18 | 102 | 17.6% |
| Synaxis.InferenceGateway.Infrastructure.Auth.FileTokenStore | 12 | 16 | 28 | 54 | 42.8% | 3 | 8 | 37.5% |
| Synaxis.InferenceGateway.Infrastructure.Candidate | 2 | 0 | 2 | 305 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.ChatClients.ChatClientFactory | 11 | 3 | 14 | 33 | 78.5% | 2 | 4 | 50% |
| Synaxis.InferenceGateway.Infrastructure.ChatClients.Strategies.CloudflareStrategy | 1 | 6 | 7 | 38 | 14.2% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.ChatClients.Strategies.OpenAiGenericStrategy | 11 | 0 | 11 | 35 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.CloudflareChatClient | 81 | 3 | 84 | 145 | 96.4% | 16 | 22 | 72.7% |
| Synaxis.InferenceGateway.Infrastructure.CohereChatClient | 48 | 88 | 136 | 233 | 35.2% | 7 | 82 | 8.5% |
| Synaxis.InferenceGateway.Infrastructure.Content | 2 | 0 | 2 | 305 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.ControlPlane.ControlPlaneDbContext | 146 | 13 | 159 | 189 | 91.8% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.ControlPlane.ControlPlaneExtensions | 19 | 0 | 19 | 32 | 100% | 4 | 4 | 100% |
| Synaxis.InferenceGateway.Infrastructure.ControlPlane.ControlPlaneOptions | 3 | 0 | 3 | 8 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.ControlPlane.ControlPlaneStore | 10 | 10 | 20 | 37 | 50% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.ControlPlane.DeviationRegistry | 26 | 2 | 28 | 49 | 92.8% | 3 | 4 | 75% |
| Synaxis.InferenceGateway.Infrastructure.ControlPlane.Migrations.AddDynamicModelRegistry | 923 | 8 | 931 | 1016 | 99.1% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.ControlPlane.Migrations.AddModelCosts | 1112 | 34 | 1146 | 1266 | 97% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.ControlPlane.Migrations.ControlPlaneDbContextModelSnapshot | 843 | 0 | 843 | 901 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Extensions.BespokeExtensions | 0 | 16 | 16 | 29 | 0% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Extensions.CustomProviderExtensions | 0 | 58 | 58 | 88 | 0% | 0 | 4 | 0% |
| Synaxis.InferenceGateway.Infrastructure.Extensions.GeminiExtensions | 0 | 5 | 5 | 16 | 0% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Extensions.HuggingFaceExtensions | 7 | 0 | 7 | 17 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Extensions.InfrastructureExtensions | 167 | 64 | 231 | 342 | 72.2% | 92 | 138 | 66.6% |
| Synaxis.InferenceGateway.Infrastructure.Extensions.NvidiaExtensions | 7 | 0 | 7 | 17 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Extensions.OpenAiCompatibleExtensions | 5 | 4 | 9 | 28 | 55.5% | 0 | 2 | 0% |
| Synaxis.InferenceGateway.Infrastructure.Extensions.OpenRouterExtensions | 11 | 0 | 11 | 22 | 100% | 2 | 4 | 50% |
| Synaxis.InferenceGateway.Infrastructure.External.AiHorde.AiHordeChatClient | 74 | 7 | 81 | 129 | 91.3% | 20 | 32 | 62.5% |
| Synaxis.InferenceGateway.Infrastructure.External.DuckDuckGo.DuckDuckGoChatClient | 54 | 27 | 81 | 138 | 66.6% | 14 | 32 | 43.7% |
| Synaxis.InferenceGateway.Infrastructure.External.GitHub.CopilotClientAdapter | 0 | 8 | 8 | 39 | 0% | 0 | 2 | 0% |
| Synaxis.InferenceGateway.Infrastructure.External.GitHub.CopilotSdkAdapter | 0 | 127 | 127 | 206 | 0% | 0 | 60 | 0% |
| Synaxis.InferenceGateway.Infrastructure.External.GitHub.CopilotSdkClient | 13 | 4 | 17 | 62 | 76.4% | 1 | 4 | 25% |
| Synaxis.InferenceGateway.Infrastructure.External.GitHub.CopilotSessionAdapter | 0 | 4 | 4 | 39 | 0% | 0 | 2 | 0% |
| Synaxis.InferenceGateway.Infrastructure.External.GitHub.GitHubCopilotChatClient | 84 | 18 | 102 | 147 | 82.3% | 29 | 62 | 46.7% |
| Synaxis.InferenceGateway.Infrastructure.External.Google.GoogleChatClient | 0 | 82 | 82 | 142 | 0% | 0 | 40 | 0% |
| Synaxis.InferenceGateway.Infrastructure.External.KiloCode.KiloCodeChatClient | 11 | 0 | 11 | 26 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.Dto.ModelDto | 17 | 0 | 17 | 41 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.Dto.ProviderDto | 1 | 0 | 1 | 8 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.ModelsDevClient | 29 | 18 | 47 | 85 | 61.7% | 12 | 28 | 42.8% |
| Synaxis.InferenceGateway.Infrastructure.External.OpenAi.Dto.OpenAiModelDto | 4 | 0 | 4 | 19 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.External.OpenAi.Dto.OpenAiModelsResponse | 1 | 0 | 1 | 11 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.External.OpenAi.OpenAiModelDiscoveryClient | 23 | 4 | 27 | 58 | 85.1% | 9 | 12 | 75% |
| Synaxis.InferenceGateway.Infrastructure.GenerationConfig | 5 | 0 | 5 | 305 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.GenericOpenAiChatClient | 28 | 20 | 48 | 87 | 58.3% | 7 | 12 | 58.3% |
| Synaxis.InferenceGateway.Infrastructure.Identity.Core.AuthResult | 2 | 3 | 5 | 30 | 40% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Identity.Core.EncryptedFileTokenStore | 24 | 9 | 33 | 62 | 72.7% | 5 | 10 | 50% |
| Synaxis.InferenceGateway.Infrastructure.Identity.Core.IdentityAccount | 7 | 0 | 7 | 16 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Identity.Core.IdentityManager | 122 | 62 | 184 | 253 | 66.3% | 37 | 62 | 59.6% |
| Synaxis.InferenceGateway.Infrastructure.Identity.Core.TokenResponse | 3 | 0 | 3 | 30 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Identity.IdentityTokenProvider | 0 | 8 | 8 | 23 | 0% | 0 | 2 | 0% |
| Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.GitHub.DeviceFlowService | 5 | 92 | 97 | 122 | 5.1% | 2 | 8 | 25% |
| Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.GitHub.GhConfigWriter | 0 | 46 | 46 | 77 | 0% | 0 | 24 | 0% |
| Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.GitHub.GitHubAuthStrategy | 6 | 85 | 91 | 144 | 6.5% | 3 | 24 | 12.5% |
| Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.Google.AntigravityAuthAdapter | 0 | 18 | 18 | 43 | 0% | 0 | 10 | 0% |
| Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.Google.GoogleAuthStrategy | 6 | 224 | 230 | 334 | 2.6% | 3 | 66 | 4.5% |
| Synaxis.InferenceGateway.Infrastructure.Jobs.ModelsDevSyncJob | 50 | 5 | 55 | 93 | 90.9% | 36 | 54 | 66.6% |
| Synaxis.InferenceGateway.Infrastructure.Jobs.ProviderDiscoveryJob | 107 | 39 | 146 | 221 | 73.2% | 95 | 118 | 80.5% |
| Synaxis.InferenceGateway.Infrastructure.Part | 1 | 0 | 1 | 305 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.PollinationsChatClient | 57 | 12 | 69 | 113 | 82.6% | 12 | 16 | 75% |
| Synaxis.InferenceGateway.Infrastructure.RequestPayload | 3 | 0 | 3 | 305 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Routing.CostService | 12 | 0 | 12 | 28 | 100% | 2 | 2 | 100% |
| Synaxis.InferenceGateway.Infrastructure.Routing.RedisHealthStore | 14 | 14 | 28 | 55 | 50% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Routing.RedisQuotaTracker | 17 | 4 | 21 | 46 | 80.9% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Security.AesGcmTokenVault | 0 | 74 | 74 | 124 | 0% | 0 | 22 | 0% |
| Synaxis.InferenceGateway.Infrastructure.Security.ApiKeyService | 17 | 0 | 17 | 35 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.Security.AuditService | 20 | 0 | 20 | 37 | 100% | 4 | 4 | 100% |
| Synaxis.InferenceGateway.Infrastructure.Security.JwtService | 32 | 0 | 32 | 59 | 100% | 8 | 8 | 100% |
| Synaxis.InferenceGateway.Infrastructure.SystemInstruction | 1 | 0 | 1 | 305 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.Infrastructure.ThinkingConfig | 0 | 2 | 2 | 305 | 0% | 0 | 0 |  |
| **Synaxis.InferenceGateway.WebApi** | **1020** | **1131** | **2151** | **8959** | **47.4%** | **229** | **752** | **30.4%** |
| Mediator.AssemblyReference | 0 | 7 | 7 | 37 | 0% | 0 | 0 |  |
| Mediator.Internals.CommandHandlerWrapper<T1, T2> | 0 | 39 | 39 | 338 | 0% | 0 | 6 | 0% |
| Mediator.Internals.ContainerMetadata | 25 | 0 | 25 | 677 | 100% | 0 | 0 |  |
| Mediator.Internals.NotificationHandlerWrapper<T> | 0 | 28 | 28 | 618 | 0% | 0 | 8 | 0% |
| Mediator.Internals.QueryHandlerWrapper<T1, T2> | 0 | 39 | 39 | 491 | 0% | 0 | 6 | 0% |
| Mediator.Internals.RequestHandlerWrapper<T1, T2> | 16 | 23 | 39 | 185 | 41% | 2 | 6 | 33.3% |
| Mediator.Internals.StreamCommandHandlerWrapper<T1, T2> | 0 | 40 | 40 | 415 | 0% | 0 | 12 | 0% |
| Mediator.Internals.StreamQueryHandlerWrapper<T1, T2> | 0 | 40 | 40 | 568 | 0% | 0 | 12 | 0% |
| Mediator.Internals.StreamRequestHandlerWrapper<T1, T2> | 16 | 24 | 40 | 262 | 40% | 2 | 12 | 16.6% |
| Mediator.Mediator | 38 | 163 | 201 | 1241 | 18.9% | 6 | 68 | 8.8% |
| Mediator.MediatorOptions | 8 | 0 | 8 | 54 | 100% | 0 | 0 |  |
| Mediator.MediatorOptionsAttribute | 0 | 4 | 4 | 32 | 0% | 0 | 0 |  |
| Microsoft.AspNetCore.OpenApi.Generated | 4 | 377 | 381 | 606 | 1% | 0 | 206 | 0% |
| Microsoft.Extensions.DependencyInjection.MediatorDependencyInjectionExtensions | 23 | 9 | 32 | 77 | 71.8% | 4 | 6 | 66.6% |
| Program | 154 | 4 | 158 | 246 | 97.4% | 5 | 6 | 83.3% |
| Synaxis.InferenceGateway.WebApi.Agents.RoutingAgent | 71 | 6 | 77 | 153 | 92.2% | 33 | 54 | 61.1% |
| Synaxis.InferenceGateway.WebApi.Agents.RoutingService | 7 | 45 | 52 | 103 | 13.4% | 0 | 26 | 0% |
| Synaxis.InferenceGateway.WebApi.Controllers.ApiKeysController | 0 | 40 | 40 | 83 | 0% | 0 | 8 | 0% |
| Synaxis.InferenceGateway.WebApi.Controllers.AuthController | 0 | 27 | 27 | 56 | 0% | 0 | 2 | 0% |
| Synaxis.InferenceGateway.WebApi.Controllers.CreateKeyRequest | 0 | 1 | 1 | 83 | 0% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.Controllers.DevLoginRequest | 0 | 1 | 1 | 56 | 0% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.DTOs.CompletionRequest | 6 | 4 | 10 | 29 | 60% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.DTOs.OpenAi.ChatCompletionChoice | 3 | 0 | 3 | 130 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.DTOs.OpenAi.ChatCompletionChunk | 5 | 0 | 5 | 130 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.DTOs.OpenAi.ChatCompletionChunkChoice | 3 | 0 | 3 | 130 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.DTOs.OpenAi.ChatCompletionChunkDelta | 3 | 0 | 3 | 130 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.DTOs.OpenAi.ChatCompletionMessageDto | 4 | 0 | 4 | 130 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.DTOs.OpenAi.ChatCompletionRequest | 0 | 7 | 7 | 130 | 0% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.DTOs.OpenAi.ChatCompletionResponse | 6 | 0 | 6 | 130 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.DTOs.OpenAi.ChatCompletionUsage | 3 | 0 | 3 | 130 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.Endpoints.Antigravity.AntigravityEndpoints | 52 | 32 | 84 | 124 | 61.9% | 5 | 16 | 31.2% |
| Synaxis.InferenceGateway.WebApi.Endpoints.Antigravity.CompleteAuthRequest | 4 | 0 | 4 | 124 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.Endpoints.Antigravity.StartAuthRequest | 1 | 0 | 1 | 124 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.Endpoints.Identity.IdentityEndpoints | 9 | 18 | 27 | 50 | 33.3% | 0 | 4 | 0% |
| Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI.LegacyCompletionsEndpoint | 79 | 56 | 135 | 191 | 58.5% | 7 | 38 | 18.4% |
| Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI.ModelsEndpoint | 33 | 0 | 33 | 49 | 100% | 6 | 8 | 75% |
| Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI.OpenAIEndpointsExtensions | 125 | 6 | 131 | 160 | 95.4% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.Features.Chat.Commands.ChatCommand | 1 | 0 | 1 | 15 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.Features.Chat.Commands.ChatStreamCommand | 1 | 0 | 1 | 15 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.Features.Chat.Handlers.ChatCompletionHandler | 10 | 0 | 10 | 31 | 100% | 0 | 0 |  |
| Synaxis.InferenceGateway.WebApi.Health.ConfigHealthCheck | 25 | 0 | 25 | 44 | 100% | 13 | 14 | 92.8% |
| Synaxis.InferenceGateway.WebApi.Health.ProviderConnectivityHealthCheck | 94 | 2 | 96 | 140 | 97.9% | 75 | 88 | 85.2% |
| Synaxis.InferenceGateway.WebApi.Helpers.OpenAIRequestMapper | 42 | 53 | 95 | 133 | 44.2% | 17 | 58 | 29.3% |
| Synaxis.InferenceGateway.WebApi.Helpers.OpenAIRequestParser | 30 | 19 | 49 | 90 | 61.2% | 10 | 20 | 50% |
| Synaxis.InferenceGateway.WebApi.Middleware.OpenAIErrorHandlerMiddleware | 88 | 14 | 102 | 151 | 86.2% | 38 | 62 | 61.2% |
| Synaxis.InferenceGateway.WebApi.Middleware.OpenAIMetadataMiddleware | 30 | 0 | 30 | 43 | 100% | 6 | 6 | 100% |
| Synaxis.InferenceGateway.WebApi.Middleware.RoutingContext | 1 | 0 | 1 | 2 | 100% | 0 | 0 |  |
| System.Runtime.CompilerServices | 0 | 3 | 3 | 23 | 0% | 0 | 0 |  |

