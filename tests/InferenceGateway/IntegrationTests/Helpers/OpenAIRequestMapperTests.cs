// <copyright file="OpenAIRequestMapperTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text.Json;
using Microsoft.Extensions.AI;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.Application.Translation;
using Synaxis.InferenceGateway.WebApi.Helpers;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests.Helpers
{
    public class OpenAIRequestMapperTests
    {
        [Fact]
        public void ToCanonicalRequest_WithValidRequest_ReturnsCanonicalRequest()
        {
            var openAIRequest = new OpenAIRequest
            {
                Model = "gpt-4",
                Messages = new List<OpenAIMessage>(),
                Temperature = 0.7,
                TopP = 0.9,
                MaxTokens = 100,
            };
            var messages = new List<ChatMessage> { new (ChatRole.User, "Hello") };

            var result = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);

            Assert.NotNull(result);
            Assert.Equal(EndpointKind.ChatCompletions, result.Endpoint);
            Assert.Equal("gpt-4", result.Model);
            Assert.Single(result.Messages);
            Assert.NotNull(result.AdditionalOptions);
            Assert.Equal(0.7f, result.AdditionalOptions!.Temperature);
            Assert.Equal(0.9f, result.AdditionalOptions.TopP);
            Assert.Equal(100, result.AdditionalOptions.MaxOutputTokens);
        }

        [Fact]
        public void ToCanonicalRequest_WithEmptyModel_UsesDefault()
        {
            var openAIRequest = new OpenAIRequest
            {
                Model = string.Empty,
                Messages = new List<OpenAIMessage>(),
            };
            var messages = new List<ChatMessage>();

            var result = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);

            Assert.Equal("default", result.Model);
        }

        [Fact]
        public void ToCanonicalRequest_WithNullModel_UsesDefault()
        {
            var openAIRequest = new OpenAIRequest
            {
                Model = null!,
                Messages = new List<OpenAIMessage>(),
            };
            var messages = new List<ChatMessage>();

            var result = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);

            Assert.Equal("default", result.Model);
        }

        [Fact]
        public void ToCanonicalRequest_WithWhitespaceModel_UsesDefault()
        {
            var openAIRequest = new OpenAIRequest
            {
                Model = "   ",
                Messages = new List<OpenAIMessage>(),
            };
            var messages = new List<ChatMessage>();

            var result = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);

            Assert.Equal("default", result.Model);
        }

        [Fact]
        public void ToCanonicalRequest_WithNullMessages_ThrowsArgumentNullException()
        {
            var openAIRequest = new OpenAIRequest
            {
                Model = "gpt-4",
            };

            Assert.Throws<ArgumentNullException>(() =>
                OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, null!));
        }

        [Fact]
        public void ToCanonicalRequest_WithTools_MapsTools()
        {
            var openAIRequest = new OpenAIRequest
            {
                Model = "gpt-4",
                Messages = new List<OpenAIMessage>(),
                Tools = new List<OpenAITool>
            {
                new ()
                {
                    Type = "function",
                    Function = new OpenAIFunction
                    {
                        Name = "get_weather",
                        Description = "Get weather information"
                    }
                },
            },
            };
            var messages = new List<ChatMessage>();

            var result = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);

            Assert.NotNull(result.Tools);
            Assert.Single(result.Tools);
        }

        [Fact]
        public void ToCanonicalRequest_WithToolChoice_MapsToolChoice()
        {
            var toolChoice = new { type = "function" };
            var openAIRequest = new OpenAIRequest
            {
                Model = "gpt-4",
                Messages = new List<OpenAIMessage>(),
                ToolChoice = toolChoice,
            };
            var messages = new List<ChatMessage>();

            var result = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);

            Assert.Equal(toolChoice, result.ToolChoice);
        }

        [Fact]
        public void ToCanonicalRequest_WithResponseFormat_MapsResponseFormat()
        {
            var responseFormat = new { type = "json_object" };
            var openAIRequest = new OpenAIRequest
            {
                Model = "gpt-4",
                Messages = new List<OpenAIMessage>(),
                ResponseFormat = responseFormat,
            };
            var messages = new List<ChatMessage>();

            var result = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);

            Assert.Equal(responseFormat, result.ResponseFormat);
        }

        [Fact]
        public void ToCanonicalRequest_WithNullOptions_SetsNullOptions()
        {
            var openAIRequest = new OpenAIRequest
            {
                Model = "gpt-4",
                Messages = new List<OpenAIMessage>(),
                Temperature = null,
                TopP = null,
                MaxTokens = null,
                Stop = null,
            };
            var messages = new List<ChatMessage>();

            var result = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);

            Assert.NotNull(result.AdditionalOptions);
            Assert.Null(result.AdditionalOptions!.Temperature);
            Assert.Null(result.AdditionalOptions.TopP);
            Assert.Null(result.AdditionalOptions.MaxOutputTokens);
            Assert.Null(result.AdditionalOptions.StopSequences);
        }

        [Fact]
        public void ToCanonicalRequest_WithStringStopSequence_MapsToList()
        {
            var stopJson = JsonSerializer.SerializeToElement("STOP");
            var openAIRequest = new OpenAIRequest
            {
                Model = "gpt-4",
                Messages = new List<OpenAIMessage>(),
                Stop = stopJson,
            };
            var messages = new List<ChatMessage>();

            var result = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);

            Assert.NotNull(result.AdditionalOptions!.StopSequences);
            Assert.Single(result.AdditionalOptions.StopSequences);
            Assert.Equal("STOP", result.AdditionalOptions.StopSequences[0]);
        }

        [Fact]
        public void ToCanonicalRequest_WithArrayStopSequences_MapsToList()
        {
            var stopJson = JsonSerializer.SerializeToElement(new[] { "STOP", "END" });
            var openAIRequest = new OpenAIRequest
            {
                Model = "gpt-4",
                Messages = new List<OpenAIMessage>(),
                Stop = stopJson,
            };
            var messages = new List<ChatMessage>();

            var result = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);

            Assert.NotNull(result.AdditionalOptions!.StopSequences);
            Assert.Equal(2, result.AdditionalOptions.StopSequences.Count);
            Assert.Equal("STOP", result.AdditionalOptions.StopSequences[0]);
            Assert.Equal("END", result.AdditionalOptions.StopSequences[1]);
        }

        [Fact]
        public void ToChatMessages_WithNullMessages_ReturnsEmpty()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = null!,
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest);

            Assert.Empty(result);
        }

        [Fact]
        public void ToChatMessages_WithEmptyMessages_ReturnsEmpty()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>(),
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest);

            Assert.Empty(result);
        }

        [Fact]
        public void ToChatMessages_WithSystemRole_MapsToSystemRole()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new () { Role = "system", Content = "You are helpful" },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Single(result);
            Assert.Equal(ChatRole.System, result[0].Role);
            Assert.Equal("You are helpful", result[0].Text);
        }

        [Fact]
        public void ToChatMessages_WithUserRole_MapsToUserRole()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new () { Role = "user", Content = "Hello" },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Single(result);
            Assert.Equal(ChatRole.User, result[0].Role);
            Assert.Equal("Hello", result[0].Text);
        }

        [Fact]
        public void ToChatMessages_WithAssistantRole_MapsToAssistantRole()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new () { Role = "assistant", Content = "Hi there" },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Single(result);
            Assert.Equal(ChatRole.Assistant, result[0].Role);
            Assert.Equal("Hi there", result[0].Text);
        }

        [Fact]
        public void ToChatMessages_WithToolRole_MapsToToolRole()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new () { Role = "tool", Content = "Tool result", ToolCallId = "call_123" },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Single(result);
            Assert.Equal(ChatRole.Tool, result[0].Role);
        }

        [Fact]
        public void ToChatMessages_WithUnknownRole_CreatesCustomRole()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new () { Role = "custom_role", Content = "Custom content" },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Single(result);
            Assert.Equal("custom_role", result[0].Role.Value);
            Assert.Equal("Custom content", result[0].Text);
        }

        [Fact]
        public void ToChatMessages_WithName_MapsAuthorName()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new () { Role = "user", Content = "Hello", Name = "John" },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Single(result);
            Assert.Equal("John", result[0].AuthorName);
        }

        [Fact]
        public void ToChatMessages_WithNullContent_ReturnsEmptyString()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new () { Role = "user", Content = null },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Single(result);
            Assert.Equal(string.Empty, result[0].Text);
        }

        [Fact]
        public void ToChatMessages_WithToolCalls_MapsFunctionCalls()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new ()
                {
                    Role = "assistant",
                    Content = "Calling function",
                    ToolCalls = new List<OpenAIToolCall>
                    {
                        new ()
                        {
                            Id = "call_123",
                            Type = "function",
                            Function = new OpenAIFunctionCall
                            {
                                Name = "get_weather",
                                Arguments = "{\"location\": \"NYC\"}"
                            }
                        }
                    }
                },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Single(result);
            var contents = result[0].Contents.ToList();
            Assert.True(contents.Count > 0);
            var functionCall = contents.OfType<FunctionCallContent>().FirstOrDefault();
            Assert.NotNull(functionCall);
            Assert.Equal("call_123", functionCall.CallId);
            Assert.Equal("get_weather", functionCall.Name);
        }

        [Fact]
        public void ToChatMessages_WithMultipleToolCalls_MapsAllFunctionCalls()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new ()
                {
                    Role = "assistant",
                    Content = "Calling multiple functions",
                    ToolCalls = new List<OpenAIToolCall>
                    {
                        new ()
                        {
                            Id = "call_1",
                            Type = "function",
                            Function = new OpenAIFunctionCall
                            {
                                Name = "func1",
                                Arguments = "{}"
                            }
                        },
                        new ()
                        {
                            Id = "call_2",
                            Type = "function",
                            Function = new OpenAIFunctionCall
                            {
                                Name = "func2",
                                Arguments = "{}"
                            }
                        }
                    }
                },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Single(result);
            var functionCalls = result[0].Contents.OfType<FunctionCallContent>().ToList();
            Assert.Equal(2, functionCalls.Count);
        }

        [Fact]
        public void ToChatMessages_WithToolCallEmptyArguments_MapsNullArguments()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new ()
                {
                    Role = "assistant",
                    Content = string.Empty,
                    ToolCalls = new List<OpenAIToolCall>
                    {
                        new ()
                        {
                            Id = "call_123",
                            Type = "function",
                            Function = new OpenAIFunctionCall
                            {
                                Name = "func",
                                Arguments = string.Empty
                            }
                        }
                    }
                },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Single(result);
            var functionCall = result[0].Contents.OfType<FunctionCallContent>().FirstOrDefault();
            Assert.NotNull(functionCall);
        }

        [Fact]
        public void ToChatMessages_WithToolRoleAndToolCallId_MapsFunctionResult()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new ()
                {
                    Role = "tool",
                    Content = "25 degrees",
                    ToolCallId = "call_123",
                },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Single(result);
            Assert.Equal(ChatRole.Tool, result[0].Role);
            var functionResult = result[0].Contents.OfType<FunctionResultContent>().FirstOrDefault();
            Assert.NotNull(functionResult);
            Assert.Equal("call_123", functionResult.CallId);
        }

        [Fact]
        public void ToChatMessages_WithToolRoleAndNullToolCallId_DoesNotAddFunctionResult()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new ()
                {
                    Role = "tool",
                    Content = "Result",
                    ToolCallId = null,
                },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Single(result);
            Assert.Equal(ChatRole.Tool, result[0].Role);
            var functionResult = result[0].Contents.OfType<FunctionResultContent>().FirstOrDefault();
            Assert.Null(functionResult);
        }

        [Fact]
        public void ToChatMessages_WithMixedRoles_MapsAllCorrectly()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new () { Role = "system", Content = "System prompt" },
                new () { Role = "user", Content = "User message" },
                new () { Role = "assistant", Content = "Assistant response" },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal(ChatRole.System, result[0].Role);
            Assert.Equal(ChatRole.User, result[1].Role);
            Assert.Equal(ChatRole.Assistant, result[2].Role);
        }

        [Fact]
        public void ToChatMessages_WithEmptyToolCallsList_HandlesGracefully()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new ()
                {
                    Role = "assistant",
                    Content = "No tools",
                    ToolCalls = new List<OpenAIToolCall>(),
                },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Single(result);
            Assert.Empty(result[0].Contents.OfType<FunctionCallContent>());
        }

        [Fact]
        public void ToChatMessages_WithToolCallNullFunction_SkipsFunction()
        {
            var openAIRequest = new OpenAIRequest
            {
                Messages = new List<OpenAIMessage>
            {
                new ()
                {
                    Role = "assistant",
                    Content = string.Empty,
                    ToolCalls = new List<OpenAIToolCall>
                    {
                        new ()
                        {
                            Id = "call_123",
                            Type = "function",
                            Function = null,
                        },
                    },
                },
            },
            };

            var result = OpenAIRequestMapper.ToChatMessages(openAIRequest).ToList();

            Assert.Single(result);
            Assert.Empty(result[0].Contents.OfType<FunctionCallContent>());
        }
    }
}
