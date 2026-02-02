## WebAPI Endpoints

This document enumerates all WebAPI routes implemented under src/InferenceGateway/WebApi/, including minimal APIs and MVC controllers. For each route I list: method, path, auth requirements, streaming support, request schema (source), response schema (source), and notes.

---

### POST /openai/v1/chat/completions
- Method: POST
- Path: /openai/v1/chat/completions
- Auth: none (no RequireAuthorization)
- Streaming: YES (SSE, Content-Type: text/event-stream) when request.stream == true
- Request schema: OpenAIRequest (src/InferenceGateway/Application/Translation/OpenAIRequest.cs) — fields: model, messages[], tools, tool_choice, response_format, stream (bool), temperature, top_p, max_tokens, stop
- Non-streaming Response schema: ChatCompletionResponse (src/InferenceGateway/WebApi/DTOs/OpenAi/ChatDtos.cs)
- Streaming Response schema: ChatCompletionChunk (src/InferenceGateway/WebApi/DTOs/OpenAi/ChatDtos.cs) emitted as SSE `data: {json}\n\n` frames followed by `data: [DONE]`.
- Notes: Implementation lives in src/InferenceGateway/WebApi/Endpoints/OpenAI/OpenAIEndpointsExtensions.cs. Requests are parsed by OpenAIRequestParser.ParseAsync and mapped to internal ChatCommand / ChatStreamCommand. Response includes x-gateway-model-* headers via OpenAIMetadataMiddleware.

### POST /openai/v1/responses
- Method: POST
- Path: /openai/v1/responses
- Auth: none
- Streaming: YES (SSE) when request.stream == true
- Request schema: OpenAIRequest (same as /chat/completions)
- Non-streaming Response schema: ResponseCompletion (src/InferenceGateway/WebApi/Endpoints/OpenAI/OpenAIEndpointsExtensions.cs ResponseCompletion/ResponseOutput/ResponseContent)
- Streaming Response schema: ResponseStreamChunk / ResponseDelta (same file) emitted as SSE `data: {json}` frames and `data: [DONE]`.
- Notes: OpenAI-compatible 'responses' endpoint. Implementation also in OpenAIEndpointsExtensions.cs.

### POST /openai/v1/completions (Legacy)
- Method: POST
- Path: /openai/v1/completions
- Auth: none
- Streaming: YES supported (legacy streaming format) when request.stream == true
- Request schema: CompletionRequest (src/InferenceGateway/WebApi/DTOs/CompletionRequest.cs) — fields: model, prompt (string|array), max_tokens, temperature, stream, user, echo, stop, best_of
- Response schema (non-stream): legacy text_completion JSON object (anonymous object returned in LegacyCompletionsEndpoint.cs). Streaming emits text_completion chunks as SSE with `data: {json}` frames and `data: [DONE]`.
- Notes: Marked Deprecated in OpenAPI via operation.Deprecated = true. Implementation: src/InferenceGateway/WebApi/Endpoints/OpenAI/LegacyCompletionsEndpoint.cs.

### GET /openai/v1/models
- Method: GET
- Path: /openai/v1/models
- Auth: none
- Streaming: NO
- Response schema: ModelsListResponseDto (src/InferenceGateway/WebApi/Endpoints/OpenAI/ModelsEndpoint.cs) — contains list of ModelDto and Providers summary
- Notes: Returns canonical models and aliases from configuration.

### GET /openai/v1/models/{**id}
- Method: GET
- Path: /openai/v1/models/{**id}
- Auth: none
- Streaming: NO
- Response schema: ModelDto (src/InferenceGateway/WebApi/Endpoints/OpenAI/ModelsEndpoint.cs) or 404 error payload

### Identity endpoints (minimal APIs)

- POST /api/identity/{provider}/start
  - Method: POST
  - Path: /api/identity/{provider}/start
  - Auth: none
  - Streaming: NO
  - Request: None (route param: provider)
  - Response: result from IdentityManager.StartAuth (implementation in src/InferenceGateway/WebApi/Endpoints/Identity/IdentityEndpoints.cs)

- POST /api/identity/{provider}/complete
  - Method: POST
  - Path: /api/identity/{provider}/complete
  - Auth: none
  - Streaming: NO
  - Request: CompleteRequest { code, state } (src/InferenceGateway/WebApi/Endpoints/Identity/IdentityEndpoints.cs)
  - Response: IdentityManager.CompleteAuth result

- GET /api/identity/accounts
  - Method: GET
  - Path: /api/identity/accounts
  - Auth: none
  - Streaming: NO
  - Response: list of stored accounts (masked) via ISecureTokenStore.LoadAsync

### Antigravity / OAuth helper endpoints

- GET /oauth/antigravity/callback
  - Method: GET
  - Path: /oauth/antigravity/callback
  - Auth: none
  - Streaming: NO
  - Response: HTML page (success/failure message) — implementation returns small HTML snippet

- GET /antigravity/accounts
  - Method: GET
  - Path: /antigravity/accounts
  - Auth: none
  - Streaming: NO
  - Response: list of Antigravity accounts (IAntigravityAuthManager.ListAccounts)

- POST /antigravity/auth/start
  - Method: POST
  - Path: /antigravity/auth/start
  - Auth: none
  - Streaming: NO
  - Request: StartAuthRequest { redirectUrl? }
  - Response: { AuthUrl, RedirectUrl, Instructions }

- POST /antigravity/auth/complete
  - Method: POST
  - Path: /antigravity/auth/complete
  - Auth: none
  - Streaming: NO
  - Request: CompleteAuthRequest { code?, state?, redirectUrl?, callbackUrl? }
  - Response: success message or 400 with error

### Admin endpoints (Require authentication)

All endpoints in /admin group are protected by RequireAuthorization(policy => policy.RequireAuthenticatedUser()) in src/InferenceGateway/WebApi/Endpoints/Admin/AdminEndpoints.cs. Authentication is configured via JWT Bearer in Program.cs.

- GET /admin/providers
  - Method: GET
  - Path: /admin/providers
  - Auth: REQUIRED (JWT) — user must be authenticated
  - Streaming: NO
  - Response: ProviderAdminDto[] (provider details, models and status)

- PUT /admin/providers/{providerId}
  - Method: PUT
  - Path: /admin/providers/{providerId}
  - Auth: REQUIRED (JWT)
  - Streaming: NO
  - Request: ProviderUpdateRequest { enabled?, key?, endpoint?, tier? }
  - Response: { success: true, message }

- GET /admin/health
  - Method: GET
  - Path: /admin/health
  - Auth: REQUIRED (JWT)
  - Streaming: NO
  - Response: HealthDataDto (services[], providers[], overall status, timestamp)

### Health check endpoints (mapped in Program.cs)

- GET /health/liveness
  - Method: GET
  - Path: /health/liveness
  - Auth: none
  - Streaming: NO
  - Notes: health check mapped with Predicate = r => r.Tags.Contains("liveness")

- GET /health/readiness
  - Method: GET
  - Path: /health/readiness
  - Auth: none
  - Streaming: NO
  - Notes: readiness includes DB and Redis checks (tags 'readiness')

### Auth & API key controllers (MVC controllers - require MapControllers())

- POST /auth/dev-login
  - Method: POST
  - Path: /auth/dev-login
  - Auth: none
  - Streaming: NO
  - Request: DevLoginRequest { email }
  - Response: { token }
  - Notes: Development-only convenience; creates tenant/user if not present.

- POST /projects/{projectId}/keys
  - Method: POST
  - Path: /projects/{projectId}/keys
  - Auth: REQUIRED ([Authorize] on controller)
  - Streaming: NO
  - Request: CreateKeyRequest { name }
  - Response: { Id, Key, Name }

- DELETE /projects/{projectId}/keys/{keyId}
  - Method: DELETE
  - Path: /projects/{projectId}/keys/{keyId}
  - Auth: REQUIRED
  - Streaming: NO
  - Response: 204 No Content

---

Files referenced (primary sources):
- src/InferenceGateway/WebApi/Endpoints/OpenAI/OpenAIEndpointsExtensions.cs (chat, responses, legacy, models mapping + DTOs)
- src/InferenceGateway/WebApi/Endpoints/OpenAI/LegacyCompletionsEndpoint.cs
- src/InferenceGateway/WebApi/Endpoints/OpenAI/ModelsEndpoint.cs
- src/InferenceGateway/WebApi/Endpoints/Admin/AdminEndpoints.cs
- src/InferenceGateway/WebApi/Endpoints/Identity/IdentityEndpoints.cs
- src/InferenceGateway/WebApi/Endpoints/Antigravity/AntigravityEndpoints.cs
- src/InferenceGateway/WebApi/Controllers/AuthController.cs
- src/InferenceGateway/WebApi/Controllers/ApiKeysController.cs
- src/InferenceGateway/Application/Translation/OpenAIRequest.cs (request schema)
- src/InferenceGateway/WebApi/DTOs/OpenAi/ChatDtos.cs (response DTOs)

---

Compatibility note (WebApp):
- The WebApp client (src/Synaxis.WebApp/ClientApp) expects endpoints at the root OpenAI-compatible path `/v1/*` (search: src/Synaxis.WebApp/ClientApp/src/api/client.ts uses base `/v1` and posts to `/chat/completions`). The gateway currently maps OpenAI endpoints under the `/openai` prefix (e.g. `/openai/v1/chat/completions`). This causes a mismatch: the WebApp's client requests to `/v1/chat/completions` will NOT hit `/openai/v1/chat/completions` unless a proxy or path rewrite exists.

- WebApp calls confirmed via simple grep: `/chat/completions`, `/admin/providers`, `/admin/health`.
- Admin endpoints used by WebApp (e.g. `/admin/providers`, `/admin/health`, `/admin/providers/{id}`) exist and require JWT.

Actionable observation (no changes applied): ensure either:
- WebApp baseURL is updated to include `/openai` prefix (e.g. `/openai/v1`) OR
- Gateway exposes duplicate root routes at `/v1/*` (OpenAI-compatible top-level) to preserve client expectations.

---

If you want, I can produce a machine-readable list (YAML/JSON) of all endpoints with exact DTO properties next — this file currently focuses on human-readable summary and references to source files.
