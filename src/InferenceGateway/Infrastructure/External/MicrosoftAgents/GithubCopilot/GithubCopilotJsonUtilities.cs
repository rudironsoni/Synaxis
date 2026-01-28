// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Agents;

namespace Synaxis.InferenceGateway.Infrastructure.External.MicrosoftAgents.GithubCopilot;

internal static partial class GithubCopilotJsonUtilities
{
    public static JsonSerializerOptions DefaultOptions { get; } = CreateDefaultOptions();

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        JsonSerializerOptions options = new(JsonContext.Default.Options)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        options.TypeInfoResolverChain.Clear();
        // Fallback if AgentAbstractionsJsonUtilities is not accessible or we can use generic resolver
        options.TypeInfoResolverChain.Add(JsonContext.Default.Options.TypeInfoResolver!);

        options.MakeReadOnly();
        return options;
    }

    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
        UseStringEnumConverter = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString)]
    [JsonSerializable(typeof(GithubCopilotAgentSession.State))]
    [ExcludeFromCodeCoverage]
    private sealed partial class JsonContext : JsonSerializerContext;
}
