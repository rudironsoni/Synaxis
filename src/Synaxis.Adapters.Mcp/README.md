# Synaxis.Adapters.Mcp

Model Context Protocol (MCP) adapter for tool integration and context management.

## When to Use

Use this package when you need to:
- Integrate external tools with AI models
- Implement MCP server capabilities
- Provide context and resources to LLMs
- Build tool-calling workflows
- Support Claude Desktop, IDEs, and MCP clients
- Create custom MCP tools and resources

## Installation

```bash
dotnet add package Synaxis.Adapters.Mcp
```

## Quick Start

### Server Setup

```csharp
using Synaxis.Adapters.Mcp.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Synaxis core
builder.Services.AddSynaxis();

// Add MCP adapter
builder.Services.AddSynaxisAdapterMcp(options =>
{
    options.ServerName = "synaxis-mcp";
    options.ServerVersion = "1.0.0";
    options.EnableToolDiscovery = true;
});

var app = builder.Build();

// Map MCP endpoints
app.MapSynaxisMcpServer("/mcp");

app.Run();
```

### Define Custom Tool

```csharp
using Synaxis.Adapters.Mcp.Tools;

public class WeatherTool : IMcpTool
{
    public string Name => "get_weather";
    public string Description => "Get current weather for a location";
    
    public McpToolSchema Schema => new()
    {
        Type = "object",
        Properties = new Dictionary<string, object>
        {
            ["location"] = new { type = "string", description = "City name" },
            ["units"] = new { type = "string", enum = new[] { "celsius", "fahrenheit" } }
        },
        Required = new[] { "location" }
    };

    public async Task<object> ExecuteAsync(
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var location = parameters["location"].ToString();
        var units = parameters.GetValueOrDefault("units", "celsius").ToString();
        
        // Fetch weather data
        return new
        {
            location,
            temperature = 22,
            units,
            condition = "Sunny"
        };
    }
}

// Register tool
services.AddScoped<IMcpTool, WeatherTool>();
```

### Define Resource Provider

```csharp
using Synaxis.Adapters.Mcp.Resources;

public class DocumentResource : IMcpResource
{
    public string Uri => "document://my-docs";
    public string Name => "Company Documents";
    public string? Description => "Access to company documentation";
    public string MimeType => "text/plain";

    public async Task<string> GetContentAsync(
        CancellationToken cancellationToken = default)
    {
        // Fetch document content
        return "Document content here...";
    }
}

// Register resource
services.AddScoped<IMcpResource, DocumentResource>();
```

### Client Usage

```csharp
using Synaxis.Adapters.Mcp.Client;

var client = new McpClient("http://localhost:5000/mcp");

// Discover available tools
var tools = await client.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"{tool.Name}: {tool.Description}");
}

// Call a tool
var result = await client.CallToolAsync("get_weather", new
{
    location = "San Francisco",
    units = "celsius"
});

Console.WriteLine(result);

// Access resource
var content = await client.ReadResourceAsync("document://my-docs");
Console.WriteLine(content);
```

## Features

- **Tool Discovery** - Automatic tool registration and discovery
- **Resource Management** - Provide context and data to models
- **Schema Validation** - JSON Schema-based parameter validation
- **Prompt Templates** - Reusable prompt definitions
- **Sampling Support** - Request completions from clients
- **Transport Agnostic** - HTTP, stdio, WebSocket support

## MCP Protocol Support

Implements MCP specification:
- **Tools** - Callable functions with typed parameters
- **Resources** - Accessible data sources (files, APIs, databases)
- **Prompts** - Template-based prompt generation
- **Sampling** - Request LLM completions
- **Logging** - Structured logging to clients

## Configuration

```csharp
services.AddSynaxisAdapterMcp(options =>
{
    options.ServerName = "synaxis-mcp";
    options.ServerVersion = "1.0.0";
    options.EnableToolDiscovery = true;        // Auto-discover tools
    options.EnableResourceDiscovery = true;    // Auto-discover resources
    options.MaxToolExecutionTime = TimeSpan.FromSeconds(30);
    options.ValidateSchemas = true;            // Validate parameters
});
```

## Built-in Tools

The adapter includes standard Synaxis tools:
- **chat** - Execute chat completions
- **embed** - Generate embeddings
- **image** - Generate images
- **transcribe** - Transcribe audio
- **rerank** - Rerank documents

## Integration with Claude Desktop

Add to Claude Desktop config (`~/Library/Application Support/Claude/claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "synaxis": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/synaxis-mcp"],
      "env": {
        "SYNAXIS_OPENAI_KEY": "your-key"
      }
    }
  }
}
```

## Dependencies

- .NET 10.0
- Synaxis.Abstractions
- Synaxis.Contracts
- Synaxis (core library)
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging

## Documentation

Full documentation at [docs/packages/Synaxis.Adapters.Mcp.md](/docs/packages/Synaxis.Adapters.Mcp.md)
