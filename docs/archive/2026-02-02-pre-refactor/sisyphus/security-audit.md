# Synaxis Security Audit

Generated: 2026-02-01

Purpose: document current security posture for Synaxis (inference gateway + webapp), identify gaps, and list required tests.

IMPORTANT: This is a documentation-only deliverable. No code changes made.

---

1. Executive summary
--------------------
- Project: Synaxis (Inference Gateway + WebApp)
- Scope: API server (src/InferenceGateway/WebApi) and frontend client (src/Synaxis.WebApp/ClientApp)
- High-level risk areas identified: weak request validation, default JWT secret in Program.cs fallback, minimal rate-limiting implementation, potential XSS on client inputs, minimal CORS configuration visibility, SQL injection risks mitigated by EF Core but review required, streaming endpoints exposing SSE parsing pitfalls.

2. Assets reviewed
------------------
- src/InferenceGateway/WebApi/Program.cs
- src/InferenceGateway/WebApi/Endpoints/OpenAI/OpenAIEndpointsExtensions.cs
- src/InferenceGateway/WebApi/Endpoints/OpenAI/LegacyCompletionsEndpoint.cs
- src/InferenceGateway/WebApi/Controllers/AuthController.cs
- src/InferenceGateway/Infrastructure/Security/JwtService.cs
- src/InferenceGateway/Infrastructure/Routing/RedisQuotaTracker.cs
- src/Synaxis.WebApp/ClientApp/src/api/client.ts

3. Findings by area
-------------------

3.1 Input validation review
- Current state:
  - Request parsing for OpenAI endpoints uses OpenAIRequestParser.ParseAsync. Some endpoints perform ad-hoc validation (e.g., LegacyCompletions.TryParsePrompt) while many parameters (temperature, max_tokens, messages) are not strictly validated at API boundary.
  - Integration tests indicate many malformed inputs produce 500 (deserialization) or are accepted with defaults instead of returning 400.
  - DevLogin endpoint accepts any email and auto-registers a user (dev-only) without rate limiting or email verification.

Risks:
  - Malformed JSON causing 500 may leak internal error information.
  - Lack of strict validation allows unexpected types and values to flow into business logic and downstream providers.

Recommendations:
  - Introduce model binding validation for API DTOs (DataAnnotations or FluentValidation) and return 400 for invalid payloads.
  - Normalize and validate numeric ranges (temperature, max_tokens) and boolean types (stream).
  - Ensure TryParsePrompt/ParseAsync returns precise field-level error messages.

3.2 Rate limiting status
- Current state:
  - Quota/rate-limiting concept exists (ProviderModel has RateLimitRPM/TPM; RedisQuotaTracker class implemented), but RedisQuotaTracker.CheckQuotaAsync currently returns true unconditionally (placeholder).
  - SmartRouter/clients call quota checks, but enforcement is effectively inert until CheckQuotaAsync is implemented.

Risks:
  - Without enforcement, abusive clients can exhaust provider quotas or cause high backend load.

Recommendations:
  - Implement provider- and tenant-level rate limiting using Redis counters and Lua scripts for atomicity. Support RPM and TPM configs.
  - Add a global request throttling middleware for unauthenticated routes and suspicious patterns.
  - Instrument quota counters and alerting when thresholds are approached.

3.3 JWT token expiration and validation
- Current state:
  - JwtService.GenerateToken issues tokens with Expires = DateTime.UtcNow.AddDays(7).
  - Program.cs configures JwtBearer with TokenValidationParameters: ValidateIssuerSigningKey = true, ValidateIssuer = false, ValidateAudience = false. RequireHttpsMetadata = false (in Dev?).
  - Jwt secret fallback defined in Program.cs: a long default string is provided if configuration missing -> this is dangerous if used in prod.

Risks:
  - 7-day token lifetime may be long for sensitive actions (consider refresh tokens / shorter lifetimes).
  - Disabled issuer/audience validation reduces guarantee the token was intended for this service.
  - Default secret fallback can lead to predictable signing key in misconfigured deployments.

Recommendations:
  - Enforce presence of a strong JwtSecret at startup (fail fast), or log explicit warning and refuse to run in production if default used.
  - Shorten token lifetime and add refresh token flow for admin operations.
  - Enable ValidateIssuer and ValidateAudience and require configured values in production.
  - Set RequireHttpsMetadata = true for production.

3.4 CORS configuration
- Current state:
  - Program.cs does not explicitly show AddCors usage; I could not find explicit CORS policy registration in scanned Program.cs.
  - The WebApp client uses fetch/axios to /v1 endpoints; if hosted under a different origin, missing CORS policy could block requests or allow overly permissive defaults.

Risks:
  - If no CORS policy = default denies cross-origin calls; if later added permissively (AllowAnyOrigin), risk of cross-origin exploitation.

Recommendations:
  - Add explicit CORS policies differentiating webapp origins vs public APIs. Use AllowCredentials=false for public endpoints, restrict allowed origins for admin UI and authenticated routes.
  - Document required allowed origins for deployments.

3.5 XSS vulnerabilities (frontend)
- Current state:
  - Client sanitization: React renders message content from server. Tests include checks for special characters and XSS-like strings in stores, but no explicit sanitization in client.ts for message content before rendering.
  - Message content originates from models/providers; untrusted data may contain HTML or script-like content.

Risks:
  - Server-supplied content rendered without sanitization could enable stored XSS in admin pages or chat history.

Recommendations:
  - Ensure all render paths treat model output as plain text (do not dangerouslySetInnerHTML). Use text-only rendering or sanitize with a whitelist library when rich content is required.
  - Add automated UI tests injecting HTML-like strings and verifying they render as escaped text.

3.6 SQL injection and database access
- Current state:
  - Backend uses EF Core (ControlPlaneDbContext) for DB access; queries primarily use LINQ and parameterized commands.
  - DevLogin auto-registers users by directly adding entities; no raw SQL observed in scanned files.

Risks:
  - Direct use of FromSqlRaw or string concatenation in other parts (not found in reviewed files) could introduce injection risk.

Recommendations:
  - Audit for any direct SQL usage (FromSqlRaw, ExecuteSqlRaw) across repository.
  - Ensure all raw SQL uses parameterized queries or interpolated form with parameters (EF Core supports parameterization).

3.7 Security middleware, headers, and transport
- Current state:
  - app.UseHttpsRedirection() is enabled.
  - No explicit security headers middleware (HSTS, CSP, X-Content-Type-Options, X-Frame-Options) found in Program.cs.

Risks:
  - Missing security headers increase risk surface for clickjacking, content sniffing, and mixed-content issues.

Recommendations:
  - Add security headers (HSTS in production, CSP with conservative defaults, X-Frame-Options: DENY, Referrer-Policy).
  - Enable RequireHttpsMetadata for JwtBearer in production.

4. Required security tests
------------------------
- API validation tests:
  - Malformed JSON -> assert 400 (not 500)
  - Missing required fields (messages/model) -> 400
  - Invalid field types (stream as string) -> 400
  - Numeric range checks (temperature, max_tokens) -> 400

- Auth tests:
  - JWT token validation: expired token -> 401
  - Token signed with wrong key -> 401
  - Missing issuer/audience -> depending on config -> 401

- Rate limit tests:
  - Provider-level TPM/RPM enforced: send > limit and expect 429
  - Tenant-level rate limiting enforced: shared quota per tenant -> 429

- XSS tests (frontend):
  - Message containing <script> tags is rendered escaped
  - Admin UI fields (provider names, config values) render escaped

- SQL injection tests:
  - Input containing SQL meta-characters does not cause unexpected DB behavior or errors

- CORS tests:
  - Allowed origin passes CORS preflight and requests
  - Disallowed origin preflight denied

5. Actionable next steps (implementation NOT included in this doc)
---------------------------------------------------------------
1. Replace default JwtSecret fallback: require configuration or fail fast in production.
2. Implement and enable RedisQuotaTracker.CheckQuotaAsync to enforce RateLimitRPM/TPM.
3. Add validation layer (FluentValidation or DataAnnotations + automatic 400 responses) for OpenAI request DTOs and Identity endpoints.
4. Harden CORS policy: add named policies for WebApp and public clients; avoid AllowAnyOrigin for authenticated endpoints.
5. Add security headers middleware and enable HSTS in production.
6. Add automated security tests described in Section 4 and integrate into CI.

6. Appendix — Quick evidence & code pointers
-------------------------------------------
- Program.cs: JwtBearer configured with ValidateIssuer=false, ValidateAudience=false; default JwtSecret fallback present.
- JwtService.cs: token expiry = 7 days, claims include role and tenantId.
- RedisQuotaTracker.cs: CheckQuotaAsync returns true (placeholder).
- LegacyCompletionsEndpoint.TryParsePrompt: custom prompt parsing accepts arrays/strings; lacks strict type enforcement for other request fields.
- Client API (client.ts): uses fetch for SSE and axios for JSON endpoints; Authorization header is set when token provided.

---

This file created by Sisyphus audit assistant. Append-only note will be written to .sisyphus/notepads/ as part of the task workflow.
