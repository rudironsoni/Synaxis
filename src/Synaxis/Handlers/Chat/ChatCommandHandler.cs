// <copyright file="ChatCommandHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Handlers.Chat
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Mediator;
    using Microsoft.Extensions.Logging;
    using Synaxis.Abstractions.Providers;
    using Synaxis.Abstractions.Routing;
    using Synaxis.Commands.Chat;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// Handles chat completion commands by routing to the appropriate provider.
    /// </summary>
    public sealed class ChatCommandHandler : IRequestHandler<ChatCommand, ChatResponse>
    {
        private readonly IProviderSelector _providerSelector;
        private readonly ILogger<ChatCommandHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatCommandHandler"/> class.
        /// </summary>
        /// <param name="providerSelector">The provider selector.</param>
        /// <param name="logger">The logger.</param>
        public ChatCommandHandler(
            IProviderSelector providerSelector,
            ILogger<ChatCommandHandler> logger)
        {
            this._providerSelector = providerSelector ?? throw new ArgumentNullException(nameof(providerSelector));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async ValueTask<ChatResponse> Handle(ChatCommand request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            this._logger.LogDebug("Processing chat command for model {Model}", request.Model);

            var providerName = await this._providerSelector.SelectProviderAsync(request, cancellationToken)
                .ConfigureAwait(false);

            this._logger.LogInformation("Selected provider {Provider} for chat command", providerName);

            // TODO: Resolve provider and invoke ChatAsync
            // For now, return a placeholder response
            return new ChatResponse
            {
                Id = Guid.NewGuid().ToString(),
                Model = request.Model,
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Choices = Array.Empty<ChatChoice>(),
            };
        }
    }
}
