// <copyright file="UserController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// User controller for provider requests.
    /// </summary>
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public UserController(ILogger<UserController> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            this.logger = logger;
        }

        /// <summary>
        /// Submits a BYOK provider request.
        /// </summary>
        /// <param name="request">The provider request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Request result.</returns>
        [HttpPost("providers/request")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> SubmitProviderRequest(
            [FromBody] ProviderRequestDto request,
            CancellationToken cancellationToken = default)
        {
            this.logger.LogInformation("User submitted provider request for '{ProviderKey}'", request.ProviderKey);

            // NOTE: Implement provider request submission (create ProviderRequest entity with status Pending). This is interim implementation.
            await Task.CompletedTask.ConfigureAwait(false);

            return this.CreatedAtAction(
                nameof(this.GetProviderRequest),
                new { requestId = Guid.NewGuid() },
                new { message = $"Provider request {request.ProviderKey} submitted for approval" });
        }

        /// <summary>
        /// Gets a provider request by ID.
        /// </summary>
        /// <param name="requestId">The request ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Provider request.</returns>
        [HttpGet("providers/request/{requestId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetProviderRequest(
            Guid requestId,
            CancellationToken cancellationToken = default)
        {
            this.logger.LogInformation("User requested provider request '{RequestId}'", requestId);

            // NOTE: Implement provider request retrieval. This is interim implementation.
            await Task.CompletedTask.ConfigureAwait(false);

            return this.Ok(new { message = $"Provider request {requestId} not yet implemented" });
        }
    }

    /// <summary>
    /// DTO for submitting a provider request.
    /// </summary>
    /// <param name="ProviderKey">The provider key.</param>
    /// <param name="ApiKey">The API key.</param>
    /// <param name="BaseUrl">The base URL.</param>
    /// <param name="DisplayName">The display name.</param>
    /// <param name="Description">The description.</param>
    public record ProviderRequestDto(
        string ProviderKey,
        string ApiKey,
        string BaseUrl,
        string? DisplayName = null,
        string? Description = null);
}
