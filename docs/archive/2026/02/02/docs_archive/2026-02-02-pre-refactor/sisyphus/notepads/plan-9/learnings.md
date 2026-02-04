
## Task 9.2: WebApp Curl Test Scripts - Completed 2026-02-01

### What Was Done
Created comprehensive curl test script for WebApp at `.sisyphus/scripts/webapp-curl-tests.sh`.

### Script Structure
The script tests the following areas:

1. **WebApp Pages** (test_webapp_pages):
   - GET / - App shell (index.html)
   - GET /chat - Chat page (SPA route)
   - GET /admin - Admin shell (SPA route)
   - GET /admin/providers - Provider config (SPA route)
   - GET /admin/health - Health dashboard (SPA route)
   - GET /admin/login - Login page (SPA route)

2. **Static Assets** (test_static_assets):
   - JavaScript bundle (/assets/index-*.js)
   - CSS bundle (/assets/index-*.css)
   - Favicon/image (/vite.svg)

3. **API Proxy** (test_api_proxy):
   - GET /v1/models - API proxy to WebAPI
   - POST /v1/chat/completions - Chat through proxy

4. **Admin API via Proxy** (test_admin_api_via_proxy):
   - GET /admin/providers - Admin providers via proxy (with auth)
   - GET /admin/health - Admin health via proxy (with auth)
   - GET /admin/providers - Without auth (should return 401)

5. **Authentication Flows** (test_authentication_flows):
   - GET /admin/providers - With valid JWT token
   - GET /admin/providers - With invalid JWT token
   - GET /admin/providers - Without JWT token

6. **Error Scenarios** (test_error_scenarios):
   - GET /nonexistent-page - SPA fallback to index.html
   - GET /assets/nonexistent.js - Non-existent static asset (404)
   - GET /v1/nonexistent - Invalid API endpoint via proxy (404)

### Key Features
- **Configurable URLs**: Supports custom WebApp and WebAPI URLs via CLI options or environment variables
- **Authentication**: Obtains JWT token from WebAPI /auth/dev-login endpoint
- **Comprehensive Testing**: Tests all documented WebApp pages and endpoints
- **Error Handling**: Proper exit codes (0 on success, non-zero on failure)
- **Verbose Mode**: Optional verbose output for debugging
- **Color-coded Output**: Easy-to-read test results with pass/fail indicators
- **Test Summary**: Reports total, passed, and failed tests

### Usage
```bash
# Basic usage (default URLs)
./webapp-curl-tests.sh

# Custom URLs
./webapp-curl-tests.sh -u http://localhost:5001 -a http://localhost:5000

# Verbose mode
./webapp-curl-tests.sh -v

# Custom email for auth
./webapp-curl-tests.sh -e test@example.com
```

### Environment Variables
- `WEBAPP_BASE_URL`: WebApp base URL (default: http://localhost:5001)
- `WEBAPI_BASE_URL`: WebAPI base URL for authentication (default: http://localhost:5000)
- `TEST_EMAIL`: Test email for authentication (default: test@example.com)

### Dependencies
- curl: HTTP requests
- jq: Optional JSON parsing (fallback to grep/sed if not available)
- Standard Unix tools: grep, sed, awk

### Notes
- Script follows the same pattern as webapi-curl-tests.sh for consistency
- All SPA routes return index.html (handled by MapFallbackToFile in Program.cs)
- Admin endpoints require JWT authentication obtained from WebAPI
- Static assets are served from wwwroot directory
- API proxy uses YARP to forward /v1 requests to WebAPI

### Verification
Script is executable and help output works correctly. All test functions are properly structured and called in the main function.
