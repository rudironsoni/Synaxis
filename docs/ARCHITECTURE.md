# Architecture

Synaxis follows the **Clean Architecture** principles to ensure separation of concerns, testability, and independence from frameworks.

## Project Structure

The solution is divided into three main layers:

### 1. `Synaxis.Api` (Presentation)
*   **Role:** The entry point of the application.
*   **Responsibilities:**
    *   Hosting the ASP.NET Core Web API.
    *   Handling HTTP requests and responses.
    *   `appsettings.json` configuration binding.
    *   Dependency Injection (DI) wiring via `ApplicationExtensions`.
*   **Key Files:** `Program.cs`, `appsettings.json`.

### 2. `Synaxis.Application` (Core)
*   **Role:** The "Brain" of the system. Contains business logic and interfaces.
*   **Responsibilities:**
    *   **Routing Logic:** Decides which provider handles a request.
    *   **Interfaces:** Defines `IProviderRegistry` and abstract `IChatClient` usage.
    *   **Configuration Models:** `SynaxisConfiguration` class.
*   **Key Component:** `TieredRoutingChatClient`
    *   This is the core intelligence. It intercepts requests, looks up the requested `model`, and executes the failover strategy.
*   **Pipeline:**
    *   Requests pass through `UsageTrackingChatClient` -> `FunctionInvocation` -> `TieredRoutingChatClient`.

### 3. `Synaxis.Infrastructure` (Implementation)
*   **Role:** External concerns and concrete implementations.
*   **Responsibilities:**
    *   **Provider Implementations:** Concrete classes for talking to specific APIs (Groq, Cohere, Cloudflare, etc.).
    *   **Extensions:** Helper methods (`AddOpenAiCompatibleClient`, `AddOpenRouterClient`, etc.) to register clients into the DI container.
*   **Key Classes:** `CloudflareChatClient`, `GenericOpenAiChatClient`, `CohereChatClient`.

---

## Request Flow

1.  **Client Request:** A client sends a standard OpenAI format request (e.g., POST `/chat/completions`) to the `Synaxis.Api`.
2.  **Controller:** The API controller receives the request and delegates it to the `IChatClient`.
3.  **Pipeline Execution:**
    *   **Usage Tracking:** Logs request metrics.
    *   **Function Invocation:** Handles tool calls if requested.
    *   **Routing (The Brain):** The `TieredRoutingChatClient` takes over.
4.  **The Brain (`TieredRoutingChatClient`):**
    *   **Lookup:** It queries the `IProviderRegistry` for providers that support the requested `model`.
    *   **Grouping:** Candidates are grouped by their configured `Tier` (1, 2, 3...).
    *   **Execution:**
        *   It starts with **Tier 1**.
        *   It **shuffles** the providers in that tier (Load Balancing).
        *   It attempts to call the first provider.
5.  **Infrastructure:** The specific `ChatClient` (e.g., `Groq`) translates the request (if necessary) and calls the external LLM API.
6.  **Response/Failover:**
    *   **Success:** The response is returned up the stack to the user.
    *   **Failure:** The `TieredRoutingChatClient` catches the exception, logs a warning, and immediately tries the next provider in the current tier, or moves to the next tier.
7.  **Terminal Failure:** If all configured providers for all tiers fail, an `AggregateException` is thrown.
