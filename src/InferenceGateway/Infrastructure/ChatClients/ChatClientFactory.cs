using System;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.InferenceGateway.Application.ChatClients;
using Microsoft.Extensions.AI;

namespace Synaxis.InferenceGateway.Infrastructure.ChatClients
{
    public class ChatClientFactory : IChatClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ChatClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IChatClient? GetClient(string? key = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                return _serviceProvider.GetService<IChatClient>();
            }

            return _serviceProvider.GetKeyedService<IChatClient>(key);
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            if (serviceKey == null) return _serviceProvider.GetService(serviceType);
            return _serviceProvider.GetKeyedService(serviceType, serviceKey);
        }
    }
}
