// <copyright file="ProviderSelector.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Routing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.Abstractions.Routing;

    /// <summary>
    /// Default implementation of provider selection logic.
    /// </summary>
    public sealed class ProviderSelector : IProviderSelector
    {
        private readonly ILogger<ProviderSelector> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderSelector"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ProviderSelector(ILogger<ProviderSelector> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            this._logger = logger;
        }

        /// <inheritdoc/>
        public Task<string> SelectProviderAsync(object request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            this._logger.LogDebug("Selecting provider for request type {RequestType}", request.GetType().Name);
            return Task.FromResult("default");
        }
    }
}
