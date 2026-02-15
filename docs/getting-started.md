# Getting Started with Synaxis

This guide will help you get up and running with Synaxis, whether you're using it as an embedded SDK, a self-hosted gateway, or connecting to a SaaS instance.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Quick Start Options](#quick-start-options)
- [Configuration](#configuration)
- [First Deployment](#first-deployment)
- [Next Steps](#next-steps)

## Prerequisites

### For SDK Usage

- **.NET 10 SDK** or later ([Download](https://dotnet.microsoft.com/download))
- **IDE**: Visual Studio 2022, VS Code, or Rider
- **API Key**: At least one AI provider (OpenAI, Anthropic, Azure, etc.)

### For Self-Hosted Gateway

- **Docker** 20.10+ or **Docker Compose** 2.0+
- **2GB RAM** minimum (4GB recommended)
- **10GB disk space**

### For Development

All of the above, plus:
- **Git** for cloning the repository
- **PostgreSQL** (optional, for full features)
- **Redis** (optional, for caching)

## Installation

### Option 1: SDK in Your Application

Add Synaxis packages to your .NET project:

```bash
# Core SDK (required)
dotnet add package Synaxis

# Choose your transport
dotnet add package Synaxis.Transport.Http
dotnet add package Synaxis.Transport.Grpc
dotnet add package Synaxis.Transport.WebSocket

# Add AI providers
dotnet add package Synaxis.Providers.OpenAI
dotnet add package Synaxis.Providers.Azure
dotnet add package Synaxis.Providers.Anthropic
```

### Option 2: Self-Hosted Gateway

#### Using Docker

```bash
# Pull the latest image
docker pull synaxis/gateway:latest

# Run with environment variables
docker run -d \
  -p 8080:8080 \
  -e OPENAI_API_KEY=your-key-here \
  -e AZURE_OPENAI_ENDPOINT=your-endpoint \
  synaxis/gateway:latest
```

#### Using Docker Compose

```bash
# Clone the repository
git clone https://github.com/rudironsoni/Synaxis.git
cd Synaxis

# Copy environment template
cp .env.example .env

# Edit .env with your API keys
nano .env

# Start all services
docker-compose up -d
```

### Option 3: Build from Source

```bash
# Clone the repository
git clone https://github.com/rudironsoni/Synaxis.git
cd Synaxis

# Restore dependencies
dotnet restore

# Build the solution
dotnet build -c Release

# Run the gateway
dotnet run --project src/Synaxis.Server/Synaxis.Server.csproj
```

## Quick Start Options

### SDK Quick Start (Embedded)

Create a new console application:

```bash
dotnet new console -n SynaxisQuickstart
cd SynaxisQuickstart
dotnet add package Synaxis
dotnet add package Synaxis.Providers.OpenAI
```

Add the following code to `Program.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Synaxis;
using Synaxis.Providers.OpenAI;

var host = Host.CreateApplicationBuilder(args);

// Configure Synaxis
host.Services.AddSynaxis(options =>
{
    options.AddOpenAIProvider(apiKey: "your-openai-api-key");
});

var app = host.Build();

// Get the chat service
var chatService = app.Services.GetRequiredService<IChatService>();

// Send a message
var response = await chatService.CompleteAsync(new ChatRequest
{
    Model = "gpt-4",
    Messages = new[]
    {
        new ChatMessage { Role = "user", Content = "Hello, Synaxis!" }
    }
});

Console.WriteLine(response.Choices[0].Message.Content);
```

Run it:

```bash
dotnet run
```

### Self-Hosted Gateway Quick Start

1. **Start the gateway**:

```bash
docker run -d \
  -p 8080:8080 \
  -e OPENAI_API_KEY=sk-your-key-here \
  --name synaxis-gateway \
  synaxis/gateway:latest
```

2. **Test with curl**:

```bash
curl -X POST http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [{"role": "user", "content": "Hello!"}]
  }'
```

3. **Test with JavaScript**:

```javascript
const response = await fetch('http://localhost:8080/v1/chat/completions', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    model: 'gpt-4',
    messages: [{ role: 'user', content: 'Hello!' }]
  })
});

const data = await response.json();
console.log(data.choices[0].message.content);
```

### SaaS Client Quick Start

If you're using a hosted Synaxis instance:

```csharp
using Synaxis.Client;

var client = new SynaxisClient(
  baseUrl: "https://api.synaxis.io",
  apiKey: "your-synaxis-api-key"
);

var response = await client.Chat.CompleteAsync(new ChatRequest
{
    Model = "gpt-4",
    Messages = new[]
    {
        new ChatMessage { Role = "user", Content = "Hello!" }
    }
});

Console.WriteLine(response.Choices[0].Message.Content);
```

## Configuration

### Environment Variables

Synaxis uses environment variables for configuration. Create a `.env` file:

```bash
# OpenAI Configuration
OPENAI_API_KEY=sk-your-openai-key
OPENAI_API_ENDPOINT=https://api.openai.com/v1

# Azure OpenAI Configuration
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com
AZURE_OPENAI_API_KEY=your-azure-key
AZURE_OPENAI_API_VERSION=2024-02-15-preview

# Anthropic Configuration
ANTHROPIC_API_KEY=sk-ant-your-key

# Server Configuration
SERVER_PORT=8080
SERVER_HOST=0.0.0.0

# Optional: Database (for features like caching, rate limiting)
POSTGRES_CONNECTION_STRING=Host=localhost;Database=synaxis;Username=synaxis;Password=your-password
REDIS_CONNECTION_STRING=localhost:6379,password=your-password
```

### Configuration File (appsettings.json)

For SDK usage, you can also use `appsettings.json`:

```json
{
  "Synaxis": {
    "Providers": {
      "OpenAI": {
        "ApiKey": "sk-your-key",
        "Endpoint": "https://api.openai.com/v1",
        "DefaultModel": "gpt-4"
      },
      "Azure": {
        "Endpoint": "https://your-resource.openai.azure.com",
        "ApiKey": "your-key",
        "ApiVersion": "2024-02-15-preview"
      }
    },
    "Routing": {
      "Strategy": "CostOptimized",
      "EnableFallback": true
    },
    "RateLimiting": {
      "Enabled": true,
      "RequestsPerMinute": 60
    }
  }
}
```

### Programmatic Configuration

```csharp
builder.Services.AddSynaxis(options =>
{
    // Add OpenAI provider
    options.AddOpenAIProvider(config =>
    {
        config.ApiKey = builder.Configuration["OpenAI:ApiKey"];
        config.Endpoint = builder.Configuration["OpenAI:Endpoint"];
        config.DefaultModel = "gpt-4";
    });

    // Add Azure provider as fallback
    options.AddAzureProvider(config =>
    {
        config.Endpoint = builder.Configuration["Azure:Endpoint"];
        config.ApiKey = builder.Configuration["Azure:ApiKey"];
        config.ApiVersion = "2024-02-15-preview";
    });

    // Configure routing
    options.RoutingStrategy = RoutingStrategy.CostOptimized;
    options.EnableAutomaticFallback = true;

    // Configure rate limiting
    options.EnableRateLimiting = true;
    options.MaxRequestsPerMinute = 60;

    // Configure retries
    options.MaxRetries = 3;
    options.RetryDelay = TimeSpan.FromSeconds(2);
    options.EnableExponentialBackoff = true;
});
```

## First Deployment

### Deploy to Azure

#### Using Azure Container Instances

```bash
# Create a resource group
az group create --name synaxis-rg --location eastus

# Create a container instance
az container create \
  --resource-group synaxis-rg \
  --name synaxis-gateway \
  --image synaxis/gateway:latest \
  --dns-name-label synaxis-gateway-unique \
  --ports 8080 \
  --environment-variables \
    OPENAI_API_KEY=sk-your-key \
    AZURE_OPENAI_ENDPOINT=your-endpoint

# Get the FQDN
az container show \
  --resource-group synaxis-rg \
  --name synaxis-gateway \
  --query ipAddress.fqdn \
  --output tsv
```

#### Using Azure App Service

```bash
# Create an App Service plan
az appservice plan create \
  --name synaxis-plan \
  --resource-group synaxis-rg \
  --sku B1 \
  --is-linux

# Create a web app
az webapp create \
  --name synaxis-gateway-unique \
  --resource-group synaxis-rg \
  --plan synaxis-plan \
  --deployment-container-image-name synaxis/gateway:latest

# Configure environment variables
az webapp config appsettings set \
  --resource-group synaxis-rg \
  --name synaxis-gateway-unique \
  --settings \
    OPENAI_API_KEY=sk-your-key \
    AZURE_OPENAI_ENDPOINT=your-endpoint
```

### Deploy to Kubernetes

Create a `deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: synaxis-gateway
spec:
  replicas: 3
  selector:
    matchLabels:
      app: synaxis-gateway
  template:
    metadata:
      labels:
        app: synaxis-gateway
    spec:
      containers:
      - name: synaxis
        image: synaxis/gateway:latest
        ports:
        - containerPort: 8080
        env:
        - name: OPENAI_API_KEY
          valueFrom:
            secretKeyRef:
              name: synaxis-secrets
              key: openai-api-key
        - name: AZURE_OPENAI_ENDPOINT
          valueFrom:
            secretKeyRef:
              name: synaxis-secrets
              key: azure-endpoint
---
apiVersion: v1
kind: Service
metadata:
  name: synaxis-service
spec:
  selector:
    app: synaxis-gateway
  ports:
  - port: 80
    targetPort: 8080
  type: LoadBalancer
```

Create secrets:

```bash
kubectl create secret generic synaxis-secrets \
  --from-literal=openai-api-key=sk-your-key \
  --from-literal=azure-endpoint=your-endpoint
```

Deploy:

```bash
kubectl apply -f deployment.yaml
```

### Deploy to AWS

#### Using ECS

```bash
# Create a task definition
aws ecs register-task-definition \
  --cli-input-json file://task-definition.json

# Create a service
aws ecs create-service \
  --cluster synaxis-cluster \
  --service-name synaxis-gateway \
  --task-definition synaxis-task \
  --desired-count 2 \
  --launch-type FARGATE
```

## Next Steps

- **[Architecture Guide](./architecture.md)** - Learn about Synaxis architecture
- **[API Reference](./api-reference.md)** - Explore the complete API
- **[Deployment Guide](./deployment.md)** - Advanced deployment options
- **[Development Guide](./development.md)** - Contribute to Synaxis
- **[Examples](../examples/)** - Browse code examples

## Troubleshooting

### Common Issues

#### "API key not valid"

- Verify your API key is correct
- Check that the key has proper permissions
- Ensure the key isn't expired

#### "Connection refused"

- Check that the gateway is running: `docker ps`
- Verify the port mapping: `docker port synaxis-gateway`
- Check firewall settings

#### "Rate limit exceeded"

- Configure rate limiting in your settings
- Implement retry logic with exponential backoff
- Consider upgrading your API plan

### Getting Help

- **Documentation**: [https://docs.synaxis.io](https://docs.synaxis.io)
- **GitHub Issues**: [https://github.com/rudironsoni/Synaxis/issues](https://github.com/rudironsoni/Synaxis/issues)
- **Discord Community**: [Join our Discord](https://discord.gg/synaxis)

---

**Ready to dive deeper?** Check out our [architecture documentation](./architecture.md) to understand how Synaxis works under the hood.
