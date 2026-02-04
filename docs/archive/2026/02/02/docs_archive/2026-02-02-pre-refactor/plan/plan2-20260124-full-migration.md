# Synaxis 2.0: Migration Master Plan (The "Migration")

**Goal**: Port **ALL** active legacy providers (`Cohere`, `NVIDIA`, `OpenRouter`, `Pollinations`, `Cloudflare`) and logic (`TieredProviderRouter`) to the new Microsoft Agent Framework architecture. Strictly switch to a **Config-Driven** approach (No DB, No gRPC, No Social/EventBus).

## 1. Architecture: The Connector Ecosystem

We will expand `Synaxis.Connectors` to support a rich ecosystem of providers.

### A. The "Generic" Connector (OpenAI Compatible)
Many providers just mimic OpenAI. We will create a single, robust `GenericOpenAiChatClient` to handle them all.
*   **Target Providers**: `NVIDIA`, `OpenRouter`, `HuggingFace` (TGI).
*   **Implementation**: Wraps `Microsoft.Extensions.AI.OpenAI` or uses a custom `HttpClient` implementation if strict control over headers (like `HTTP-Referer` for OpenRouter) is needed.

### B. The "Custom" Connectors
Providers with unique APIs need dedicated clients implementing `IChatClient`.
*   **`CohereChatClient`**:
    *   Endpoint: `https://api.cohere.com/v2/chat`
    *   Logic: Maps `ChatRole` to Cohere's JSON format.
*   **`PollinationsChatClient`**:
    *   Endpoint: `https://text.pollinations.ai/`
    *   Logic: Collapses conversation history into a single prompt string (GET request).
*   **`CloudflareChatClient`**:
    *   Endpoint: `https://api.cloudflare.com/client/v4/accounts/{id}/ai/run/{model}`
    *   Logic: Handles Cloudflare's specific REST payload structure.

## 2. Architecture: The Brain (Advanced Routing)

We will upgrade `Synaxis.Brain` to support the "Ultra-Miser" Tiered Logic using strictly **configuration** (appsettings.json), replacing the old Database approach.

### A. Configuration (`Synaxis.Brain.Configuration`)
*   Port `ProvidersOptions` -> `SynaxisConfiguration`.
*   Structure:
    ```json
    {
      "Providers": {
        "Groq": { "Key": "...", "Tier": 1, "Models": ["llama3*"] },
        "Gemini": { "Key": "...", "Tier": 1, "Models": ["gemini*"] },
        "Cohere": { "Key": "...", "Tier": 2, "Models": ["command-r*"] }
      }
    }
    ```

### B. The Smart Router (`TieredRoutingChatClient`)
*   **Logic**:
    1.  **Identify Candidates**: Find all providers that support the requested `model`.
    2.  **Group by Tier**: Sort by `Tier` (1 = Free/Fast, 2 = Paid).
    3.  **Shuffle**: Randomize within the same tier (Load Balancing).
    4.  **Execute & Failover**: Try the first. If it fails, try the next.
    5.  **Aggregate Exception**: If all fail, throw.

## 3. Execution Steps

### Phase 1: The Generic Connector
1.  Implement `GenericOpenAiChatClient` in `Synaxis.Connectors`.
2.  Add specific extensions: `AddOpenRouter()`, `AddNvidia()`.
    *   *Note*: Ensure OpenRouter headers (`HTTP-Referer`) are injected.

### Phase 2: The Custom Connectors
3.  Implement `CohereChatClient`.
4.  Implement `PollinationsChatClient`.
5.  Implement `CloudflareChatClient`.

### Phase 3: The Brain Transplant
6.  Create `ProviderRegistry`: Service holding `IChatClient` instances + metadata (from Config).
7.  Implement `TieredRoutingChatClient`.
8.  Update `Gateway/Program.cs` to wire everything up.

## 4. Integration Test Architecture

**Goal**: Implement a production-grade Integration Test suite using `WebApplicationFactory`.

### A. Project Structure
*   **Project**: `tests/Synaxis.IntegrationTests`
*   **Dependencies**:
    *   `Microsoft.AspNetCore.Mvc.Testing`
    *   `MartinCostello.Logging.XUnit`
    *   `Microsoft.Extensions.AI` (For strong-typed response validation)
    *   `Synaxis.Gateway`

### B. Infrastructure: SynaxisWebApplicationFactory
*   **Log Capture**: Configures `logging.AddXUnit()`.
*   **Configuration**: Explicitly loads Environment Variables.

### C. The Test Suite: LiveProviderValidationTests
*   **Strict Safety**: No `[SkippableFact]`. Tests fail if API keys are missing.
*   **Flow**: Arrange (Factory/Client) -> Act (POST /v1/chat/completions) -> Assert (200 OK).

### D. Planned Test Cases
*   `Validate_Groq_Connectivity`
*   `Validate_Gemini_Connectivity`
*   `Validate_Cohere_Connectivity`
*   `Validate_Pollinations_Connectivity`
*   `Validate_Cloudflare_Connectivity`

## 5. Scope Reduction & Cleanup (The "Purge")
We are permanently removing legacy complexity to focus on the Gateway.
*   **Remove gRPC**: Delete `Synaxis.API` / `LlmGrpcService`.
*   **Remove Social**: Delete `Synaxis.Contracts` (Twitter protos, EventBus).
*   **Remove Persistence**: Delete `Synaxis.Domain` & `Synaxis.Infrastructure` (DB Entities/Repos).
*   **Remove DeepInfra**: Not migrating.
*   **Remove Legacy Tests**: Delete `tests/Synaxis.Gateway.Tests/*Matrix*`.

## 6. Timeline
*   **Step 1**: Generic Client & OpenRouter/NVIDIA.
*   **Step 2**: Custom Clients (Cohere, Pollinations, Cloudflare).
*   **Step 3**: Advanced Routing Logic & Registry.
*   **Step 4**: Cleanup (Delete Legacy Projects).
*   **Step 5**: Full System Test (Integration Suite).
