# ADR-015: Contracts Versioning Strategy

## Status
Accepted

## Context
API contracts (request/response DTOs, commands, queries) evolve over time:

1. **New Features**: Adding fields to existing messages
2. **Breaking Changes**: Removing fields, changing types, renaming properties
3. **Deprecation**: Phasing out old endpoints or message formats
4. **Compatibility**: Supporting multiple client versions simultaneously

Challenges:
- **SDK Versioning**: Client SDK must work with older server versions
- **Wire Format**: JSON/protobuf serialization must handle missing fields
- **Type Safety**: Compile-time guarantees for version compatibility
- **Migration Path**: Clear upgrade path for consumers

Common versioning approaches:

### URL Versioning
```
/api/v1/inference
/api/v2/inference
```
**Pros**: Clear, simple  
**Cons**: URL sprawl, duplicate implementations

### Header Versioning
```
Accept: application/vnd.synaxis.v2+json
```
**Pros**: Clean URLs  
**Cons**: Easy to miss, harder to discover

### Content Negotiation
```json
{ "$type": "InferenceRequestV2", ... }
```
**Pros**: Flexible  
**Cons**: Runtime type checks, complex dispatching

### No Versioning (Additive Only)
```csharp
public class InferenceRequest {
    public string? NewField { get; set; } // Always optional
}
```
**Pros**: Simple  
**Cons**: Can't remove fields, breaks eventually

## Decision
Use **namespace-based versioning** for contracts with explicit version boundaries:

### 1. Versioned Namespaces
```csharp
// Synaxis.Contracts/V1/InferenceContracts.cs
namespace Synaxis.Contracts.V1
{
    public record InferenceRequest(
        string Model,
        string Prompt,
        int? MaxTokens = null
    );

    public record InferenceResponse(
        string Text,
        int TokensUsed
    );
}

// Synaxis.Contracts/V2/InferenceContracts.cs
namespace Synaxis.Contracts.V2
{
    public record InferenceRequest(
        string Model,
        IReadOnlyList<Message> Messages, // Breaking: changed from Prompt
        int? MaxTokens = null,
        StreamingOptions? Streaming = null // New field
    );

    public record InferenceResponse(
        string Text,
        int TokensUsed,
        UsageMetadata Metadata // New field
    );
}
```

### 2. Message Contracts (Not DTOs)
Only **message types** are versioned:
- Commands (e.g., `CreateInferenceCommand`)
- Queries (e.g., `GetModelQuery`)
- Events (e.g., `InferenceCompletedEvent`)

Internal DTOs, entities, and value objects are NOT versioned:
```csharp
// NOT versioned (internal use only)
namespace Synaxis.Domain.Entities
{
    public class InferenceRecord { ... }
}

// Versioned (public API)
namespace Synaxis.Contracts.V1
{
    public record CreateInferenceCommand : ICommand<InferenceResponse> { ... }
}
```

### 3. Breaking Changes = New Version
Breaking changes require new version namespace:

**Breaking Changes:**
- Removing fields
- Renaming properties
- Changing types (e.g., `string` → `Guid`)
- Making optional field required
- Changing semantics (same field, different meaning)

**Non-Breaking Changes (Same Version):**
- Adding optional fields (with defaults)
- Adding new message types
- Deprecating fields (marked `[Obsolete]`)

### 4. Deprecation Pattern
```csharp
namespace Synaxis.Contracts.V1
{
    public record InferenceRequest(
        string Model,
        [property: Obsolete("Use Messages instead. Will be removed in V3.")]
        string? Prompt,
        IReadOnlyList<Message>? Messages = null,
        int? MaxTokens = null
    );
}
```

### 5. Version Mapping
Server maintains version mappers:
```csharp
public class InferenceRequestMapper
{
    public V2.InferenceRequest MapFromV1(V1.InferenceRequest v1)
    {
        return new V2.InferenceRequest(
            Model: v1.Model,
            Messages: [new Message("user", v1.Prompt)], // Convert prompt to message
            MaxTokens: v1.MaxTokens,
            Streaming: null
        );
    }
}
```

### 6. Client SDK Version Selection
```csharp
// Explicit version selection
var client = new SynaxisClient(options => {
    options.UseApiVersion(ApiVersion.V2);
});

// Or auto-negotiate (default: latest)
var client = new SynaxisClient(options => {
    options.UseLatestCompatibleVersion();
});
```

## Consequences

### Positive
- **Backward Compatibility**: V1 clients work with V2 server (via mapping)
- **Clear Migration Path**: Consumers see `[Obsolete]` warnings before breaking changes
- **Type Safety**: Compile-time errors for incompatible versions
- **Discoverability**: IntelliSense shows available versions
- **Side-by-Side**: Both V1 and V2 can coexist in same codebase
- **Documentation**: Version in namespace makes intent clear

### Negative
- **Code Duplication**: Similar contracts duplicated across versions
- **Maintenance**: Need to maintain mappers between versions
- **Package Size**: Shipping all versions increases SDK size
- **Complexity**: More types to understand

### Mitigation Strategies

#### 1. Limit Active Versions
- Support **N** and **N-1** only (e.g., V2 and V1)
- Remove older versions after deprecation period (e.g., 12 months)

#### 2. Shared Base Types
```csharp
namespace Synaxis.Contracts.Common
{
    public record ModelInfo(string Id, string Name);
}

namespace Synaxis.Contracts.V1
{
    using Synaxis.Contracts.Common;
    public record GetModelQuery(string Id) : IQuery<ModelInfo>;
}
```

#### 3. Automated Mappers
Use source generators for common mapping patterns:
```csharp
[GenerateMapper(From = typeof(V1.InferenceRequest), To = typeof(V2.InferenceRequest))]
public partial class InferenceRequestMapper { }
```

#### 4. Version-Specific Packages (Future)
```xml
<PackageReference Include="Synaxis.Contracts.V2" Version="2.0.0" />
<!-- Only ships V2 contracts, smaller package -->
```

### Example: Full Version Lifecycle

**Phase 1: V1 Initial Release**
```csharp
namespace Synaxis.Contracts.V1
{
    public record InferenceRequest(string Model, string Prompt);
}
```

**Phase 2: V2 with Breaking Change**
```csharp
namespace Synaxis.Contracts.V2
{
    public record InferenceRequest(string Model, IReadOnlyList<Message> Messages);
}

// V1 marked deprecated
namespace Synaxis.Contracts.V1
{
    [Obsolete("Use V2.InferenceRequest instead. V1 will be removed in 6 months.")]
    public record InferenceRequest(string Model, string Prompt);
}
```

**Phase 3: V1 Removal**
```csharp
// V1 namespace removed entirely
// Only V2 remains
namespace Synaxis.Contracts.V2 { ... }
```

## Related
- [ADR-012: SDK-First Package Architecture](./012-sdk-first-package-architecture.md)
- [ADR-013: Transport Abstraction with Mediator](./013-transport-abstraction-mediator.md)
- [ADR-001: Stream-Native CQRS](./001-stream-native-cqrs.md)
