# Plan: Dynamic Model Registry & Intelligence Core

**Date:** 2026-01-29
**Status:** In Progress

## 1. Objective
Transform Synaxis from a static proxy with hardcoded configuration into an intelligent, database-driven model router.
Key goals:
*   **Dynamic Discovery:** Automatically find available models from providers.
*   **Rich Metadata:** Sync capability, pricing, and limit data from `models.dev`.
*   **Smart Routing:** Route requests based on real-time availability, cost, and tenant quotas.
*   **Resilience:** Remove single points of failure (static config files).

## 2. Architecture

### A. Database Schema (ControlPlane)
Three new entities in `ControlPlaneDbContext`:

1.  **`GlobalModel`** (The "Golden Record")
    *   Source: `models.dev` API
    *   Fields: `Id` (e.g., `gemma-3-27b`), `Name`, `ContextWindow`, `InputPrice`, `OutputPrice`, `Capabilities` (Tools, Vision, etc.).
    *   Purpose: Canonical definition of what a model *is*.

2.  **`ProviderModel`** (The Implementation)
    *   Source: `/v1/models` from providers (NVIDIA, HuggingFace, etc.)
    *   Fields: `ProviderId`, `ProviderSpecificId` (e.g., `nvidia/google/gemma-3-27b`), `IsAvailable`, `RateLimitRPM`.
    *   Purpose: Defines *where* a model can be executed.

3.  **`TenantModelLimit`** (The Guardrails)
    *   Source: Admin configuration
    *   Fields: `TenantId`, `AllowedRPM`, `MonthlyBudget`.
    *   Purpose: Enforcement of usage limits per tenant/model.

### B. Infrastructure (Quartz.NET)
Background jobs to keep the system synchronized.

1.  **`ModelsDevSyncJob` (Daily)**
    *   Fetches `https://models.dev/api.json`.
    *   Upserts `GlobalModel` records.
    *   Ensures pricing and capabilities are up-to-date.

2.  **`ProviderDiscoveryJob` (Hourly)**
    *   Iterates through all enabled providers.
    *   Calls their `/v1/models` endpoint (OpenAI compatible).
    *   Upserts `ProviderModel` records.
    *   Sets `IsAvailable` status based on connectivity.

### C. Smart Routing Algorithm
A rewrite of `SmartRoutingChatClient` to use a dynamic scoring engine:

1.  **Lookup:** Find all `ProviderModel` entries for the requested `GlobalModelId`.
2.  **Filter:** Exclude unavailable providers, those hitting rate limits, or those exceeding tenant budgets.
3.  **Rank:** Sort by Cost (primary) and Performance/Tier (secondary).
4.  **Execute:** Route to the top candidate with transparent failover.

## 3. Implementation Plan

### Phase 1: Foundation (Current Status: Done)
*   [x] Create Database Entities (`GlobalModel`, `ProviderModel`, `TenantModelLimit`)
*   [x] Update `ControlPlaneDbContext`

### Phase 2: Synchronization (Current Status: In Progress)
*   [x] Implement `ModelsDevClient` and DTOs
*   [x] Implement `ModelsDevSyncJob` logic
*   [ ] Add Quartz dependencies to Infrastructure project
*   [ ] Register Quartz services and jobs in `Program.cs`
*   [ ] Create EF Migration and Update Database

### Phase 3: Dynamic Routing (Next)
*   [ ] Implement `OpenAiModelDiscoveryClient` (generic `/v1/models` fetcher)
*   [ ] Implement `ProviderDiscoveryJob`
*   [ ] Rewrite `SmartRoutingChatClient` to use `ControlPlaneDbContext`

## 4. Future Enhancements
*   **Latency Tracking:** Store historical p95 latency in `ProviderModel` for performance-based routing.
*   **Feedback Loop:** Automatically disable providers that return excessive 5xx errors.
