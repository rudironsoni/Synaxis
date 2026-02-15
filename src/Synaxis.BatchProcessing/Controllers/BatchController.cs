// <copyright file="BatchController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing.Controllers
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Synaxis.BatchProcessing.Models;
    using Synaxis.BatchProcessing.Services;

    /// <summary>
    /// Controller for batch processing API endpoints.
    /// </summary>
    [ApiController]
    [Route("v1/batches")]
    [Authorize]
    public class BatchController : ControllerBase
    {
        private readonly IBatchStorageService _storageService;
        private readonly IBatchQueueService _queueService;
        private readonly ILogger<BatchController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchController"/> class.
        /// </summary>
        /// <param name="storageService">The batch storage service.</param>
        /// <param name="queueService">The batch queue service.</param>
        /// <param name="logger">The logger.</param>
        public BatchController(
            IBatchStorageService storageService,
            IBatchQueueService queueService,
            ILogger<BatchController> logger)
        {
            this._storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            this._queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Submits a new batch for processing.
        /// </summary>
        /// <param name="request">The batch request.</param>
        /// <returns>The created batch.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(BatchRequest), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> SubmitBatch([FromBody] CreateBatchRequest request)
        {
            if (request == null)
            {
                return this.BadRequest(new { error = "Request body is required" });
            }

            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            // Get user and organization from claims
            var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var organizationIdClaim = this.User.FindFirst("organization_id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.Unauthorized(new { error = "Invalid user identifier" });
            }

            if (string.IsNullOrEmpty(organizationIdClaim) || !Guid.TryParse(organizationIdClaim, out var organizationId))
            {
                return this.Unauthorized(new { error = "Invalid organization identifier" });
            }

            // Create batch
            var batch = new BatchRequest
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                UserId = userId,
                Name = request.Name,
                Description = request.Description,
                OperationType = request.OperationType,
                Items = request.Items,
                TotalItems = request.Items.Count,
                WebhookUrl = request.WebhookUrl,
                Priority = request.Priority,
                Status = BatchStatus.Pending,
            };

            this._logger.LogInformation("Creating batch {BatchId} for user {UserId} in organization {OrganizationId}", batch.Id, userId, organizationId);

            // Store batch
            await this._storageService.CreateBatchAsync(batch);

            // Enqueue batch for processing
            await this._queueService.EnqueueBatchAsync(batch);

            this._logger.LogInformation("Batch {BatchId} created and enqueued successfully", batch.Id);

            return this.CreatedAtAction(
                nameof(this.GetBatch),
                new { id = batch.Id },
                batch);
        }

        /// <summary>
        /// Gets the status of a batch.
        /// </summary>
        /// <param name="id">The batch identifier.</param>
        /// <returns>The batch status.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BatchRequest), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetBatch(Guid id)
        {
            this._logger.LogDebug("Retrieving batch {BatchId}", id);

            var batch = await this._storageService.GetBatchAsync(id);

            if (batch == null)
            {
                return this.NotFound(new { error = $"Batch {id} not found" });
            }

            // Verify user has access to this batch
            var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.Unauthorized(new { error = "Invalid user identifier" });
            }

            if (batch.UserId != userId)
            {
                return this.Forbid();
            }

            return this.Ok(batch);
        }

        /// <summary>
        /// Gets the results of a completed batch.
        /// </summary>
        /// <param name="id">The batch identifier.</param>
        /// <returns>The batch results.</returns>
        [HttpGet("{id}/results")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetBatchResults(Guid id)
        {
            this._logger.LogDebug("Retrieving results for batch {BatchId}", id);

            var batch = await this._storageService.GetBatchAsync(id);

            if (batch == null)
            {
                return this.NotFound(new { error = $"Batch {id} not found" });
            }

            // Verify user has access to this batch
            var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.Unauthorized(new { error = "Invalid user identifier" });
            }

            if (batch.UserId != userId)
            {
                return this.Forbid();
            }

            // Check if batch is completed
            if (batch.Status != BatchStatus.Completed)
            {
                return this.BadRequest(new
                {
                    error = "Batch is not completed",
                    status = batch.Status.ToString(),
                });
            }

            // Get results
            var results = await this._storageService.GetResultsAsync(id);

            if (results == null)
            {
                return this.NotFound(new { error = $"Results not found for batch {id}" });
            }

            return this.Ok(results);
        }
    }
}
