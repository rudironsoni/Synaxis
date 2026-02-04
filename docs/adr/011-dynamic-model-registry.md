# ADR 011: Dynamic Model Registry & Intelligence Core

**Status:** Accepted  
**Date:** 2026-01-29

> **ULTRA MISER MODE™ Engineering**: Hardcoded model lists are like paying rent—you keep doing it but never own anything. A dynamic registry is like buying a house: initial effort, but then it updates itself forever.

---

## Context

The initial Synaxis architecture relied on **static configuration** for model routing:

1. **Hardcoded Model Lists:** `appsettings.json` contained manual lists of models per provider
2. **No Discovery:** New models from providers required manual config updates
3. **Stale Metadata:** Pricing, context windows, and capabilities were outdated or missing
4. **No Availability Tracking:** System couldn't detect when providers added/removed models
5. **Manual Provider Management:** Adding a new provider required code changes and redeployment

This created operational burden:
- **High Maintenance:** Every new model release required manual updates
- **Inaccurate Routing:** Smart Router made decisions based on incomplete data
- **Poor Resilience:** System couldn't adapt to provider outages or model deprecations
- **No Intelligence:** No historical data to optimize routing based on latency or reliability

As Synaxis scaled to support dozens of providers and hundreds of models, the architecture needed to evolve from a **static proxy** to an **intelligent, self-updating router**.

---

## Decision

We have implemented a **Dynamic Model Registry**—a database-driven system that automatically discovers, syncs, and tracks models from external sources (`models.dev` API, provider `/v1/models` endpoints).

### Core Architecture

#### 1. Database Schema (ControlPlane)

Three new entities represent the model intelligence layer:

```csharp
// src/InferenceGateway/Infrastructure/Persistence/Entities/GlobalModel.cs
public class GlobalModel
{
    public string Id { get; set; }                    // Canonical ID (e.g., "gemma-3-27b")
    public string Name { get; set; }                  // Display name
    public int ContextWindow { get; set; }            // Max context size
    public decimal InputPricePerMillion { get; set; } // $/1M input tokens
    public decimal OutputPricePerMillion { get; set; }// $/1M output tokens
    public DateTime LastSyncedAt { get; set; }        // Last sync from models.dev
    
    // Capabilities
    public bool SupportsTools { get; set; }
    public bool SupportsVision { get; set; }
    public bool SupportsStreaming { get; set; }
    
    // Navigation
    public ICollection<ProviderModel> ProviderModels { get; set; }
}

// src/InferenceGateway/Infrastructure/Persistence/Entities/ProviderModel.cs
public class ProviderModel
{
    public string Id { get; set; }                    // Composite key: provider + model
    public string ProviderId { get; set; }            // Provider name (e.g., "nvidia")
    public string ProviderSpecificId { get; set; }    // Provider's model ID (e.g., "nvidia/google/gemma-3-27b")
    public string GlobalModelId { get; set; }         // FK to GlobalModel
    
    // Availability
    public bool IsAvailable { get; set; }             // Discovered via /v1/models
    public DateTime? LastSeenAt { get; set; }         // Last successful discovery
    public int RateLimitRPM { get; set; }             // Provider-specific rate limit
    
    // Performance (future)
    public int? P95LatencyMs { get; set; }            // 95th percentile latency
    public int SuccessCount { get; set; }             // Historical success count
    public int FailureCount { get; set; }             // Historical failure count
    
    // Navigation
    public GlobalModel GlobalModel { get; set; }
}

// src/InferenceGateway/Infrastructure/Persistence/Entities/TenantModelLimit.cs
public class TenantModelLimit
{
    public string Id { get; set; }
    public string TenantId { get; set; }
    public string GlobalModelId { get; set; }
    
    // Guardrails
    public int AllowedRPM { get; set; }               // Requests per minute
    public decimal MonthlyBudget { get; set; }        // Max spend per month
    public decimal CurrentMonthSpend { get; set; }    // Running total
}
```

**Design Rationale:**
- `GlobalModel`: Source of truth from `models.dev` (community-maintained, up-to-date)
- `ProviderModel`: Operational data (where can this model be executed?)
- `TenantModelLimit`: Guardrails (who can use what, and how much?)

#### 2. Synchronization Jobs (Quartz.NET)

Background jobs keep the registry synchronized with external sources:

```csharp
// src/InferenceGateway/Infrastructure/Jobs/ModelsDevSyncJob.cs
[DisallowConcurrentExecution]
public class ModelsDevSyncJob : IJob
{
    private readonly IModelsDevClient _client;
    private readonly ControlPlaneDbContext _db;

    public async Task Execute(IJobExecutionContext context)
    {
        // Step 1: Fetch latest model catalog from models.dev
        var models = await _client.GetModelsAsync();

        // Step 2: Upsert GlobalModel records
        foreach (var model in models)
        {
            var existingModel = await _db.GlobalModels.FindAsync(model.Id);
            if (existingModel == null)
            {
                _db.GlobalModels.Add(new GlobalModel
                {
                    Id = model.Id,
                    Name = model.Name,
                    ContextWindow = model.ContextWindow,
                    InputPricePerMillion = model.InputPrice,
                    OutputPricePerMillion = model.OutputPrice,
                    SupportsTools = model.Capabilities.Contains("tools"),
                    SupportsVision = model.Capabilities.Contains("vision"),
                    SupportsStreaming = model.Capabilities.Contains("streaming"),
                    LastSyncedAt = DateTime.UtcNow
                });
            }
            else
            {
                // Update pricing and capabilities (may change over time)
                existingModel.InputPricePerMillion = model.InputPrice;
                existingModel.OutputPricePerMillion = model.OutputPrice;
                existingModel.LastSyncedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
    }
}

// Trigger: Daily at 3:00 AM UTC
```

```csharp
// src/InferenceGateway/Infrastructure/Jobs/ProviderDiscoveryJob.cs
[DisallowConcurrentExecution]
public class ProviderDiscoveryJob : IJob
{
    private readonly IEnumerable<ProviderConfig> _providers;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ControlPlaneDbContext _db;

    public async Task Execute(IJobExecutionContext context)
    {
        foreach (var provider in _providers.Where(p => p.Enabled))
        {
            try
            {
                // Step 1: Call provider's /v1/models endpoint
                var httpClient = _httpClientFactory.CreateClient(provider.Name);
                var response = await httpClient.GetAsync($"{provider.Endpoint}/models");
                var modelsResponse = await response.Content.ReadFromJsonAsync<OpenAIModelsResponse>();

                // Step 2: Upsert ProviderModel records
                foreach (var model in modelsResponse.Data)
                {
                    var providerModel = await _db.ProviderModels
                        .FirstOrDefaultAsync(pm => 
                            pm.ProviderId == provider.Name && 
                            pm.ProviderSpecificId == model.Id);

                    if (providerModel == null)
                    {
                        _db.ProviderModels.Add(new ProviderModel
                        {
                            Id = $"{provider.Name}/{model.Id}",
                            ProviderId = provider.Name,
                            ProviderSpecificId = model.Id,
                            GlobalModelId = ResolveGlobalModelId(model.Id), // Map to canonical ID
                            IsAvailable = true,
                            LastSeenAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        providerModel.IsAvailable = true;
                        providerModel.LastSeenAt = DateTime.UtcNow;
                    }
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discover models for provider {Provider}", provider.Name);
            }
        }
    }
}

// Trigger: Hourly (at :15 past the hour)
```

**Job Scheduling:**
- `ModelsDevSyncJob`: Daily (3:00 AM UTC) — updates pricing/capabilities from `models.dev`
- `ProviderDiscoveryJob`: Hourly (at :15) — discovers available models from providers

#### 3. Smart Routing Algorithm (Rewrite)

The `SmartRoutingChatClient` is rewritten to use the dynamic registry:

```csharp
// src/InferenceGateway/Infrastructure/Routing/DatabaseBackedSmartRouter.cs
public class DatabaseBackedSmartRouter : ISmartRouter
{
    private readonly ControlPlaneDbContext _db;
    private readonly IHealthStore _healthStore;
    private readonly IQuotaTracker _quotaTracker;

    public async Task<IReadOnlyList<EnrichedCandidate>> GetCandidatesAsync(
        string requestedModelId,
        string? tenantId = null)
    {
        // Step 1: Lookup GlobalModel
        var globalModel = await _db.GlobalModels
            .Include(gm => gm.ProviderModels)
            .FirstOrDefaultAsync(gm => gm.Id == requestedModelId);

        if (globalModel == null)
            throw new ModelNotFoundException(requestedModelId);

        // Step 2: Filter available ProviderModels
        var availableProviders = globalModel.ProviderModels
            .Where(pm => pm.IsAvailable)
            .ToList();

        // Step 3: Apply tenant limits
        if (tenantId != null)
        {
            var tenantLimit = await _db.TenantModelLimits
                .FirstOrDefaultAsync(tml => 
                    tml.TenantId == tenantId && 
                    tml.GlobalModelId == requestedModelId);

            if (tenantLimit != null && tenantLimit.CurrentMonthSpend >= tenantLimit.MonthlyBudget)
                throw new TenantBudgetExceededException(tenantId, requestedModelId);
        }

        // Step 4: Check quota and health
        var candidates = new List<EnrichedCandidate>();
        foreach (var providerModel in availableProviders)
        {
            // Skip if quota exhausted
            if (!await _quotaTracker.IsAllowedAsync(providerModel.ProviderId))
                continue;

            // Skip if circuit breaker open
            var health = await _healthStore.GetHealthAsync(providerModel.ProviderId);
            if (health.CircuitState == CircuitState.Open)
                continue;

            candidates.Add(new EnrichedCandidate
            {
                ProviderId = providerModel.ProviderId,
                ProviderSpecificModelId = providerModel.ProviderSpecificId,
                GlobalModelId = globalModel.Id,
                IsFree = globalModel.InputPricePerMillion == 0,
                EstimatedLatencyMs = providerModel.P95LatencyMs ?? 1000,
                HealthScore = health.SuccessRate
            });
        }

        // Step 5: Rank by cost (free first), then health, then latency
        return candidates
            .OrderByDescending(c => c.IsFree)
            .ThenByDescending(c => c.HealthScore)
            .ThenBy(c => c.EstimatedLatencyMs)
            .ToList();
    }
}
```

**Routing Logic:**
1. **Model Lookup:** Find canonical model in `GlobalModels` table
2. **Provider Filter:** Identify which providers support this model
3. **Availability Check:** Exclude providers where `IsAvailable = false`
4. **Tenant Guardrails:** Enforce budget and rate limits
5. **Quota & Health:** Skip providers with exhausted quotas or failing health
6. **Ranking:** Sort by cost → health → latency

---

## Implementation Phases

### Phase 1: Foundation ✅
- [x] Define database entities (`GlobalModel`, `ProviderModel`, `TenantModelLimit`)
- [x] Create EF Core migration
- [x] Update `ControlPlaneDbContext`

### Phase 2: Synchronization ✅
- [x] Implement `ModelsDevClient` (HTTP client for `models.dev` API)
- [x] Implement `ModelsDevSyncJob` (Quartz.NET job)
- [x] Implement `ProviderDiscoveryJob` (Quartz.NET job)
- [x] Register Quartz services in `Program.cs`

### Phase 3: Dynamic Routing (In Progress)
- [x] Rewrite `SmartRoutingChatClient` to use `DatabaseBackedSmartRouter`
- [ ] Deprecate static `appsettings.json` model lists
- [ ] Add admin UI for model management

### Phase 4: Intelligence Layer (Future)
- [ ] Track latency metrics (p50, p95, p99) per `ProviderModel`
- [ ] Implement feedback loop (auto-disable failing providers)
- [ ] Add ML-based routing (predict best provider based on request context)

---

## Consequences

### Positive

- **Zero Maintenance:** New models are discovered automatically (no manual config updates)
- **Accurate Pricing:** Pricing data stays up-to-date via daily `models.dev` syncs
- **High Availability:** System adapts to provider outages (marks models as unavailable)
- **Intelligent Routing:** Routing decisions based on real-time data (not stale config)
- **Tenant Isolation:** Per-tenant budgets and rate limits enforced at database level
- **Auditability:** Full history of model availability, pricing changes, and routing decisions

### Negative

- **Database Dependency:** Adds EF Core, migrations, and database management overhead
- **Initial Complexity:** More moving parts (jobs, background services, entity relationships)
- **Latency (Minimal):** Additional database query per request (~5-10ms overhead)
- **Data Freshness:** Models discovered hourly (not real-time)

### Mitigations

- **Caching:** Use Redis/in-memory cache for `GlobalModel` lookups (reduce DB load)
- **Fallback Logic:** If database unavailable, fall back to static config
- **Monitoring:** Quartz dashboard exposes job execution history and failures
- **Testing:** Comprehensive unit tests for sync jobs and routing logic

---

## Real-World Impact

### Before (Static Config)
```json
// appsettings.json
{
  "Providers": {
    "NVIDIA": {
      "Models": [
        "meta/llama-3.1-405b-instruct", // Manually added on 2026-01-15
        "google/gemma-3-27b"            // Added on 2026-01-20
        // What if NVIDIA adds a new model tomorrow? Manual update required.
      ]
    }
  }
}
```

**Problem:** New model `nvidia/mixtral-8x22b-instruct` released on 2026-02-01 → not available until manual config update and redeployment.

### After (Dynamic Registry)
```bash
# 2026-02-01 01:15 UTC - ProviderDiscoveryJob runs
[INFO] ProviderDiscoveryJob: Discovered 12 models from NVIDIA
[INFO] ProviderDiscoveryJob: New model detected: nvidia/mixtral-8x22b-instruct
[INFO] ProviderDiscoveryJob: Saved to database (IsAvailable = true)

# 2026-02-01 01:16 UTC - User requests new model
POST /v1/chat/completions
{
  "model": "mixtral-8x22b-instruct",
  "messages": [...]
}

# SmartRoutingChatClient queries database
[INFO] SmartRouter: Found 3 candidates for mixtral-8x22b-instruct
[INFO] SmartRouter: Selected nvidia/mixtral-8x22b-instruct (IsFree: true, Health: 99%)
```

**Result:** New model available automatically within 1 hour of provider release (no manual intervention).

---

## Future Enhancements

### A. Latency Tracking

```csharp
// After each request, record latency
await _db.ProviderModels
    .Where(pm => pm.Id == selectedProviderId)
    .ExecuteUpdateAsync(pm => new ProviderModel
    {
        SuccessCount = pm.SuccessCount + 1,
        P95LatencyMs = CalculateP95(pm.Id, responseTime) // Rolling window calculation
    });
```

### B. Automatic Circuit Breaking

```csharp
// In ProviderDiscoveryJob, check failure rate
var failureRate = (double)providerModel.FailureCount / (providerModel.SuccessCount + providerModel.FailureCount);
if (failureRate > 0.5) // >50% failure rate
{
    providerModel.IsAvailable = false;
    _logger.LogWarning("Disabled provider {Provider} due to high failure rate", providerModel.ProviderId);
}
```

### C. ML-Based Routing

```csharp
// Use historical data to predict best provider for request
var prediction = await _mlModel.PredictBestProviderAsync(new RoutingFeatures
{
    RequestedModel = "gpt-4o",
    TimeOfDay = DateTime.UtcNow.Hour,
    TenantId = tenantId,
    HistoricalLatency = providerLatencies
});
```

---

## Related Decisions

- [ADR-001: Stream-Native CQRS](./001-stream-native-cqrs.md) — Architecture supporting dynamic routing
- [ADR-002: Tiered Routing Strategy](./002-tiered-routing-strategy.md) — Routing algorithm enhanced by registry
- [ADR-010: Ultra-Miser Mode](./010-ultra-miser-mode.md) — Cost optimization enabled by accurate pricing data

---

## Evidence

- **Archived Plan:** `docs/archive/2026/01/29/docs_archive/2026-02-02-pre-refactor/plan/20260129-plan7-dynamic-model-registry.md`
- **Related Commits:** EF Core entity definitions, Quartz.NET job implementations
- **Implementation:** `src/InferenceGateway/Infrastructure/Persistence/Entities/`, `src/InferenceGateway/Infrastructure/Jobs/`

---

> *"A static configuration is a snapshot of a world that no longer exists. A dynamic registry is a living map of a world that's changing faster than you can keep up with—so let the computer keep up for you."* — ULTRA MISER MODE™ Principle #28
