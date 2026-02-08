# ADR-014: Explicit Registration Pattern

## Status
Accepted

## Context
Synaxis needs to register multiple extensibility points at startup:

1. **Providers**: AI service integrations (OpenAI, Anthropic, Google, etc.)
2. **Routing Strategies**: Logic for selecting providers (cost-based, quality-based, failover)
3. **Command Handlers**: Business logic for commands and queries
4. **Middleware**: Cross-cutting concerns (logging, metrics, retries)

Common .NET approaches for extensibility:

### Auto-Discovery via Assembly Scanning
```csharp
services.AddSynaxis(options => {
    options.ScanAssemblies(typeof(Program).Assembly);
});
```
**Problems:**
- **Dependency Ordering**: Handlers registered before dependencies available
- **Testability**: Hard to control which handlers are registered
- **Startup Performance**: Reflection-based scanning adds 50-500ms
- **Debugging**: Difficult to trace why a handler was or wasn't registered
- **Versioning**: Breaking changes if scanning discovers unexpected types

### Service Locator Pattern
```csharp
var provider = ServiceLocator.GetRequiredService<IProviderClient>();
```
**Problems:**
- **Hidden Dependencies**: Not visible in constructor
- **Runtime Failures**: Missing services discovered at runtime, not compile-time
- **Testing**: Hard to mock service locator itself

### Magic Registration (Conventions)
```csharp
services.AddSynaxis(); // Registers everything it can find
```
**Problems:**
- **Implicit Behavior**: Unclear what gets registered
- **Side Effects**: May register unintended services
- **Configuration**: Hard to override conventions

## Decision
Require **explicit registration** of all Synaxis components:

### Provider Registration
```csharp
services.AddSynaxis(options => {
    // Explicit provider registration
    options.AddOpenAIProvider(config => {
        config.ApiKey = configuration["OpenAI:ApiKey"];
        config.Models = ["gpt-4o", "gpt-4o-mini"];
    });

    options.AddAnthropicProvider(config => {
        config.ApiKey = configuration["Anthropic:ApiKey"];
        config.Models = ["claude-3-5-sonnet-20241022"];
    });

    // No auto-discovery of providers
});
```

### Routing Strategy Registration
```csharp
services.AddSynaxis(options => {
    // Explicit routing strategy
    options.UseRoutingStrategy<CostBasedRoutingStrategy>();

    // Or configure inline
    options.UseRoutingStrategy(builder => {
        builder.PreferProvider("openai")
               .FallbackTo("anthropic")
               .WithMaxCostPerRequest(0.01m);
    });
});
```

### Handler Registration
```csharp
services.AddSynaxis(options => {
    // Explicit handler registration
    options.AddCommandHandler<CreateInferenceCommand, CreateInferenceHandler>();
    options.AddQueryHandler<GetModelQuery, GetModelHandler>();
    options.AddStreamHandler<StreamInferenceQuery, StreamInferenceHandler>();

    // Optional: Scan specific namespace only (opt-in)
    options.AddHandlersFromNamespace("Synaxis.Application.Commands");
});
```

### Middleware Registration
```csharp
services.AddSynaxis(options => {
    // Explicit middleware pipeline
    options.UseMiddleware<LoggingMiddleware>();
    options.UseMiddleware<MetricsMiddleware>();
    options.UseMiddleware<RetryMiddleware>();

    // Order matters: registered in execution order
});
```

### Testing Registration
```csharp
// In tests, register only what's needed
services.AddSynaxis(options => {
    options.AddProviderMock("test-provider");
    options.UseRoutingStrategy<AlwaysFirstStrategy>();
    options.AddCommandHandler<TestCommand, TestHandler>();
});
```

## Consequences

### Positive
- **Predictable Startup**: Clear what gets registered and when
- **Testability**: Easy to register subset of components in tests
- **Performance**: No assembly scanning overhead
- **Debugging**: Clear registration trace in stack traces
- **Compile-Time Safety**: Missing registrations caught at build time (with analyzers)
- **Documentation**: Registration code serves as documentation
- **Control**: Easy to override or exclude specific components

### Negative
- **Boilerplate**: More code than magic registration
- **Maintenance**: Need to update registration when adding new handlers
- **Onboarding**: New developers must learn registration patterns

### Mitigation Strategies

#### 1. Builder Pattern for Discoverability
```csharp
services.AddSynaxis(options => {
    options.AddOpenAIProvider(...); // IntelliSense shows available providers
});
```

#### 2. Source Generators for Common Cases
```csharp
// Generate registration code at compile-time
[assembly: RegisterAllHandlers]

// Generates:
// services.AddCommandHandler<Cmd1, Handler1>();
// services.AddCommandHandler<Cmd2, Handler2>();
```

#### 3. Opt-In Scanning for Specific Namespaces
```csharp
options.AddHandlersFromNamespace("Synaxis.Application.Commands");
// Explicit about where scanning happens
```

#### 4. Diagnostic Analyzers
- Warn if handler implements `ICommandHandler<T>` but not registered
- Suggest registration code in IDE quick-fix

#### 5. Validation at Startup
```csharp
services.AddSynaxis(options => {
    options.ValidateOnStartup(); // Throws if missing handlers
});
```

### Example: Full Registration
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSynaxis(options => {
        // Providers
        options.AddOpenAIProvider(config => { /* ... */ });
        options.AddAnthropicProvider(config => { /* ... */ });

        // Routing
        options.UseRoutingStrategy<TieredRoutingStrategy>();

        // Handlers
        options.AddCommandHandler<CreateInferenceCommand, CreateInferenceHandler>();
        options.AddQueryHandler<GetModelQuery, GetModelHandler>();
        options.AddStreamHandler<StreamInferenceQuery, StreamInferenceHandler>();

        // Middleware
        options.UseMiddleware<LoggingMiddleware>();
        options.UseMiddleware<RetryMiddleware>();

        // Validation
        options.ValidateOnStartup();
    });
}
```

## Related
- [ADR-012: SDK-First Package Architecture](./012-sdk-first-package-architecture.md)
- [ADR-013: Transport Abstraction with Mediator](./013-transport-abstraction-mediator.md)
- [ADR-002: Tiered Routing Strategy](./002-tiered-routing-strategy.md)
