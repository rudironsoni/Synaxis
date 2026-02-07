using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure
{
    /// <summary>
    /// Provides deterministic mock responses for different providers and models.
    /// This eliminates network dependencies and provides consistent test data.
    /// </summary>
    public class MockProviderResponses
    {
        // Mock responses for different providers/models
        private readonly Dictionary<string, ChatCompletionResponse> _chatResponses;
        private readonly Dictionary<string, LegacyCompletionResponse> _legacyResponses;
        private readonly List<ModelInfo> _availableModels;

        public MockProviderResponses()
        {
            _chatResponses = CreateChatResponses();
            _legacyResponses = CreateLegacyResponses();
            _availableModels = CreateAvailableModels();
        }

        public ChatCompletionResponse GetChatCompletionResponse(string model)
        {
            // Return specific model response if exists, otherwise return generic response
            if (_chatResponses.TryGetValue(model, out var response))
            {
                return new ChatCompletionResponse
                {
                    Id = $"chatcmpl-{GenerateId()}",
                    Object = response.Object,
                    Created = response.Created,
                    Model = model,
                    Choices = response.Choices,
                    Usage = response.Usage
                };
            }

            // Return generic mock response
            return CreateGenericChatResponse(model);
        }

        public LegacyCompletionResponse GetLegacyCompletionResponse(string model)
        {
            // Return specific model response if exists, otherwise return generic response
            if (_legacyResponses.TryGetValue(model, out var response))
            {
                return new LegacyCompletionResponse
                {
                    Id = $"cmpl-{GenerateId()}",
                    Object = response.Object,
                    Created = response.Created,
                    Model = model,
                    Choices = response.Choices,
                    Usage = response.Usage
                };
            }

            // Return generic mock response
            return CreateGenericLegacyResponse(model);
        }

        public object GetAvailableModels()
        {
            return new
            {
                @object = "list",
                data = _availableModels
            };
        }

        private static string GenerateId()
        {
            return Guid.NewGuid().ToString("N")[..8];
        }

        private static ChatCompletionResponse CreateGenericChatResponse(string model)
        {
            return new ChatCompletionResponse
            {
                Id = $"chatcmpl-{GenerateId()}",
                Object = "chat.completion",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = model,
                Choices = new List<ChatCompletionChoice>
                {
                    new ChatCompletionChoice
                    {
                        Index = 0,
                        Message = new ChatCompletionMessageDto
                        {
                            Role = "assistant",
                            Content = "Mock response for " + model
                        },
                        FinishReason = "stop"
                    }
                },
                Usage = new ChatCompletionUsage
                {
                    PromptTokens = 10,
                    CompletionTokens = 5,
                    TotalTokens = 15
                }
            };
        }

        private static LegacyCompletionResponse CreateGenericLegacyResponse(string model)
        {
            return new LegacyCompletionResponse
            {
                Id = $"cmpl-{GenerateId()}",
                Object = "text_completion",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = model,
                Choices = new List<LegacyCompletionChoice>
                {
                    new LegacyCompletionChoice
                    {
                        Index = 0,
                        Text = "Mock text completion for " + model,
                        FinishReason = "stop"
                    }
                },
                Usage = new LegacyUsage
                {
                    PromptTokens = 8,
                    CompletionTokens = 6,
                    TotalTokens = 14
                }
            };
        }

        private Dictionary<string, ChatCompletionResponse> CreateChatResponses()
        {
            return new Dictionary<string, ChatCompletionResponse>
            {
                ["llama-3.1-70b-versatile"] = new ChatCompletionResponse
                {
                    Id = "chatcmpl-mock",
                    Object = "chat.completion",
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Model = "llama-3.1-70b-versatile",
                    Choices = new List<ChatCompletionChoice>
                    {
                        new ChatCompletionChoice
                        {
                            Index = 0,
                            Message = new ChatCompletionMessageDto
                            {
                                Role = "assistant",
                                Content = "OK"
                            },
                            FinishReason = "stop"
                        }
                    },
                    Usage = new ChatCompletionUsage
                    {
                        PromptTokens = 12,
                        CompletionTokens = 2,
                        TotalTokens = 14
                    }
                },
                ["deepseek-chat"] = new ChatCompletionResponse
                {
                    Id = "chatcmpl-mock",
                    Object = "chat.completion",
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Model = "deepseek-chat",
                    Choices = new List<ChatCompletionChoice>
                    {
                        new ChatCompletionChoice
                        {
                            Index = 0,
                            Message = new ChatCompletionMessageDto
                            {
                                Role = "assistant",
                                Content = "OK"
                            },
                            FinishReason = "stop"
                        }
                    },
                    Usage = new ChatCompletionUsage
                    {
                        PromptTokens = 10,
                        CompletionTokens = 2,
                        TotalTokens = 12
                    }
                },
                // Add more provider-specific responses as needed
                ["gpt-4o"] = new ChatCompletionResponse
                {
                    Id = "chatcmpl-mock",
                    Object = "chat.completion",
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Model = "gpt-4o",
                    Choices = new List<ChatCompletionChoice>
                    {
                        new ChatCompletionChoice
                        {
                            Index = 0,
                            Message = new ChatCompletionMessageDto
                            {
                                Role = "assistant",
                                Content = "OK"
                            },
                            FinishReason = "stop"
                        }
                    },
                    Usage = new ChatCompletionUsage
                    {
                        PromptTokens = 15,
                        CompletionTokens = 2,
                        TotalTokens = 17
                    }
                }
            };
        }

        private Dictionary<string, LegacyCompletionResponse> CreateLegacyResponses()
        {
            return new Dictionary<string, LegacyCompletionResponse>
            {
                ["llama-3.1-70b-versatile"] = new LegacyCompletionResponse
                {
                    Id = "cmpl-mock",
                    Object = "text_completion",
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Model = "llama-3.1-70b-versatile",
                    Choices = new List<LegacyCompletionChoice>
                    {
                        new LegacyCompletionChoice
                        {
                            Index = 0,
                            Text = "OK",
                            FinishReason = "stop"
                        }
                    },
                    Usage = new LegacyUsage
                    {
                        PromptTokens = 11,
                        CompletionTokens = 1,
                        TotalTokens = 12
                    }
                },
                ["deepseek-chat"] = new LegacyCompletionResponse
                {
                    Id = "cmpl-mock",
                    Object = "text_completion",
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Model = "deepseek-chat",
                    Choices = new List<LegacyCompletionChoice>
                    {
                        new LegacyCompletionChoice
                        {
                            Index = 0,
                            Text = "OK",
                            FinishReason = "stop"
                        }
                    },
                    Usage = new LegacyUsage
                    {
                        PromptTokens = 9,
                        CompletionTokens = 1,
                        TotalTokens = 10
                    }
                }
            };
        }

        private List<ModelInfo> CreateAvailableModels()
        {
            return new List<ModelInfo>
            {
                new ModelInfo
                {
                    Id = "llama-3.1-70b-versatile",
                    Object = "model",
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    OwnedBy = "Groq"
                },
                new ModelInfo
                {
                    Id = "deepseek-chat",
                    Object = "model",
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    OwnedBy = "DeepSeek"
                },
                new ModelInfo
                {
                    Id = "gpt-4o",
                    Object = "model",
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    OwnedBy = "OpenAI"
                },
                new ModelInfo
                {
                    Id = "claude-3.5-sonnet",
                    Object = "model",
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    OwnedBy = "Anthropic"
                }
            };
        }
    }

    // DTOs for mock responses
    public class ChatCompletionResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public long Created { get; set; }
        public string Model { get; set; } = string.Empty;
        public List<ChatCompletionChoice> Choices { get; set; } = new ();
        public ChatCompletionUsage? Usage { get; set; }
    }

    public class ChatCompletionChoice
    {
        public int Index { get; set; }
        public ChatCompletionMessageDto Message { get; set; } = new ();
        public string FinishReason { get; set; } = string.Empty;
    }

    public class ChatCompletionMessageDto
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class ChatCompletionUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    public class LegacyCompletionResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public long Created { get; set; }
        public string Model { get; set; } = string.Empty;
        public List<LegacyCompletionChoice> Choices { get; set; } = new ();
        public LegacyUsage? Usage { get; set; }
    }

    public class LegacyCompletionChoice
    {
        public int Index { get; set; }
        public string Text { get; set; } = string.Empty;
        public string FinishReason { get; set; } = string.Empty;
    }

    public class LegacyUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    public class ModelInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public long Created { get; set; }
        public string OwnedBy { get; set; } = string.Empty;
    }
}
