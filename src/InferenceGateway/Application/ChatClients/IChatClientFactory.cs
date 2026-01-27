using System;
using Microsoft.Extensions.AI;

namespace Synaxis.InferenceGateway.Application.ChatClients
{
    public interface IChatClientFactory
    {
        /// <summary>
        /// Resolve the IChatClient for the current request context or by provider key.
        /// If key is null, returns the primary IChatClient for the request.
        /// </summary>
        IChatClient? GetClient(string? key = null);

        /// <summary>
        /// Generic service accessor used by chat clients that expose GetService mirrors.
        /// </summary>
        object? GetService(Type serviceType, object? serviceKey = null);
    }
}
