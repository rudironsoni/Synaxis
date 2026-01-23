namespace ContextSavvy.LlmProviders.Domain.ValueObjects;

/// <summary>
/// Supported LLM provider types.
/// </summary>
public enum ProviderType
{
    /// <summary>
    /// OpenAI's ChatGPT.
    /// </summary>
    ChatGpt,

    /// <summary>
    /// Anthropic's Claude.
    /// </summary>
    Claude,

    /// <summary>
    /// Google's AI Studio (Gemini).
    /// </summary>
    AIStudio,

    /// <summary>
    /// Groq high-speed inference.
    /// </summary>
    Groq,

    /// <summary>
    /// OpenRouter aggregator.
    /// </summary>
    OpenRouter
}
