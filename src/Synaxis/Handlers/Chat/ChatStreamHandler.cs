// <copyright file="ChatStreamHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Handlers.Chat
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Mediator;
    using Microsoft.Extensions.Logging;
    using Synaxis.Abstractions.Routing;
    using Synaxis.Commands.Chat;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// Handles streaming chat completion commands by routing to the appropriate provider.
    /// </summary>
    public sealed class ChatStreamHandler : IStreamRequestHandler<ChatStreamCommand, ChatStreamChunk>
    {
        private readonly IProviderSelector _providerSelector;
        private readonly ILogger<ChatStreamHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatStreamHandler"/> class.
        /// </summary>
        /// <param name="providerSelector">The provider selector.</param>
        /// <param name="logger">The logger.</param>
        public ChatStreamHandler(
            IProviderSelector providerSelector,
            ILogger<ChatStreamHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(providerSelector);
            this._providerSelector = providerSelector;
            ArgumentNullException.ThrowIfNull(logger);
            this._logger = logger;
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ChatStreamChunk> Handle(
            ChatStreamCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            return this.HandleCore(request, cancellationToken);
        }

        private async IAsyncEnumerable<ChatStreamChunk> HandleCore(
            ChatStreamCommand request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            this._logger.LogDebug("Processing streaming chat command for model {Model}", request.Model);

            var providerName = await this._providerSelector.SelectProviderAsync(request, cancellationToken)
                .ConfigureAwait(false);

            this._logger.LogInformation("Selected provider {Provider} for streaming chat command", providerName);

            yield return new ChatStreamChunk
            {
                Id = Guid.NewGuid().ToString(),
                Model = request.Model,
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Choices = Array.Empty<ChatChoice>(),
            };
        }
    }
}
