using Microsoft.Extensions.AI;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

namespace Synaxis.Common.Tests.Factories;

public static class TestDataFactory
{
    public static ChatMessage CreateUserMessage(string content = "Hello, world!")
        => new(ChatRole.User, content);

    public static ChatMessage CreateAssistantMessage(string content = "Mock response")
        => new(ChatRole.Assistant, content);

    public static ChatMessage CreateSystemMessage(string content = "You are a helpful assistant")
        => new(ChatRole.System, content);

    public static List<ChatMessage> CreateConversation(params string[] userMessages)
    {
        var messages = new List<ChatMessage>();
        foreach (var msg in userMessages)
        {
            messages.Add(CreateUserMessage(msg));
            messages.Add(CreateAssistantMessage($"Response to: {msg}"));
        }
        return messages;
    }

    public static ProviderConfig CreateProviderConfig(
        string key = "test-provider",
        string type = "openai",
        int tier = 0,
        string[]? models = null,
        bool enabled = true)
    {
        return new ProviderConfig
        {
            Key = key,
            Type = type,
            Tier = tier,
            Models = models?.ToList() ?? ["gpt-4", "gpt-3.5-turbo"],
            Enabled = enabled,
        };
    }

    public static CanonicalModelConfig CreateCanonicalModel(
        string id = "gpt-4",
        string provider = "OpenAI",
        bool streaming = true,
        bool tools = true,
        bool vision = false)
    {
        return new CanonicalModelConfig
        {
            Id = id,
            Provider = provider,
            Streaming = streaming,
            Tools = tools,
            Vision = vision,
        };
    }

    public static ApiKey CreateApiKey(
        Guid? id = null,
        string name = "Test Key",
        ApiKeyStatus status = ApiKeyStatus.Active)
    {
        return new ApiKey
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public static User CreateUser(
        Guid? id = null,
        string email = "test@example.com",
        UserRole role = UserRole.Owner)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            Role = role,
            AuthProvider = "dev",
            ProviderUserId = email,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public static Project CreateProject(
        Guid? id = null,
        string name = "Test Project")
    {
        return new Project
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public static SynaxisConfiguration CreateConfiguration(
        string jwtSecret = "test-secret-key-min-32-characters-long",
        int defaultTier = 0)
    {
        return new SynaxisConfiguration
        {
            JwtSecret = jwtSecret,
            Providers = new Dictionary<string, ProviderConfig>(StringComparer.Ordinal)
            {
                ["groq"] = CreateProviderConfig("groq", "groq", 0, ["llama-3.1-70b-versatile"]),
                ["openai"] = CreateProviderConfig("openai", "openai", 1, ["gpt-4", "gpt-3.5-turbo"]),
                ["deepseek"] = CreateProviderConfig("deepseek", "openai", 1, ["deepseek-chat"], enabled: true),
            },
            CanonicalModels =
            [
                CreateCanonicalModel("gpt-4", "OpenAI"),
                CreateCanonicalModel("llama-3.1-70b-versatile", "Groq"),
                CreateCanonicalModel("deepseek-chat", "DeepSeek")
            ],
            Aliases = new Dictionary<string, AliasConfig>(StringComparer.Ordinal)
            {
                ["default"] = new AliasConfig
                {
                    Candidates = ["deepseek-chat"],
                },
            },
        };
    }
}
