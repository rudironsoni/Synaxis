# Learnings - WebAPI Curl Test Scripts

## Task: Generate curl test scripts for WebAPI (Task 9.1)

### Date: 2025-02-01

---

## Key Findings

### Endpoint Structure
- All OpenAI-compatible endpoints are under `/openai/v1/` prefix
- Admin endpoints require JWT authentication via `Authorization: Bearer <token>` header
- Streaming responses use Server-Sent Events (SSE) format with `data: {json}\n\n` frames
- Health check endpoints are at `/health/liveness` and `/health/readiness`

### Authentication Flow
1. Use `POST /auth/dev-login` with email to get JWT token
2. Token is returned in JSON response: `{"token": "..."}`
3. Include token in subsequent requests: `Authorization: Bearer <token>`
4. JWT secret is configured in `appsettings.json` under `Synaxis:InferenceGateway:JwtSecret`

### Request/Response Patterns

#### Chat Completions
- Non-streaming: Standard JSON response with `choices` array
- Streaming: SSE format with `data:` prefixed JSON chunks, ending with `data: [DONE]`
- Request schema: `model`, `messages[]`, `stream`, `max_tokens`, `temperature`, etc.

#### Legacy Completions
- Similar to chat but uses `prompt` instead of `messages`
- Marked as deprecated in OpenAPI spec
- Still supports streaming via SSE

#### Models Endpoint
- `GET /openai/v1/models` returns list of all available models
- `GET /openai/v1/models/{id}` returns specific model or 404
- Response includes provider information and model capabilities

#### Admin Endpoints
- All require authentication
- `GET /admin/providers` - List all providers with status
- `PUT /admin/providers/{providerId}` - Update provider configuration
- `GET /admin/health` - Detailed health status including providers

### Error Handling
- Invalid JSON returns 400
- Missing authentication returns 401
- Invalid model ID returns 404
- Missing required fields returns 400

### Script Design Decisions

#### Modular Test Functions
- Each endpoint category has its own test function
- Functions are named descriptively: `test_chat_completions`, `test_models`, etc.
- Makes it easy to run specific test suites in isolation

#### Helper Functions
- `make_request`: Generic curl wrapper with status code checking
- `print_test`, `print_pass`, `print_fail`: Colored output functions
- `setup_authentication`: Handles JWT token retrieval

#### Configuration
- API base URL configurable via `-u` flag or `API_BASE_URL` env var
- Test email configurable via `-e` flag or `TEST_EMAIL` env var
- Verbose mode via `-v` flag for debugging

#### Exit Codes
- Returns 0 if all tests pass
- Returns 1 if any test fails
- Individual test failures don't stop execution (uses `|| true` for expected failures)

### Testing Strategy

#### Happy Path Tests
- Valid requests with proper parameters
- Both streaming and non-streaming variants
- Authentication with valid JWT token

#### Error Scenario Tests
- Invalid JSON payload
- Missing required fields
- Invalid model IDs
- Missing authentication
- Non-existent endpoints

#### Expected vs Unexpected Failures
- Some tests use `|| true` to indicate expected failures (e.g., invalid model ID)
- Other tests expect specific HTTP status codes (400, 401, 404)
- This distinction is important for test result interpretation

### Dependencies
- `curl`: Required for making HTTP requests
- `jq`: Optional but recommended for JSON parsing
- Script falls back to `grep`/`sed` if `jq` is not available

### Best Practices Applied

1. **Self-documenting code**: Function names clearly indicate what they test
2. **Modular design**: Easy to add new test cases or modify existing ones
3. **Flexible configuration**: Works with different API URLs and environments
4. **Clear output**: Color-coded results make it easy to spot failures
5. **Comprehensive coverage**: Tests all documented endpoints plus error scenarios
6. **Exit code semantics**: Follows Unix convention (0 = success, non-zero = failure)

### Potential Improvements

1. Add support for running specific test suites via command-line flags
2. Add JSON output format for CI/CD integration
3. Add timeout configuration for slow endpoints
4. Add retry logic for flaky network conditions
5. Add support for custom request payloads from files
6. Add performance metrics (response time tracking)

### Integration Notes

- Script assumes API is running before execution
- Checks `/health/liveness` endpoint first to verify API availability
- Can be integrated into CI/CD pipelines
- Environment variables make it easy to configure for different environments

---

## Files Created

- `.sisyphus/scripts/webapi-curl-tests.sh` - Comprehensive test script (523 lines)

## Files Referenced

- `.sisyphus/webapi-endpoints.md` - Endpoint documentation
- `src/InferenceGateway/WebApi/Program.cs` - Route mapping and auth configuration
- `src/InferenceGateway/WebApi/Controllers/AuthController.cs` - Auth endpoint implementation
