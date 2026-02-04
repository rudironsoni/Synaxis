# WebSocket Real-Time Updates for Synaxis

This implementation provides real-time notifications for Synaxis using SignalR and WebSockets.

## Architecture

### Backend Components

#### 1. **SynaxisHub** (`src/InferenceGateway/WebApi/Hubs/SynaxisHub.cs`)
Main SignalR hub for real-time updates. Clients connect to this hub and join organization groups.

**Methods:**
- `JoinOrganization(string organizationId)` - Subscribe to organization-specific updates
- `LeaveOrganization(string organizationId)` - Unsubscribe from organization updates

**Features:**
- JWT authentication required
- Automatic connection management
- Organization-based grouping for targeted updates

#### 2. **RealTimeNotifier** (`src/InferenceGateway/WebApi/Hubs/RealTimeNotifier.cs`)
Service for broadcasting notifications to connected clients.

**Interface:** `IRealTimeNotifier` (in `Application/RealTime/`)

**Methods:**
- `NotifyProviderHealthChanged()` - Provider health status updates
- `NotifyCostOptimizationApplied()` - Cost optimization alerts
- `NotifyModelDiscovered()` - New model discoveries
- `NotifySecurityAlert()` - Security issues
- `NotifyAuditEvent()` - Audit trail events

#### 3. **Update Types** (`src/InferenceGateway/Application/RealTime/`)
Strongly-typed records for real-time notifications:

- `ProviderHealthUpdate` - Provider health metrics
- `CostOptimizationResult` - Cost savings information
- `ModelDiscoveryResult` - New model availability
- `SecurityAlert` - Security warnings
- `AuditEvent` - Audit log entries

### Frontend Components

#### **RealtimeService** (`src/Synaxis.WebApp/ClientApp/src/services/realtimeService.ts`)
TypeScript client for WebSocket connections.

**Features:**
- Automatic reconnection with exponential backoff
- JWT token authentication
- Organization switching
- Event handler management
- Connection state tracking

**Usage:**
```typescript
const realtimeService = new RealtimeService(jwtToken, {
  onProviderHealthChanged: (update) => console.log('Health:', update),
  onCostOptimizationApplied: (result) => console.log('Cost:', result),
  onSecurityAlert: (alert) => console.log('Security:', alert),
});

await realtimeService.connect(organizationId);
```

## Configuration

### Program.cs Registration
```csharp
// In services configuration
builder.Services.AddSignalR();
builder.Services.AddSingleton<IRealTimeNotifier, RealTimeNotifier>();

// In app configuration
app.MapHub<SynaxisHub>("/hubs/synaxis");
```

### JWT Authentication for WebSockets
WebSocket connections use JWT tokens passed via query string:
```
wss://api.synaxis.dev/hubs/synaxis?access_token=YOUR_JWT_TOKEN
```

The `JwtBearerEvents.OnMessageReceived` handler extracts the token from the query string for SignalR connections.

### CORS Configuration
Ensure WebSocket endpoints are included in CORS policy:
```csharp
policy.WithOrigins(allowedOrigins)
      .AllowAnyMethod()
      .AllowAnyHeader()
      .AllowCredentials();
```

## Integration Points

### 1. **Health Monitoring Agent**
```csharp
public class HealthMonitoringAgent
{
    private readonly IRealTimeNotifier _notifier;
    
    public async Task CheckHealth()
    {
        // Perform health check
        var update = new ProviderHealthUpdate(/*...*/);
        await _notifier.NotifyProviderHealthChanged(organizationId, update);
    }
}
```

### 2. **Cost Optimization Agent**
```csharp
public class CostOptimizationAgent
{
    private readonly IRealTimeNotifier _notifier;
    
    public async Task OptimizeRouting()
    {
        // Apply optimization
        var result = new CostOptimizationResult(/*...*/);
        await _notifier.NotifyCostOptimizationApplied(organizationId, result);
    }
}
```

### 3. **Model Discovery Agent**
```csharp
public class ModelDiscoveryAgent
{
    private readonly IRealTimeNotifier _notifier;
    
    public async Task DiscoverModels()
    {
        // Discover new model
        var result = new ModelDiscoveryResult(/*...*/);
        await _notifier.NotifyModelDiscovered(organizationId, result);
    }
}
```

### 4. **Security Audit Agent**
```csharp
public class SecurityAuditAgent
{
    private readonly IRealTimeNotifier _notifier;
    
    public async Task AuditSecurity()
    {
        // Detect security issue
        var alert = new SecurityAlert(/*...*/);
        await _notifier.NotifySecurityAlert(organizationId, alert);
    }
}
```

### 5. **Audit Service**
```csharp
public class AuditService
{
    private readonly IRealTimeNotifier _notifier;
    
    public async Task LogAuditEvent()
    {
        var auditEvent = new AuditEvent(/*...*/);
        await _notifier.NotifyAuditEvent(organizationId, auditEvent);
    }
}
```

## Security

### Authentication
- **Required:** JWT token for all WebSocket connections
- **Enforcement:** `[Authorize]` attribute on `SynaxisHub`
- **Validation:** User authentication checked in `OnConnectedAsync()`

### Authorization
- Organization membership validation (TODO: implement in `JoinOrganization()`)
- Users can only join organizations they belong to
- Connections auto-abort if unauthenticated

### Rate Limiting
- Recommended: Implement rate limiting per user
- Limit concurrent connections per user
- Throttle message frequency

### Best Practices
1. Always validate organization membership before allowing `JoinOrganization()`
2. Never trust client-provided organization IDs without verification
3. Use HTTPS/WSS in production
4. Implement connection timeout policies
5. Monitor WebSocket connection count

## Testing

### Unit Tests
- `SynaxisHubTests.cs` - Hub method tests
- `RealTimeNotifierTests.cs` - Notification delivery tests

### Integration Tests
- `WebSocketConnectivityTests.cs` - End-to-end connection tests
- Marked with `Skip` attribute - require running application

**Running Integration Tests:**
```bash
# Set test JWT token
export TEST_JWT_TOKEN="your-test-token"

# Run tests with specific category
dotnet test --filter "FullyQualifiedName~WebSocketConnectivityTests"
```

## Monitoring

### Metrics to Track
- Active WebSocket connections
- Messages sent per second
- Connection failures/reconnections
- Average message latency
- Organization group sizes

### Logging
All operations are logged with appropriate log levels:
- `Debug` - Message sends
- `Information` - Connection lifecycle, security alerts
- `Warning` - Authentication failures
- `Error` - Send failures, exceptions

## Troubleshooting

### Connection Fails
1. Check JWT token validity
2. Verify CORS configuration
3. Ensure HTTPS/WSS in production
4. Check firewall/proxy WebSocket support

### Messages Not Received
1. Verify client joined correct organization
2. Check organization ID matches
3. Review error logs for send failures
4. Verify handler registration

### High Latency
1. Check Redis connection (if using backplane)
2. Monitor server load
3. Review network infrastructure
4. Consider geographic distribution

## Future Enhancements

1. **Backplane:** Add Redis backplane for multi-server deployments
2. **Compression:** Enable WebSocket compression for large messages
3. **Filtering:** Client-side message filtering
4. **History:** Message replay for reconnected clients
5. **Presence:** User presence tracking
6. **Typing Indicators:** Real-time collaboration features

## Dependencies

### NuGet Packages
- `Microsoft.AspNetCore.SignalR` (included in ASP.NET Core)

### NPM Packages
```json
{
  "@microsoft/signalr": "^8.0.0"
}
```

## Files Created

### Backend
- `src/InferenceGateway/WebApi/Hubs/SynaxisHub.cs`
- `src/InferenceGateway/WebApi/Hubs/RealTimeNotifier.cs`
- `src/InferenceGateway/Application/RealTime/IRealTimeNotifier.cs`
- `src/InferenceGateway/Application/RealTime/ProviderHealthUpdate.cs`
- `src/InferenceGateway/Application/RealTime/CostOptimizationResult.cs`
- `src/InferenceGateway/Application/RealTime/ModelDiscoveryResult.cs`
- `src/InferenceGateway/Application/RealTime/SecurityAlert.cs`
- `src/InferenceGateway/Application/RealTime/AuditEvent.cs`

### Frontend
- `src/Synaxis.WebApp/ClientApp/src/services/realtimeService.ts`

### Tests
- `tests/InferenceGateway/WebApi.Tests/Hubs/SynaxisHubTests.cs`
- `tests/InferenceGateway/WebApi.Tests/Hubs/RealTimeNotifierTests.cs`
- `tests/InferenceGateway/WebApi.Tests/Synaxis.InferenceGateway.WebApi.Tests.csproj`
- `tests/InferenceGateway/IntegrationTests/RealTime/WebSocketConnectivityTests.cs`

### Documentation
- This README
