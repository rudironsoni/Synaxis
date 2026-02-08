// <copyright file="ModelsController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.Http.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Controller for model listing endpoints.
    /// </summary>
    [ApiController]
    [Route("v1/models")]
    public class ModelsController : ControllerBase
    {
        private readonly ILogger<ModelsController> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelsController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public ModelsController(ILogger<ModelsController> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Lists available models.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of available models.</returns>
        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ListModelsAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Received list models request");

            // Placeholder implementation
            var response = new
            {
                @object = "list",
                data = new List<object>(),
            };

            return await Task.FromResult(this.Ok(response)).ConfigureAwait(false);
        }
    }
}
