# Synaxis Error Handling Review

This document reviews current error handling across Synaxis (backend WebApi and frontend client), documents current state, identifies gaps, and lists required tests to validate behavior. This is a documentation-only artifact — no code changes are made here.

## Summary

- Primary middleware: OpenAIErrorHandlerMiddleware (src/InferenceGateway/WebApi/Middleware/OpenAIErrorHandlerMiddleware.cs)
- Endpoints: OpenAI-compatible endpoints under src/InferenceGateway/WebApi/Endpoints (mounted in Program.cs)
- Frontend client: GatewayClient (src/Synaxis.WebApp/ClientApp/src/api/client.ts)
- Existing tests: numerous unit & integration tests exercise middleware and endpoints; API error-case integration tests exist (tests/InferenceGateway/IntegrationTests/API/ApiEndpointErrorTests.cs)

## Error handling paths review

1) Global HTTP pipeline
  - OpenAIErrorHandlerMiddleware wraps all requests (registered in Program.cs via app.UseMiddleware<OpenAIErrorHandlerMiddleware>()).
  - Behavior:
    - Catches exceptions from downstream middleware/endpoint handlers.
    - Special-case AggregateException: flattens inner exceptions, extracts provider/source, attempts to derive a status code from common properties or reflection, produces a consolidated JSON error with `error: { message, code = "upstream_routing_failure", details: [...] }` and sets status 502/400/500 depending on inner codes.
    - Non-aggregate exceptions: maps ArgumentException -> 400 (invalid_request_error/invalid_value), otherwise 500 (server_error/internal_error) and returns OpenAI-compatible single-error JSON shape.
    - If context.Response.HasStarted, rethrows (preserves in-flight response behavior).

2) Endpoint-level validation and routing
  - Endpoints do minimal request validation; many parameter validation responsibilities are delegated to routing and downstream components (ModelResolver, SmartRouter).
  - Tests show certain malformed inputs produce 500 due to JSON deserialization errors rather than 400 (ApiEndpointErrorTests findings).

3) Provider/client-level errors
  - Provider HTTP clients (IChatClient adapters) may throw HttpRequestException or provider-specific exceptions. Middleware attempts to inspect StatusCode properties and uses them to inform aggregate decisions.

4) Streaming path
  - Streaming uses SSE and GatewayClient.sendMessageStream which reads fetch Response.body and yields parsed chunks.
  - On non-OK responses the client reads text and throws Error(`HTTP ${status}: ${payload}`) — effectively surface the raw upstream error to UI code.

## Exception catching and logging status

- The middleware catches and formats errors; however, there is no explicit logger usage in OpenAIErrorHandlerMiddleware.cs (no ILogger injected or used). Tests reference ILogger in test harness but middleware itself does not log exceptions.
- For AggregateException handling, internal messages are summarized and returned in response body but not written to any server-side logs in this middleware. This reduces observability for production (no structured logs, no correlation IDs attached by middleware).
- A few tests validate that when a RequestId is present the response includes it (tests reference this behavior); middleware does not explicitly add RequestId header in the file inspected — the test suite may inject it earlier in pipeline.

Gaps / Risks:
- No structured logging inside middleware: exceptions are returned to clients but not reliably logged.
- No correlation/request-id injection or guaranteed inclusion in error responses from middleware.
- JSON deserialization and model validation errors bubble as 500 instead of 400.

## User-friendly error messages

- Current behavior tends toward returning provider-origin messages for aggregate errors (inner.Message) and raw exception.Message for single exceptions.
- For streaming client, GatewayClient throws an Error containing full response text; the UI will likely display that raw string.
- Many error messages are generic (e.g., "server_error" / "internal_error") or include provider technical details which are not user friendly.

Recommendations:
- Introduce a mapping layer: validation errors -> clear field-level messages; upstream provider errors -> sanitized user-facing message + internal details in `details` for diagnostics.
- Ensure streaming client surfaces a sanitized message (e.g., code + short message) and stores raw provider payload in developer console / logs only.

## Error codes consistency

- Middleware currently returns OpenAI-style error shape (error: { message, type?, param?, code }) for single errors and a custom `upstream_routing_failure` code for aggregated errors.
- There's partial convention but not fully enforced across the codebase — some downstream components and tests expect 400 vs 404 vs 500 inconsistently.

Recommendations:
- Define canonical error code list (example):
  - invalid_request_error / invalid_value
  - upstream_routing_failure
  - provider_error
  - rate_limit
  - authentication_error
  - internal_error
- Map status codes consistently: 400 (client validation errors), 401 (auth), 403 (forbidden), 404 (not found/missing model), 429 (rate limit), 502 (upstream provider failure / bad gateway), 503 (service unavailable), 500 (internal).

## Error scenario test requirements

Create or ensure the following tests exist (integration preferred where possible):

1) Middleware behavior
  - AggregateException with only client errors (400/404) -> overall 400 and details reflect inner messages (already tested).
  - AggregateException containing HttpRequestException with StatusCode (e.g., 502/503/429) -> overall 502 and code `upstream_routing_failure` (already tested).
  - Non-aggregate ArgumentException -> 400 with OpenAI-compatible invalid_request_error shape (already tested).
  - Generic exception -> 500 with server_error/internal_error (already tested).
  - Response.HasStarted scenario: when response has started middleware should rethrow and not attempt to write response (tests exist).

2) Endpoint validation
  - Malformed JSON body -> should return 400 (current behavior is 500) — add test for desired behavior and track as TODO to change API model binding/validation.
  - Missing required fields (model/messages) -> explicit 400 with field-level messages (tests exist for current behavior; add desired tests).
  - Invalid parameter types (stream non-boolean) -> 400 not 500 (add test + implementation task later).

3) Streaming path
  - Non-OK streaming response -> client throws Error with sanitized message; test that UI receives error code and raw body is only in developer logs.
  - Streaming partial failure mid-stream -> client yields what it can, then surfaces error; verify behavior end-to-end with SSE mock provider.

4) Provider failures and retries
  - Provider returns 429 -> retry logic invoked and eventually returns appropriate error code (test the retry backoff behavior + final error mapping).
  - Provider returns malformed response (invalid JSON) -> middleware returns 502 with `provider_error` code; test for robust handling.

5) Observability tests
  - Errors result in structured logs (log level, exception, request id); add integration test that asserts logs contain expected fields when an endpoint fails.

## Recommended action items (document-only)

Short-term (documentation/tests):
- Create an explicit error code catalog file and reference it from README.
- Add tests asserting desired API validation semantics (400 vs 500) so future changes are gated by tests.
- Add tests for streaming error paths using mocked SSE provider.

Medium-term (implementation suggestions, not performed here):
- Inject ILogger into OpenAIErrorHandlerMiddleware and log structured errors with RequestId.
- Add request correlation middleware earlier in pipeline to ensure request id present.
- Add an API validation layer (FluentValidation or ModelState checks) to convert deserialization/validation errors to 400.
- Sanitize provider messages surfaced to end-users; keep technical details in `details` blob and server logs.

## Current state matrix (short)

| Area | Current | Tests present | Priority |
|------|---------|---------------|----------|
| Global middleware | OpenAIErrorHandlerMiddleware handles AggregateException + single exceptions; no logging | Yes (integration tests) | High |
| Endpoint validation | Minimal; many validation errors bubble as 500 | Partial (ApiEndpointErrorTests document existing behavior) | High |
| Streaming client | Client parses SSE and throws raw error on non-OK | Partial tests for successful streams; error handling tests limited | Medium |
| Error codes | Partial OpenAI-style shapes present; inconsistent mapping across components | Partial | High |
| Observability/logging | Not present in middleware; tests reference ILogger but middleware doesn't log | Lacking | High |

## Appendices

References inspected during review:
- src/InferenceGateway/WebApi/Middleware/OpenAIErrorHandlerMiddleware.cs
- src/InferenceGateway/WebApi/Endpoints/
- src/Synaxis.WebApp/ClientApp/src/api/client.ts
- tests/InferenceGateway/IntegrationTests/API/ApiEndpointErrorTests.cs
- .sisyphus/notepads/synaxis-enterprise-stabilization/learnings.md (recent session notes)

---
Generated by Sisyphus-Junior review on 2026-02-01
