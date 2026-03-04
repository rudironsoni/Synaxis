// <copyright file="ModelsController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Api.Controllers;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Synaxis.Inference.Api.Models;

/// <summary>
/// Controller for model management endpoints.
/// </summary>
[ApiController]
[Route("api/models")]
public class ModelsController : ControllerBase
{
    private readonly ILogger<ModelsController> logger;
    private readonly IOptions<ModelConfiguration> configuration;
    private static readonly Dictionary<string, RegisteredModel> RegisteredModels = new();
    private static readonly Dictionary<string, List<ProviderModel>> ProviderModels = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelsController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configuration">The model configuration.</param>
    public ModelsController(ILogger<ModelsController> logger, IOptions<ModelConfiguration> configuration)
    {
        this.logger = logger;
        this.configuration = configuration;
    }

    /// <summary>
    /// Lists available models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available models.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ModelListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> ListModelsAsync(CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Received list models request");

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var configModels = this.configuration.Value.AvailableModels.Select(m => new ModelInfo
        {
            Id = m.Id,
            Created = now,
            OwnedBy = m.Provider,
            Capabilities = new ModelCapabilities
            {
                Streaming = m.SupportsStreaming,
                FunctionCalling = m.SupportsFunctionCalling,
                Vision = m.SupportsVision,
                JsonMode = m.SupportsJsonMode,
            },
        }).ToList();

        var registeredModels = RegisteredModels.Values.Select(m => new ModelInfo
        {
            Id = m.Id,
            Created = m.CreatedAt,
            OwnedBy = m.Provider,
            Capabilities = new ModelCapabilities
            {
                Streaming = m.SupportsStreaming,
                FunctionCalling = m.SupportsFunctionCalling,
                Vision = m.SupportsVision,
                JsonMode = m.SupportsJsonMode,
            },
        });

        configModels.AddRange(registeredModels);

        var response = new ModelListResponse
        {
            Data = configModels,
        };

        return Task.FromResult<IActionResult>(this.Ok(response));
    }

    /// <summary>
    /// Retrieves a specific model.
    /// </summary>
    /// <param name="id">The model ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The model information.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ModelDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetModelAsync(
        [FromRoute, Required] string id,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Received get model request for {ModelId}", id);

        // Check configuration models
        var configModel = this.configuration.Value.AvailableModels.FirstOrDefault(m => m.Id == id);
        if (configModel is not null)
        {
            var response = new ModelDetailResponse
            {
                Id = configModel.Id,
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                OwnedBy = configModel.Provider,
                Capabilities = new ModelCapabilities
                {
                    Streaming = configModel.SupportsStreaming,
                    FunctionCalling = configModel.SupportsFunctionCalling,
                    Vision = configModel.SupportsVision,
                    JsonMode = configModel.SupportsJsonMode,
                },
                ContextWindow = 128000,
                Description = $"Model from {configModel.Provider}",
            };

            return Task.FromResult<IActionResult>(this.Ok(response));
        }

        // Check registered models
        if (RegisteredModels.TryGetValue(id, out var registeredModel))
        {
            var response = new ModelDetailResponse
            {
                Id = registeredModel.Id,
                Created = registeredModel.CreatedAt,
                OwnedBy = registeredModel.Provider,
                Capabilities = new ModelCapabilities
                {
                    Streaming = registeredModel.SupportsStreaming,
                    FunctionCalling = registeredModel.SupportsFunctionCalling,
                    Vision = registeredModel.SupportsVision,
                    JsonMode = registeredModel.SupportsJsonMode,
                },
                ContextWindow = registeredModel.ContextWindow,
                Description = registeredModel.Description,
                MaxTokens = registeredModel.MaxTokens,
            };

            return Task.FromResult<IActionResult>(this.Ok(response));
        }

        return Task.FromResult<IActionResult>(this.NotFound(new
        {
            error = new
            {
                message = $"The model '{id}' does not exist",
                type = "invalid_request_error",
                param = "model",
                code = "model_not_found",
            },
        }));
    }

    /// <summary>
    /// Registers a new model.
    /// </summary>
    /// <param name="request">The model registration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registered model.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ModelDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> RegisterModelAsync(
        [FromBody] RegisterModelRequest request,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Registering model {ModelId}", request.Id);

        if (RegisteredModels.ContainsKey(request.Id))
        {
            return Task.FromResult<IActionResult>(this.Conflict(new { error = "Model already exists" }));
        }

        if (this.configuration.Value.AvailableModels.Any(m => m.Id == request.Id))
        {
            return Task.FromResult<IActionResult>(this.Conflict(new { error = "Model already exists in configuration" }));
        }

        var model = new RegisteredModel
        {
            Id = request.Id,
            Provider = request.Provider,
            Description = request.Description,
            ContextWindow = request.ContextWindow ?? 128000,
            MaxTokens = request.MaxTokens,
            SupportsStreaming = request.SupportsStreaming ?? true,
            SupportsFunctionCalling = request.SupportsFunctionCalling ?? false,
            SupportsVision = request.SupportsVision ?? false,
            SupportsJsonMode = request.SupportsJsonMode ?? true,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        RegisteredModels[request.Id] = model;

        // Add to provider models
        if (!ProviderModels.TryGetValue(request.Provider, out var providerModels))
        {
            providerModels = new List<ProviderModel>();
            ProviderModels[request.Provider] = providerModels;
        }

        providerModels.Add(new ProviderModel
        {
            Id = request.Id,
            Description = request.Description,
        });

        var response = new ModelDetailResponse
        {
            Id = model.Id,
            Created = model.CreatedAt,
            OwnedBy = model.Provider,
            Capabilities = new ModelCapabilities
            {
                Streaming = model.SupportsStreaming,
                FunctionCalling = model.SupportsFunctionCalling,
                Vision = model.SupportsVision,
                JsonMode = model.SupportsJsonMode,
            },
            ContextWindow = model.ContextWindow,
            Description = model.Description,
            MaxTokens = model.MaxTokens,
        };

        return Task.FromResult<IActionResult>(this.Created($"/api/models/{request.Id}", response));
    }

    /// <summary>
    /// Updates a registered model.
    /// </summary>
    /// <param name="id">The model ID.</param>
    /// <param name="request">The model update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated model.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ModelDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> UpdateModelAsync(
        [FromRoute, Required] string id,
        [FromBody] UpdateModelRequest request,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Updating model {ModelId}", id);

        if (!RegisteredModels.TryGetValue(id, out var model))
        {
            return Task.FromResult<IActionResult>(this.NotFound(new { error = "Model not found" }));
        }

        // Update only provided fields
        if (request.Description is not null)
        {
            model.Description = request.Description;
        }

        if (request.ContextWindow.HasValue)
        {
            model.ContextWindow = request.ContextWindow.Value;
        }

        if (request.MaxTokens.HasValue)
        {
            model.MaxTokens = request.MaxTokens.Value;
        }

        if (request.SupportsStreaming.HasValue)
        {
            model.SupportsStreaming = request.SupportsStreaming.Value;
        }

        if (request.SupportsFunctionCalling.HasValue)
        {
            model.SupportsFunctionCalling = request.SupportsFunctionCalling.Value;
        }

        if (request.SupportsVision.HasValue)
        {
            model.SupportsVision = request.SupportsVision.Value;
        }

        if (request.SupportsJsonMode.HasValue)
        {
            model.SupportsJsonMode = request.SupportsJsonMode.Value;
        }

        var response = new ModelDetailResponse
        {
            Id = model.Id,
            Created = model.CreatedAt,
            OwnedBy = model.Provider,
            Capabilities = new ModelCapabilities
            {
                Streaming = model.SupportsStreaming,
                FunctionCalling = model.SupportsFunctionCalling,
                Vision = model.SupportsVision,
                JsonMode = model.SupportsJsonMode,
            },
            ContextWindow = model.ContextWindow,
            Description = model.Description,
            MaxTokens = model.MaxTokens,
        };

        return Task.FromResult<IActionResult>(this.Ok(response));
    }

    /// <summary>
    /// Removes a registered model.
    /// </summary>
    /// <param name="id">The model ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> RemoveModelAsync(
        [FromRoute, Required] string id,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Removing model {ModelId}", id);

        if (!RegisteredModels.TryGetValue(id, out var model))
        {
            return Task.FromResult<IActionResult>(this.NotFound(new { error = "Model not found" }));
        }

        RegisteredModels.Remove(id);

        // Remove from provider models
        if (ProviderModels.TryGetValue(model.Provider, out var providerModels))
        {
            providerModels.RemoveAll(m => m.Id == id);
        }

        return Task.FromResult<IActionResult>(this.NoContent());
    }

    /// <summary>
    /// Lists all available providers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of providers.</returns>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(ProvidersResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> ListProvidersAsync(CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Listing providers");

        // Get providers from configuration and registered models
        var configProviders = this.configuration.Value.AvailableModels
            .Select(m => m.Provider)
            .Distinct()
            .ToList();

        var registeredProviders = RegisteredModels.Values
            .Select(m => m.Provider)
            .Distinct()
            .ToList();

        var allProviders = configProviders.Union(registeredProviders).Distinct();

        var providers = allProviders.Select(p => new ProviderInfo
        {
            Name = p,
            ModelCount = this.GetModelCountForProvider(p),
        }).ToList();

        var response = new ProvidersResponse
        {
            Providers = providers,
        };

        return Task.FromResult<IActionResult>(this.Ok(response));
    }

    /// <summary>
    /// Lists models for a specific provider.
    /// </summary>
    /// <param name="provider">The provider name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of provider models.</returns>
    [HttpGet("providers/{provider}/models")]
    [ProducesResponseType(typeof(ProviderModelsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ListProviderModelsAsync(
        [FromRoute, Required] string provider,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Listing models for provider {Provider}", provider);

        var configModels = this.configuration.Value.AvailableModels
            .Where(m => m.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
            .Select(m => new ProviderModel
            {
                Id = m.Id,
                Description = $"Model from {m.Provider}",
            });

        var registeredModels = RegisteredModels.Values
            .Where(m => m.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
            .Select(m => new ProviderModel
            {
                Id = m.Id,
                Description = m.Description,
            });

        var allModels = configModels.Concat(registeredModels).ToList();

        if (allModels.Count == 0)
        {
            return Task.FromResult<IActionResult>(this.NotFound(new { error = "Provider not found" }));
        }

        var response = new ProviderModelsResponse
        {
            Provider = provider,
            Models = allModels,
        };

        return Task.FromResult<IActionResult>(this.Ok(response));
    }

    private int GetModelCountForProvider(string provider)
    {
        var configCount = this.configuration.Value.AvailableModels
            .Count(m => m.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));
        var registeredCount = RegisteredModels.Values
            .Count(m => m.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));
        return configCount + registeredCount;
    }
}

/// <summary>
/// Configuration for available models.
/// </summary>
public class ModelConfiguration
{
    /// <summary>
    /// Gets or sets the list of available models.
    /// </summary>
    public List<ModelDefinition> AvailableModels { get; set; } = new();
}

/// <summary>
/// Represents a model definition.
/// </summary>
public class ModelDefinition
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the model supports streaming.
    /// </summary>
    public bool SupportsStreaming { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports function calling.
    /// </summary>
    public bool SupportsFunctionCalling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports vision.
    /// </summary>
    public bool SupportsVision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports JSON mode.
    /// </summary>
    public bool SupportsJsonMode { get; set; }
}

/// <summary>
/// Represents a registered model.
/// </summary>
public class RegisteredModel
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the context window size.
    /// </summary>
    public int ContextWindow { get; set; }

    /// <summary>
    /// Gets or sets the maximum tokens.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports streaming.
    /// </summary>
    public bool SupportsStreaming { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports function calling.
    /// </summary>
    public bool SupportsFunctionCalling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports vision.
    /// </summary>
    public bool SupportsVision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports JSON mode.
    /// </summary>
    public bool SupportsJsonMode { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public long CreatedAt { get; set; }
}

/// <summary>
/// Request to register a model.
/// </summary>
public class RegisterModelRequest
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    [Required]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the context window size.
    /// </summary>
    public int? ContextWindow { get; set; }

    /// <summary>
    /// Gets or sets the maximum tokens.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports streaming.
    /// </summary>
    public bool? SupportsStreaming { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports function calling.
    /// </summary>
    public bool? SupportsFunctionCalling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports vision.
    /// </summary>
    public bool? SupportsVision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports JSON mode.
    /// </summary>
    public bool? SupportsJsonMode { get; set; }
}

/// <summary>
/// Request to update a model.
/// </summary>
public class UpdateModelRequest
{
    /// <summary>
    /// Gets or sets the model description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the context window size.
    /// </summary>
    public int? ContextWindow { get; set; }

    /// <summary>
    /// Gets or sets the maximum tokens.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports streaming.
    /// </summary>
    public bool? SupportsStreaming { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports function calling.
    /// </summary>
    public bool? SupportsFunctionCalling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports vision.
    /// </summary>
    public bool? SupportsVision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports JSON mode.
    /// </summary>
    public bool? SupportsJsonMode { get; set; }
}

/// <summary>
/// Represents detailed model information.
/// </summary>
public class ModelDetailResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the model.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the object type.
    /// </summary>
    public string Object { get; set; } = "model";

    /// <summary>
    /// Gets or sets the Unix timestamp for when the model was created.
    /// </summary>
    public long Created { get; set; }

    /// <summary>
    /// Gets or sets the organization that owns the model.
    /// </summary>
    public string OwnedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model capabilities.
    /// </summary>
    public ModelCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Gets or sets the context window size.
    /// </summary>
    public int ContextWindow { get; set; }

    /// <summary>
    /// Gets or sets the maximum tokens.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets the model description.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Represents a provider information.
/// </summary>
public class ProviderInfo
{
    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of models available from this provider.
    /// </summary>
    public int ModelCount { get; set; }
}

/// <summary>
/// Response for listing providers.
/// </summary>
public class ProvidersResponse
{
    /// <summary>
    /// Gets or sets the list of providers.
    /// </summary>
    public List<ProviderInfo> Providers { get; set; } = new();
}

/// <summary>
/// Represents a model from a provider.
/// </summary>
public class ProviderModel
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model description.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Response for listing provider models.
/// </summary>
public class ProviderModelsResponse
{
    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of models.
    /// </summary>
    public List<ProviderModel> Models { get; set; } = new();
}
