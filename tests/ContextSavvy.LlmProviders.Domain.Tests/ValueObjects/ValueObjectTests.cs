using ContextSavvy.LlmProviders.Domain.ValueObjects;
using FluentAssertions;

namespace ContextSavvy.LlmProviders.Domain.Tests.ValueObjects;

public class ValueObjectTests
{
    [Fact]
    public void SessionStatus_ShouldHaveExpectedValues()
    {
        // Act & Assert
        Enum.GetNames<SessionStatus>().Should().Contain(new[]
        {
            "Initializing",
            "Ready",
            "Busy",
            "Error",
            "Disposed"
        });
    }

    [Fact]
    public void ProviderType_ShouldHaveExpectedValues()
    {
        // Act & Assert
        Enum.GetNames<ProviderType>().Should().Contain(new[]
        {
            "ChatGpt",
            "Claude",
            "AIStudio",
            "Groq",
            "OpenRouter"
        });
    }

    [Fact]
    public void ProviderTier_ShouldHaveExpectedValues()
    {
        // Act & Assert
        Enum.GetNames<ProviderTier>().Should().Contain(new[]
        {
            "Tier1_FreeFast",
            "Tier2_Standard",
            "Tier3_Ghost",
            "Tier4_Experimental"
        });
    }
}
