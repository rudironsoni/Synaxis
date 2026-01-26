using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Agents.AI.Hosting.OpenAI;
using Synaxis.InferenceGateway.WebApi.Agents;
using System;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI;

public static class OpenAIEndpointsExtensions
{
    public static IEndpointRouteBuilder MapOpenAIEndpoints(this IEndpointRouteBuilder endpoints, RoutingAgent agent)
    {
        var group = endpoints.MapGroup("/openai");
        var apiPrefix = typeof(OpenAIEndpointsExtensions).Assembly.GetName().Name!.Split('.')[0];
        MapOpenAIRoutes(group, agent, apiPrefix);
        return group;
    }



    private static void MapOpenAIRoutes(IEndpointRouteBuilder group, RoutingAgent agent, string apiPrefix)
    {
        // Chat Completions
        group.MapOpenAIChatCompletions(agent, path: "/v1/chat/completions")
            .WithTags("Chat")
            .AddOpenApiOperationTransformer((operation, context, ct) =>
            {
                operation.Summary = "Chat Completions";
                operation.Description = "OpenAI-compatible chat completions endpoint.";
                operation.OperationId = $"{apiPrefix}/CreateChatCompletion";
                return Task.CompletedTask;
            })
            .WithName("ChatCompletions");

        // Conversations
        group.MapOpenAIConversations()
            .WithTags("Conversations")
            .AddOpenApiOperationTransformer((operation, context, ct) =>
            {
                var path = context.Description.RelativePath;
                var method = context.Description.HttpMethod;

                if (path is null) return Task.CompletedTask;

                if (path.EndsWith("v1/conversations", StringComparison.OrdinalIgnoreCase))
                {
                    if (method == "GET")
                    {
                        operation.Summary = "List conversations for a specific agent (non-standard extension)";
                        operation.OperationId = $"{apiPrefix}/ListConversationsByAgent";
                    }
                    else if (method == "POST")
                    {
                        operation.Summary = "Create a new conversation";
                        operation.OperationId = $"{apiPrefix}/CreateConversation";
                    }
                }
                else if (path.EndsWith("v1/conversations/{conversationId}", StringComparison.OrdinalIgnoreCase))
                {
                    if (method == "GET")
                    {
                        operation.Summary = "Retrieve a conversation by ID";
                        operation.OperationId = $"{apiPrefix}/GetConversation";
                    }
                    else if (method == "POST")
                    {
                        operation.Summary = "Update a conversation's metadata or title";
                        operation.OperationId = $"{apiPrefix}/UpdateConversation";
                    }
                    else if (method == "DELETE")
                    {
                        operation.Summary = "Delete a conversation and all its messages";
                        operation.OperationId = $"{apiPrefix}/DeleteConversation";
                    }
                }
                else if (path.EndsWith("v1/conversations/{conversationId}/items", StringComparison.OrdinalIgnoreCase))
                {
                    if (method == "POST")
                    {
                        operation.Summary = "Add items to a conversation";
                        operation.OperationId = $"{apiPrefix}/CreateItems";
                    }
                    else if (method == "GET")
                    {
                        operation.Summary = "List items in a conversation";
                        operation.OperationId = $"{apiPrefix}/ListItems";
                    }
                }
                else if (path.EndsWith("v1/conversations/{conversationId}/items/{itemId}", StringComparison.OrdinalIgnoreCase))
                {
                    if (method == "GET")
                    {
                        operation.Summary = "Retrieve a specific item";
                        operation.OperationId = $"{apiPrefix}/GetItem";
                    }
                    else if (method == "DELETE")
                    {
                        operation.Summary = "Delete a specific item";
                        operation.OperationId = $"{apiPrefix}/DeleteItem";
                    }
                }

                return Task.CompletedTask;
            });

        // Responses
        group.MapOpenAIResponses(agent, responsesPath: "/v1/responses")
            .WithTags("Responses")
            .AddOpenApiOperationTransformer((operation, context, ct) =>
            {
                var path = context.Description.RelativePath;
                var method = context.Description.HttpMethod;

                if (path is null) return Task.CompletedTask;

                if (method == "POST" && path.EndsWith("v1/responses", StringComparison.OrdinalIgnoreCase))
                {
                    operation.Summary = "Creates a model response for the given input";
                    operation.OperationId = $"{apiPrefix}/CreateResponse";
                }
                else if (method == "GET" && path.EndsWith("v1/responses/{responseId}", StringComparison.OrdinalIgnoreCase))
                {
                    operation.Summary = "Retrieves a response by ID";
                    operation.OperationId = $"{apiPrefix}/GetResponse";
                }
                else if (method == "POST" && path.EndsWith("v1/responses/{responseId}/cancel", StringComparison.OrdinalIgnoreCase))
                {
                    operation.Summary = "Cancels an in-progress response";
                    operation.OperationId = $"{apiPrefix}/CancelResponse";
                }
                else if (method == "DELETE" && path.EndsWith("v1/responses/{responseId}", StringComparison.OrdinalIgnoreCase))
                {
                    operation.Summary = "Deletes a response";
                    operation.OperationId = $"{apiPrefix}/DeleteResponse";
                }
                else if (method == "GET" && path.EndsWith("v1/responses/{responseId}/input_items", StringComparison.OrdinalIgnoreCase))
                {
                    operation.Summary = "Lists the input items for a response";
                    operation.OperationId = $"{apiPrefix}/ListResponseInputItems";
                }

                return Task.CompletedTask;
            });

        // Legacy & Models
        group.MapLegacyCompletions();
        group.MapModels();

    }
}