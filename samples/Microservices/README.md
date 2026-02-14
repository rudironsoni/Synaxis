# Synaxis Microservices Sample

This sample demonstrates how to use Synaxis in a microservices architecture with message queues (RabbitMQ) for asynchronous processing.

## Architecture

```
┌─────────────┐      ┌──────────────┐      ┌─────────────┐
│   Client    │─────▶│  RabbitMQ    │─────▶│ Chat Service│
└─────────────┘      │  Message     │      └─────────────┘
                     │    Queue     │
┌─────────────┐      └──────────────┘      ┌─────────────┐
│   Client    │─────▶│  RabbitMQ    │─────▶│Embedding    │
└─────────────┘      │  Message     │      │Service      │
                     │    Queue     │      └─────────────┘
┌─────────────┐      └──────────────┘      ┌─────────────┐
│   Client    │─────▶│  RabbitMQ    │─────▶│Summary      │
└─────────────┘      │  Message     │      │Service      │
                     │    Queue     │      └─────────────┘
                     └──────────────┘
```

## Services

### Chat Service
- Processes chat completion requests
- Queue: `chat.requests`
- Response Queue: `chat.responses.{requestId}`

### Embedding Service
- Generates text embeddings
- Queue: `embedding.requests`
- Response Queue: `embedding.responses.{requestId}`

### Summarization Service
- Summarizes long text
- Queue: `summarization.requests`
- Response Queue: `summarization.responses.{requestId}`

## Prerequisites

- .NET 10 SDK
- RabbitMQ server (local or remote)
- OpenAI API key

## Setup

### 1. Install RabbitMQ

**macOS**:
```bash
brew install rabbitmq
brew services start rabbitmq
```

**Linux**:
```bash
sudo apt-get install rabbitmq-server
sudo systemctl start rabbitmq-server
```

**Windows**:
Download from [https://www.rabbitmq.com/download.html](https://www.rabbitmq.com/download.html)

### 2. Configure

Edit `appsettings.json`:

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here"
  }
}
```

### 3. Run

```bash
dotnet restore
dotnet run
```

## Usage Examples

### Send Chat Request

```csharp
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

var request = new ChatRequestMessage
{
    Messages = new[]
    {
        new ChatMessage { Role = "user", Content = "Hello!" }
    },
    Model = "gpt-3.5-turbo"
};

var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
channel.BasicPublish("", "chat.requests", null, body);
```

### Send Embedding Request

```csharp
var request = new EmbeddingRequestMessage
{
    Input = "Hello, world!",
    Model = "text-embedding-ada-002"
};

var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
channel.BasicPublish("", "embedding.requests", null, body);
```

### Send Summarization Request

```csharp
var request = new SummarizationRequestMessage
{
    Text = "Long text to summarize...",
    MaxTokens = 200
};

var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
channel.BasicPublish("", "summarization.requests", null, body);
```

### Receive Response

```csharp
var responseQueue = $"chat.responses.{request.RequestId}";
channel.QueueDeclare(responseQueue, durable: false, exclusive: true, autoDelete: true);

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var response = JsonSerializer.Deserialize<ChatResponseMessage>(body);
    Console.WriteLine($"Response: {response.Response.Choices[0].Message.Content}");
};

channel.BasicConsume(responseQueue, true, consumer);
```

## Scaling

### Horizontal Scaling

Run multiple instances of the same service:

```bash
# Terminal 1
dotnet run

# Terminal 2
dotnet run

# Terminal 3
dotnet run
```

RabbitMQ will automatically distribute messages across all consumers.

### Service-Specific Scaling

Run only specific services by modifying `appsettings.json`:

```json
{
  "Services": {
    "ChatService": {
      "Enabled": true
    },
    "EmbeddingService": {
      "Enabled": false
    },
    "SummarizationService": {
      "Enabled": false
    }
  }
}
```

## Monitoring

### RabbitMQ Management UI

Access at: http://localhost:15672 (guest/guest)

Monitor:
- Queue depths
- Message rates
- Consumer connections
- Acknowledgment rates

### Logging

The application logs to console with information about:
- Connection status
- Message processing
- Errors and exceptions

## Best Practices

1. **Message Idempotency**: Include request IDs for deduplication
2. **Error Handling**: Implement dead-letter queues for failed messages
3. **Monitoring**: Track queue depths and processing times
4. **Scaling**: Add more consumers as needed
5. **Persistence**: Use durable queues for critical messages
6. **Acknowledgments**: Always ack/nack messages
7. **Timeouts**: Set appropriate timeouts for long-running operations

## Troubleshooting

### Connection Failed

**Error**: `Unable to connect to RabbitMQ`

**Solution**:
1. Verify RabbitMQ is running: `rabbitmqctl status`
2. Check host and port in `appsettings.json`
3. Verify credentials

### Queue Not Found

**Error**: `Queue not found`

**Solution**:
1. Ensure queues are declared before use
2. Check queue names match exactly
3. Verify virtual host is correct

### Message Not Processed

**Issue**: Messages stay in queue

**Solution**:
1. Check consumer is running
2. Verify service is enabled in configuration
3. Check logs for errors
4. Ensure messages are properly formatted

## Production Considerations

1. **Security**: Use strong passwords and TLS
2. **High Availability**: Use RabbitMQ clustering
3. **Persistence**: Enable durable queues and messages
4. **Monitoring**: Set up alerts for queue depths
5. **Dead Letter Queues**: Configure for failed messages
6. **Connection Pooling**: Reuse connections
7. **Message Size**: Limit message sizes
8. **Rate Limiting**: Implement backpressure

## Next Steps

- Add more microservices (translation, sentiment analysis, etc.)
- Implement dead-letter queues
- Add monitoring and alerting
- Deploy to Kubernetes
- Implement circuit breakers
- Add message compression
