using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Synaxis.InferenceGateway.Application.Translation;

public class OpenAIRequest
{
    [JsonPropertyName("model")]
    [Required(ErrorMessage = "Model is required")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    [Required(ErrorMessage = "Messages are required")]
    [MinLength(1, ErrorMessage = "At least one message is required")]
    public List<OpenAIMessage> Messages { get; set; } = new();

    [JsonPropertyName("tools")]
    public List<OpenAITool>? Tools { get; set; }

    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; set; }

    [JsonPropertyName("response_format")]
    public object? ResponseFormat { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("temperature")]
    [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0 and 2")]
    public double? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    [Range(0.0, 1.0, ErrorMessage = "TopP must be between 0 and 1")]
    public double? TopP { get; set; }

    [JsonPropertyName("max_tokens")]
    [Range(1, int.MaxValue, ErrorMessage = "MaxTokens must be a positive integer")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("stop")]
    public object? Stop { get; set; }
}

public class OpenAIMessage
{
    [JsonPropertyName("role")]
    [Required(ErrorMessage = "Message role is required")]
    [RegularExpression("^(system|user|assistant|tool)$", ErrorMessage = "Role must be one of: system, user, assistant, tool")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public object? Content { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<OpenAIToolCall>? ToolCalls { get; set; }

    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; set; }
}

public class OpenAITool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OpenAIFunction? Function { get; set; }
}

public class OpenAIFunction
{
    [JsonPropertyName("name")]
    [Required(ErrorMessage = "Function name is required")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    public object? Parameters { get; set; }
}

public class OpenAIToolCall
{
    [JsonPropertyName("id")]
    [Required(ErrorMessage = "Tool call ID is required")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OpenAIFunctionCall? Function { get; set; }
}

public class OpenAIFunctionCall
{
    [JsonPropertyName("name")]
    [Required(ErrorMessage = "Function name is required")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    [Required(ErrorMessage = "Function arguments are required")]
    public string Arguments { get; set; } = string.Empty;
}