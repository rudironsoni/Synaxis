// <copyright file="EmbeddingCommandHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Handlers.Embeddings
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Mediator;
    using Microsoft.Extensions.Logging;
    using Synaxis.Abstractions.Routing;
    using Synaxis.Commands.Embeddings;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// Handles embedding generation commands by routing to the appropriate provider.
    /// </summary>
    public sealed class EmbeddingCommandHandler : IRequestHandler<EmbeddingCommand, EmbeddingResponse>
    {
        private readonly IProviderSelector _providerSelector;
        private readonly ILogger<EmbeddingCommandHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddingCommandHandler"/> class.
        /// </summary>
        /// <param name="providerSelector">The provider selector.</param>
        /// <param name="logger">The logger.</param>
        public EmbeddingCommandHandler(
            IProviderSelector providerSelector,
            ILogger<EmbeddingCommandHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(providerSelector);
            this._providerSelector = providerSelector;
            ArgumentNullException.ThrowIfNull(logger);
            this._logger = logger;
        }

        /// <inheritdoc/>
        public async ValueTask<EmbeddingResponse> Handle(EmbeddingCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            this._logger.LogDebug("Processing embedding command for model {Model}", request.Model);

            var providerName = await this._providerSelector.SelectProviderAsync(request, cancellationToken)
                .ConfigureAwait(false);

            this._logger.LogInformation("Selected provider {Provider} for embedding command", providerName);

            return new EmbeddingResponse
            {
                Data = Array.Empty<EmbeddingData>(),
            };
        }
    }
}
