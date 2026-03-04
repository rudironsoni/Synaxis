// <copyright file="ChatController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Api.Controllers;

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Synaxis.Inference.Api.Models;
using Synaxis.Inference.Application.Commands;

/// <summary>
/// Controller for chat completion endpoints.
/// </summary>
[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IMediator mediator;
    private readonly ILogger<ChatController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator instance.</param>
    /// <param name="logger">The logger instance.</param>
    public ChatController(IMediator mediator, ILogger<ChatController> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    /// <summary>
    /// Creates a non-streaming chat completion.
    /// </summary>
    /// <param name="request">The chat completion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A chat completion response.</returns>
    [HttpPost("completions")]
    [ProducesResponseType(typeof(ChatCompletionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateChatCompletionAsync(
        [FromBody] ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Received chat completion request for model {Model}", request.Model);

        if (request.Stream)
        {
            return this.BadRequest(new { error = "Use streaming endpoint for stream=true requests" });
        }

        // Create and route inference request
        var requestId = Guid.NewGuid();
        var routeCommand = new RouteInferenceRequestCommand(requestId, request.Model, false);
        var routeResult = await this.mediator.Send(routeCommand, cancellationToken).ConfigureAwait(false);

        // Execute inference
        var executeCommand = new ExecuteInferenceCommand(requestId);
        var result = await this.mediator.Send(executeCommand, cancellationToken).ConfigureAwait(false);

        // Build response
        var response = new ChatCompletionResponse
        {
            Id = $"chatcmpl-{requestId:N}",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = request.Model,
            Choices = new List<ChatCompletionChoice>
            {
                new()
                {
                    Index = 0,
                    Message = new ChatCompletionMessage
                    {
                        Role = "assistant",
                        Content = result.ResponseContent,
                    },
                    FinishReason = "stop",
                },
            },
            Usage = new ChatCompletionUsage
            {
                PromptTokens = result.TokenUsage?.InputTokenCount ?? 0,
                CompletionTokens = result.TokenUsage?.OutputTokenCount ?? 0,
                TotalTokens = (result.TokenUsage?.InputTokenCount ?? 0) + (result.TokenUsage?.OutputTokenCount ?? 0),
            },
        };

        return this.Ok(response);
    }

    /// <summary>
    /// Creates a streaming chat completion.
    /// </summary>
    /// <param name="request">The chat completion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream of chat completion chunks.</returns>
    [HttpPost("completions/stream")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async IAsyncEnumerable<string> CreateChatCompletionStreamAsync(
        [FromBody] ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Received streaming chat completion request for model {Model}", request.Model);

        var requestId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var id = $"chatcmpl-{requestId:N}";

        // Create and route inference request
        var routeCommand = new RouteInferenceRequestCommand(requestId, request.Model, true);
        await this.mediator.Send(routeCommand, cancellationToken).ConfigureAwait(false);

        // Execute streaming inference
        var executeCommand = new ExecuteInferenceCommand(requestId);
        var result = await this.mediator.Send(executeCommand, cancellationToken).ConfigureAwait(false);

        // Stream the response
        var content = result.ResponseContent;
        var chunks = content.Split(' ');

        foreach (var chunk in chunks)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            var streamChunk = new ChatCompletionChunk
            {
                Id = id,
                Created = created,
                Model = request.Model,
                Choices = new List<ChatCompletionChunkChoice>
                {
                    new()
                    {
                        Index = 0,
                        Delta = new ChatCompletionChunkDelta
                        {
                            Content = chunk + " ",
                        },
                    },
                },
            };

            yield return FormatSseEvent(streamChunk);
        }

        // Send final chunk
        var finalChunk = new ChatCompletionChunk
        {
            Id = id,
            Created = created,
            Model = request.Model,
            Choices = new List<ChatCompletionChunkChoice>
            {
                new()
                {
                    Index = 0,
                    Delta = new ChatCompletionChunkDelta(),
                    FinishReason = "stop",
                },
            },
        };

        yield return FormatSseEvent(finalChunk);
        yield return "data: [DONE]\n\n";
    }

    /// <summary>
    /// Gets cost information for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cost information for the tenant.</returns>
    [HttpGet("costs/{tenantId}")]
    [ProducesResponseType(typeof(TenantCostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetTenantCostsAsync(
        [FromRoute, Required] string tenantId,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Getting costs for tenant {TenantId}", tenantId);

        var tenantContext = this.GetTenantContext();
        if (tenantContext is null)
        {
            return Task.FromResult<IActionResult>(this.NotFound(new { error = "Tenant not found" }));
        }

        // Return mock cost data - in production this would query the Application layer
        var response = new TenantCostResponse
        {
            TenantId = tenantId,
            TotalTokens = 1_250_000,
            TotalCost = 0.75m,
            Currency = "USD",
            Period = new CostPeriod
            {
                Start = DateTime.UtcNow.AddDays(-30),
                End = DateTime.UtcNow,
            },
            Breakdown = new CostBreakdown
            {
                InputTokens = 850_000,
                OutputTokens = 400_000,
                InputCost = 0.425m,
                OutputCost = 0.325m,
            },
        };

        return Task.FromResult<IActionResult>(this.Ok(response));
    }

    /// <summary>
    /// Gets cost information for a specific user in a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cost information for the user.</returns>
    [HttpGet("costs/{tenantId}/{userId}")]
    [ProducesResponseType(typeof(UserCostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetUserCostsAsync(
        [FromRoute, Required] string tenantId,
        [FromRoute, Required] string userId,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Getting costs for user {UserId} in tenant {TenantId}", userId, tenantId);

        var tenantContext = this.GetTenantContext();
        if (tenantContext is null)
        {
            return Task.FromResult<IActionResult>(this.NotFound(new { error = "Tenant not found" }));
        }

        // Return mock cost data - in production this would query the Application layer
        var response = new UserCostResponse
        {
            TenantId = tenantId,
            UserId = userId,
            TotalTokens = 125_000,
            TotalCost = 0.15m,
            Currency = "USD",
            Period = new CostPeriod
            {
                Start = DateTime.UtcNow.AddDays(-30),
                End = DateTime.UtcNow,
            },
            Breakdown = new CostBreakdown
            {
                InputTokens = 85_000,
                OutputTokens = 40_000,
                InputCost = 0.085m,
                OutputCost = 0.065m,
            },
        };

        return Task.FromResult<IActionResult>(this.Ok(response));
    }

    private static string FormatSseEvent(ChatCompletionChunk chunk)
    {
        var json = JsonSerializer.Serialize(chunk, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });
        return $"data: {json}\n\n";
    }

    private string? GetTenantContext()
    {
        // Extract tenant from custom header or JWT claims
        return this.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId)
            ? tenantId.ToString()
            : this.User?.FindFirst("tenant_id")?.Value;
    }
}

/// <summary>
/// Represents cost information for a tenant.
/// </summary>
public class TenantCostResponse
{
    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of tokens consumed.
    /// </summary>
    public long TotalTokens { get; set; }

    /// <summary>
    /// Gets or sets the total cost.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Gets or sets the currency code.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the billing period.
    /// </summary>
    public CostPeriod Period { get; set; } = new();

    /// <summary>
    /// Gets or sets the cost breakdown.
    /// </summary>
    public CostBreakdown Breakdown { get; set; } = new();
}

/// <summary>
/// Represents cost information for a user.
/// </summary>
public class UserCostResponse
{
    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of tokens consumed.
    /// </summary>
    public long TotalTokens { get; set; }

    /// <summary>
    /// Gets or sets the total cost.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Gets or sets the currency code.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the billing period.
    /// </summary>
    public CostPeriod Period { get; set; } = new();

    /// <summary>
    /// Gets or sets the cost breakdown.
    /// </summary>
    public CostBreakdown Breakdown { get; set; } = new();
}

/// <summary>
/// Represents a billing period.
/// </summary>
public class CostPeriod
{
    /// <summary>
    /// Gets or sets the period start date.
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// Gets or sets the period end date.
    /// </summary>
    public DateTime End { get; set; }
}

/// <summary>
/// Represents a cost breakdown.
/// </summary>
public class CostBreakdown
{
    /// <summary>
    /// Gets or sets the number of input tokens.
    /// </summary>
    public long InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of output tokens.
    /// </summary>
    public long OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the input token cost.
    /// </summary>
    public decimal InputCost { get; set; }

    /// <summary>
    /// Gets or sets the output token cost.
    /// </summary>
    public decimal OutputCost { get; set; }
}
