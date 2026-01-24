using Microsoft.Extensions.AI;
using OpenAI;
using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.Infrastructure;

public class GenericOpenAiChatClient : IChatClient
{
    private readonly IChatClient _innerClient;

    public GenericOpenAiChatClient(string apiKey, Uri endpoint, string modelId, Dictionary<string, string>? customHeaders = null, HttpClient? httpClient = null)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = endpoint
        };

        if (httpClient != null)
        {
            options.Transport = new HttpClientPipelineTransport(httpClient);
        }

        if (customHeaders != null && customHeaders.Count > 0)
        {
            options.AddPolicy(new CustomHeaderPolicy(customHeaders), PipelinePosition.PerCall);
        }

        var openAiClient = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        _innerClient = openAiClient.GetChatClient(modelId).AsIChatClient();
    }

    public ChatClientMetadata Metadata => _innerClient.GetService<ChatClientMetadata>() ?? new ChatClientMetadata("OpenAI");

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return _innerClient.GetResponseAsync(chatMessages, options, cancellationToken);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return _innerClient.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return _innerClient.GetService(serviceType, serviceKey);
    }

    public void Dispose()
    {
        _innerClient.Dispose();
    }

    private class CustomHeaderPolicy : PipelinePolicy
    {
        private readonly Dictionary<string, string> _headers;

        public CustomHeaderPolicy(Dictionary<string, string> headers)
        {
            _headers = headers;
        }

        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            foreach (var header in _headers)
            {
                message.Request.Headers.Set(header.Key, header.Value);
            }
            ProcessNext(message, pipeline, currentIndex);
        }

        public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            foreach (var header in _headers)
            {
                message.Request.Headers.Set(header.Key, header.Value);
            }
            await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
        }
    }
}
