// <copyright file="OpenAIRequestMapper.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Helpers
{
    using System.Collections.Generic;
    using System.Text.Json;
    using Microsoft.Extensions.AI;
    using Synaxis.InferenceGateway.Application.Routing;
    using Synaxis.InferenceGateway.Application.Translation;

    /// <summary>
    /// Helper class for mapping OpenAI requests to canonical format.
    /// </summary>
    public static class OpenAIRequestMapper
    {
        /// <summary>
        /// Converts an OpenAI request to a canonical request.
        /// </summary>
        /// <param name="openAIRequest">The OpenAI request.</param>
        /// <param name="messages">The chat messages.</param>
        /// <returns>The canonical request.</returns>
        public static CanonicalRequest ToCanonicalRequest(OpenAIRequest openAIRequest, IEnumerable<ChatMessage> messages)
        {
            var modelId = !string.IsNullOrWhiteSpace(openAIRequest.Model) ? openAIRequest.Model : "default";

            return new CanonicalRequest(
                EndpointKind.ChatCompletions,
                modelId,
                messages.ToList(),
                tools: MapTools(openAIRequest.Tools),
                toolChoice: openAIRequest.ToolChoice,
                responseFormat: openAIRequest.ResponseFormat,
                additionalOptions: new ChatOptions
                {
                    Temperature = (float?)openAIRequest.Temperature,
                    TopP = (float?)openAIRequest.TopP,
                    MaxOutputTokens = openAIRequest.MaxTokens,
                    StopSequences = MapStopSequences(openAIRequest.Stop),
                });
        }

        /// <summary>
        /// Converts an OpenAI request to chat messages.
        /// </summary>
        /// <param name="request">The OpenAI request.</param>
        /// <returns>The chat messages.</returns>
        public static IEnumerable<ChatMessage> ToChatMessages(OpenAIRequest request)
        {
            if (request.Messages == null)
            {
                return Enumerable.Empty<ChatMessage>();
            }

            var messages = new List<ChatMessage>();
            foreach (var msg in request.Messages)
            {
                var role = msg.Role switch
                {
                    "system" => ChatRole.System,
                    "user" => ChatRole.User,
                    "assistant" => ChatRole.Assistant,
                    "tool" => ChatRole.Tool,
                    _ => new ChatRole(msg.Role)
                };

                var content = msg.Content?.ToString() ?? "";
                // Note: This is a simplification. Real OpenAI messages can have array content (multimodal).
                // Assuming string for now as per current project usage patterns or we'd need deeper parsing.

                var chatMessage = new ChatMessage(role, content);
                if (!string.IsNullOrEmpty(msg.Name))
                {
                    chatMessage.AuthorName = msg.Name;
                }

                // Map tool calls if present
                if (msg.ToolCalls != null)
                {
                    foreach (var toolCall in msg.ToolCalls)
                    {
                        if (toolCall.Function != null)
                        {
                            chatMessage.Contents.Add(new FunctionCallContent(
                                toolCall.Id,
                                toolCall.Function.Name,
                                string.IsNullOrEmpty(toolCall.Function.Arguments) ? null : JsonSerializer.Deserialize<IDictionary<string, object?>>(toolCall.Function.Arguments)
                            ));
                        }
                    }
                }

                // Map tool response (tool_call_id)
                if (role == ChatRole.Tool && !string.IsNullOrEmpty(msg.ToolCallId))
                {
                    // In Microsoft.Extensions.AI, tool responses are often handled by matching ID.
                    // We might need to store ToolCallId in metadata or as a property if ChatMessage supports it.
                    // ChatMessage doesn't have ToolCallId directly, but FunctionResultContent does.
                    // If role is Tool, content is usually the result.
                    chatMessage.Contents.Clear();
                    chatMessage.Contents.Add(new FunctionResultContent(msg.ToolCallId, content));
                }

                messages.Add(chatMessage);
            }
            return messages;
        }

        private static IList<AITool>? MapTools(IList<OpenAITool>? tools)
        {
            if (tools == null)
            {
                return null;
            }

            var result = new List<AITool>();
            foreach (var tool in tools)
            {
                if (tool.Type == "function" && tool.Function != null)
                {
                    // We create a function definition using the AIFunctionFactory for metadata purposes.
                    // The actual execution delegate is a dummy since we are just routing.
                    var function = AIFunctionFactory.Create(
                        (string args) => Task.CompletedTask,
                        tool.Function.Name,
                        tool.Function.Description);

                    result.Add(function);
                }
            }
            return result.Count > 0 ? result : null;
        }

        private static IList<string>? MapStopSequences(object? stop)
        {
            if (stop is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    return new List<string> { element.GetString()! };
                }
                else if (element.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<string>();
                    foreach (var item in element.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            list.Add(item.GetString()!);
                        }
                    }
                    return list;
                }
            }

            return null;
        }
    }
}
