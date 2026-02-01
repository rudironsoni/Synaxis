#!/bin/bash

################################################################################
# WebApp Curl Test Script
#
# Tests all WebApp pages, static assets, and API proxy functionality.
# Returns exit code 0 on success, non-zero on failure.
#
# Usage:
#   ./webapp-curl-tests.sh [options]
#
# Options:
#   -u, --url <url>       WebApp base URL (default: http://localhost:5001)
#   -a, --api-url <url>   WebAPI base URL for auth (default: http://localhost:5000)
#   -e, --email <email>   Test email for auth (default: test@example.com)
#   -v, --verbose         Enable verbose output
#   -h, --help            Show this help message
################################################################################

set -e  # Exit on error

# Default configuration
WEBAPP_BASE_URL="${WEBAPP_BASE_URL:-http://localhost:5001}"
WEBAPI_BASE_URL="${WEBAPI_BASE_URL:-http://localhost:5000}"
TEST_EMAIL="${TEST_EMAIL:-test@example.com}"
VERBOSE=false
FAILED_TESTS=0
PASSED_TESTS=0
TOTAL_TESTS=0

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# JWT token storage
JWT_TOKEN=""

################################################################################
# Helper Functions
################################################################################

print_header() {
    echo -e "\n${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}\n"
}

print_test() {
    echo -e "${YELLOW}[TEST]${NC} $1"
    ((TOTAL_TESTS++))
}

print_pass() {
    echo -e "${GREEN}[PASS]${NC} $1"
    ((PASSED_TESTS++))
}

print_fail() {
    echo -e "${RED}[FAIL]${NC} $1"
    ((FAILED_TESTS++))
}

print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

# Make a curl request and check HTTP status code
make_request() {
    local method="$1"
    local endpoint="$2"
    local data="$3"
    local expected_status="$4"
    local description="$5"
    local auth_header="$6"

    local url="${WEBAPP_BASE_URL}${endpoint}"
    local curl_cmd="curl -s -w '\n%{http_code}' -X ${method}"

    if [ -n "$auth_header" ]; then
        curl_cmd="${curl_cmd} -H 'Authorization: Bearer ${JWT_TOKEN}'"
    fi

    if [ -n "$data" ]; then
        curl_cmd="${curl_cmd} -H 'Content-Type: application/json' -d '${data}'"
    fi

    if [ "$VERBOSE" = true ]; then
        print_info "Request: ${method} ${url}"
        if [ -n "$data" ]; then
            print_info "Data: ${data}"
        fi
    fi

    local response
    response=$(eval "$curl_cmd '${url}'")
    local http_code=$(echo "$response" | tail -n1)
    local body=$(echo "$response" | sed '$d')

    if [ "$VERBOSE" = true ]; then
        print_info "Response: ${body}"
        print_info "HTTP Status: ${http_code}"
    fi

    if [ "$http_code" = "$expected_status" ]; then
        print_pass "$description"
        return 0
    else
        print_fail "$description (expected ${expected_status}, got ${http_code})"
        if [ -n "$body" ]; then
            echo -e "${RED}Response: ${body}${NC}"
        fi
        return 1
    fi
}

# Check if jq is available for JSON parsing
check_jq() {
    if ! command -v jq &> /dev/null; then
        print_info "jq not found. JSON parsing will be limited."
        return 1
    fi
    return 0
}

# Check if response contains expected content
check_content() {
    local response="$1"
    local expected_content="$2"
    local description="$3"

    if echo "$response" | grep -q "$expected_content"; then
        print_pass "$description"
        return 0
    else
        print_fail "$description - expected content not found"
        return 1
    fi
}

################################################################################
# Authentication Setup
################################################################################

setup_authentication() {
    print_header "Setting up Authentication"

    print_test "POST /auth/dev-login - Get JWT token from WebAPI"
    local response
    response=$(curl -s -w '\n%{http_code}' -X POST \
        -H "Content-Type: application/json" \
        -d "{\"email\": \"${TEST_EMAIL}\"}" \
        "${WEBAPI_BASE_URL}/auth/dev-login")

    local http_code=$(echo "$response" | tail -n1)
    local body=$(echo "$response" | sed '$d')

    if [ "$http_code" = "200" ]; then
        if check_jq; then
            JWT_TOKEN=$(echo "$body" | jq -r '.token')
        else
            # Fallback: extract token using grep/sed
            JWT_TOKEN=$(echo "$body" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
        fi

        if [ -n "$JWT_TOKEN" ] && [ "$JWT_TOKEN" != "null" ]; then
            print_pass "JWT token obtained successfully"
            if [ "$VERBOSE" = true ]; then
                print_info "Token: ${JWT_TOKEN:0:50}..."
            fi
            return 0
        else
            print_fail "Failed to extract JWT token from response"
            return 1
        fi
    else
        print_fail "Failed to get JWT token (HTTP ${http_code})"
        return 1
    fi
}

################################################################################
# WebApp Page Tests
################################################################################

test_webapp_pages() {
    print_header "Testing WebApp Pages"

    # Test root page (app shell)
    print_test "GET / - App shell (index.html)"
    local response
    response=$(curl -s -w '\n%{http_code}' -X GET "${WEBAPP_BASE_URL}/")
    local http_code=$(echo "$response" | tail -n1)
    local body=$(echo "$response" | sed '$d')

    if [ "$http_code" = "200" ]; then
        # Check if it's HTML
        if echo "$body" | grep -q "<!doctype html>" || echo "$body" | grep -q "<html"; then
            print_pass "Root page returns HTML"
        else
            print_fail "Root page does not return valid HTML"
        fi
    else
        print_fail "Root page failed (HTTP ${http_code})"
    fi

    # Test chat page (SPA route)
    print_test "GET /chat - Chat page (SPA route)"
    make_request "GET" "/chat" "" "200" "Chat page loads"

    # Test admin shell (SPA route)
    print_test "GET /admin - Admin shell (SPA route)"
    make_request "GET" "/admin" "" "200" "Admin shell loads"

    # Test admin providers page (SPA route)
    print_test "GET /admin/providers - Provider config (SPA route)"
    make_request "GET" "/admin/providers" "" "200" "Provider config page loads"

    # Test admin health page (SPA route)
    print_test "GET /admin/health - Health dashboard (SPA route)"
    make_request "GET" "/admin/health" "" "200" "Health dashboard page loads"

    # Test admin login page (SPA route)
    print_test "GET /admin/login - Login page (SPA route)"
    make_request "GET" "/admin/login" "" "200" "Login page loads"
}

################################################################################
# Static Assets Tests
################################################################################

test_static_assets() {
    print_header "Testing Static Assets"

    # Test JavaScript bundle
    print_test "GET /assets/index-*.js - JavaScript bundle"
    local response
    response=$(curl -s -w '\n%{http_code}' -X GET "${WEBAPP_BASE_URL}/assets/index-DxjQJxCP.js")
    local http_code=$(echo "$response" | tail -n1)

    if [ "$http_code" = "200" ]; then
        # Check if it's JavaScript
        if echo "$response" | grep -q "function\|const\|let\|var\|import\|export"; then
            print_pass "JavaScript bundle loads correctly"
        else
            print_fail "JavaScript bundle does not contain valid JS code"
        fi
    else
        print_fail "JavaScript bundle failed (HTTP ${http_code})"
    fi

    # Test CSS bundle
    print_test "GET /assets/index-*.css - CSS bundle"
    response=$(curl -s -w '\n%{http_code}' -X GET "${WEBAPP_BASE_URL}/assets/index-aRPP2OpI.css")
    http_code=$(echo "$response" | tail -n1)

    if [ "$http_code" = "200" ]; then
        # Check if it's CSS
        if echo "$response" | grep -q "\.css\|{\|}\|:\|;"; then
            print_pass "CSS bundle loads correctly"
        else
            print_fail "CSS bundle does not contain valid CSS code"
        fi
    else
        print_fail "CSS bundle failed (HTTP ${http_code})"
    fi

    # Test favicon
    print_test "GET /vite.svg - Favicon/image"
    make_request "GET" "/vite.svg" "" "200" "Favicon loads"
}

################################################################################
# API Proxy Tests
################################################################################

test_api_proxy() {
    print_header "Testing API Proxy (YARP)"

    # Test that /v1 routes are proxied to WebAPI
    print_test "GET /v1/models - API proxy to WebAPI"
    local response
    response=$(curl -s -w '\n%{http_code}' -X GET "${WEBAPP_BASE_URL}/v1/models")
    local http_code=$(echo "$response" | tail -n1)
    local body=$(echo "$response" | sed '$d')

    if [ "$http_code" = "200" ]; then
        # Check if it returns JSON with models
        if echo "$body" | grep -q "object\|model\|data"; then
            print_pass "API proxy correctly forwards to WebAPI"
        else
            print_fail "API proxy response does not contain expected JSON"
        fi
    else
        print_fail "API proxy failed (HTTP ${http_code})"
    fi

    # Test chat completions through proxy
    print_test "POST /v1/chat/completions - Chat through proxy"
    local chat_request='{
        "model": "default",
        "messages": [
            {"role": "user", "content": "Say hello in one word."}
        ],
        "max_tokens": 10
    }'

    response=$(curl -s -w '\n%{http_code}' -X POST \
        -H "Content-Type: application/json" \
        -d "$chat_request" \
        "${WEBAPP_BASE_URL}/v1/chat/completions")

    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')

    if [ "$http_code" = "200" ]; then
        # Check if it returns a valid chat completion response
        if echo "$body" | grep -q "choices\|message\|content"; then
            print_pass "Chat completions through proxy work correctly"
        else
            print_fail "Chat completions response does not contain expected fields"
        fi
    else
        print_fail "Chat completions through proxy failed (HTTP ${http_code})"
    fi
}

################################################################################
# Admin API Tests (via Proxy)
################################################################################

test_admin_api_via_proxy() {
    print_header "Testing Admin API via Proxy (Requires Auth)"

    if [ -z "$JWT_TOKEN" ]; then
        print_fail "JWT token not available. Skipping admin API tests."
        return 1
    fi

    # Test /admin/providers through proxy
    print_test "GET /admin/providers - Admin providers via proxy"
    local response
    response=$(curl -s -w '\n%{http_code}' -X GET \
        -H "Authorization: Bearer ${JWT_TOKEN}" \
        "${WEBAPP_BASE_URL}/admin/providers")

    local http_code=$(echo "$response" | tail -n1)
    local body=$(echo "$response" | sed '$d')

    if [ "$http_code" = "200" ]; then
        # Check if it returns provider data
        if echo "$body" | grep -q "provider\|enabled\|tier"; then
            print_pass "Admin providers endpoint works via proxy"
        else
            print_fail "Admin providers response does not contain expected data"
        fi
    else
        print_fail "Admin providers via proxy failed (HTTP ${http_code})"
    fi

    # Test /admin/health through proxy
    print_test "GET /admin/health - Admin health via proxy"
    response=$(curl -s -w '\n%{http_code}' -X GET \
        -H "Authorization: Bearer ${JWT_TOKEN}" \
        "${WEBAPP_BASE_URL}/admin/health")

    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')

    if [ "$http_code" = "200" ]; then
        # Check if it returns health data
        if echo "$body" | grep -q "status\|health\|provider"; then
            print_pass "Admin health endpoint works via proxy"
        else
            print_fail "Admin health response does not contain expected data"
        fi
    else
        print_fail "Admin health via proxy failed (HTTP ${http_code})"
    fi

    # Test without authentication (should fail)
    print_test "GET /admin/providers - Without auth (should return 401)"
    response=$(curl -s -w '\n%{http_code}' -X GET \
        "${WEBAPP_BASE_URL}/admin/providers")

    http_code=$(echo "$response" | tail -n1)

    if [ "$http_code" = "401" ]; then
        print_pass "Admin endpoint correctly rejects unauthenticated request"
    else
        print_fail "Admin endpoint should return 401 without auth (got ${http_code})"
    fi
}

################################################################################
# Authentication Flow Tests
################################################################################

test_authentication_flows() {
    print_header "Testing Authentication Flows"

    if [ -z "$JWT_TOKEN" ]; then
        print_fail "JWT token not available. Skipping auth flow tests."
        return 1
    fi

    # Test accessing protected admin page with valid token
    print_test "GET /admin/providers - With valid JWT token"
    local response
    response=$(curl -s -w '\n%{http_code}' -X GET \
        -H "Authorization: Bearer ${JWT_TOKEN}" \
        "${WEBAPP_BASE_URL}/admin/providers")

    local http_code=$(echo "$response" | tail -n1)

    if [ "$http_code" = "200" ]; then
        print_pass "Protected admin page accessible with valid token"
    else
        print_fail "Protected admin page failed with valid token (HTTP ${http_code})"
    fi

    # Test accessing protected admin page with invalid token
    print_test "GET /admin/providers - With invalid JWT token"
    response=$(curl -s -w '\n%{http_code}' -X GET \
        -H "Authorization: Bearer invalid.token.here" \
        "${WEBAPP_BASE_URL}/admin/providers")

    http_code=$(echo "$response" | tail -n1)

    if [ "$http_code" = "401" ] || [ "$http_code" = "403" ]; then
        print_pass "Protected admin page correctly rejects invalid token"
    else
        print_fail "Protected admin page should reject invalid token (got ${http_code})"
    fi

    # Test accessing protected admin page without token
    print_test "GET /admin/providers - Without JWT token"
    response=$(curl -s -w '\n%{http_code}' -X GET \
        "${WEBAPP_BASE_URL}/admin/providers")

    http_code=$(echo "$response" | tail -n1)

    if [ "$http_code" = "401" ] || [ "$http_code" = "403" ]; then
        print_pass "Protected admin page correctly rejects missing token"
    else
        print_fail "Protected admin page should reject missing token (got ${http_code})"
    fi
}

################################################################################
# Error Scenario Tests
################################################################################

test_error_scenarios() {
    print_header "Testing Error Scenarios"

    # Test non-existent page (should return index.html for SPA)
    print_test "GET /nonexistent-page - SPA fallback to index.html"
    local response
    response=$(curl -s -w '\n%{http_code}' -X GET "${WEBAPP_BASE_URL}/nonexistent-page")
    local http_code=$(echo "$response" | tail -n1)
    local body=$(echo "$response" | sed '$d')

    if [ "$http_code" = "200" ]; then
        # SPA should return index.html for any route
        if echo "$body" | grep -q "<!doctype html>" || echo "$body" | grep -q "<html"; then
            print_pass "SPA correctly returns index.html for non-existent route"
        else
            print_fail "SPA fallback does not return valid HTML"
        fi
    else
        print_fail "SPA fallback failed (HTTP ${http_code})"
    fi

    # Test non-existent static asset
    print_test "GET /assets/nonexistent.js - Non-existent static asset"
    response=$(curl -s -w '\n%{http_code}' -X GET "${WEBAPP_BASE_URL}/assets/nonexistent.js")
    http_code=$(echo "$response" | tail -n1)

    if [ "$http_code" = "404" ]; then
        print_pass "Non-existent static asset returns 404"
    else
        print_fail "Non-existent static asset should return 404 (got ${http_code})"
    fi

    # Test invalid API endpoint through proxy
    print_test "GET /v1/nonexistent - Invalid API endpoint via proxy"
    response=$(curl -s -w '\n%{http_code}' -X GET "${WEBAPP_BASE_URL}/v1/nonexistent")
    http_code=$(echo "$response" | tail -n1)

    if [ "$http_code" = "404" ]; then
        print_pass "Invalid API endpoint returns 404"
    else
        print_fail "Invalid API endpoint should return 404 (got ${http_code})"
    fi
}

################################################################################
# Main Test Runner
################################################################################

print_usage() {
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  -u, --url <url>       WebApp base URL (default: http://localhost:5001)"
    echo "  -a, --api-url <url>   WebAPI base URL for auth (default: http://localhost:5000)"
    echo "  -e, --email <email>   Test email for auth (default: test@example.com)"
    echo "  -v, --verbose         Enable verbose output"
    echo "  -h, --help            Show this help message"
    echo ""
    echo "Environment variables:"
    echo "  WEBAPP_BASE_URL       WebApp base URL"
    echo "  WEBAPI_BASE_URL       WebAPI base URL for authentication"
    echo "  TEST_EMAIL            Test email for authentication"
}

parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -u|--url)
                WEBAPP_BASE_URL="$2"
                shift 2
                ;;
            -a|--api-url)
                WEBAPI_BASE_URL="$2"
                shift 2
                ;;
            -e|--email)
                TEST_EMAIL="$2"
                shift 2
                ;;
            -v|--verbose)
                VERBOSE=true
                shift
                ;;
            -h|--help)
                print_usage
                exit 0
                ;;
            *)
                echo "Unknown option: $1"
                print_usage
                exit 1
                ;;
        esac
    done
}

main() {
    parse_arguments "$@"

    print_header "WebApp Curl Test Suite"
    print_info "WebApp Base URL: ${WEBAPP_BASE_URL}"
    print_info "WebAPI Base URL: ${WEBAPI_BASE_URL}"
    print_info "Test Email: ${TEST_EMAIL}"
    print_info "Verbose: ${VERBOSE}"

    # Check if WebApp is reachable
    print_info "Checking if WebApp is reachable..."
    if ! curl -s -f "${WEBAPP_BASE_URL}/" > /dev/null 2>&1; then
        print_fail "WebApp is not reachable at ${WEBAPP_BASE_URL}"
        print_info "Make sure the WebApp is running before running tests."
        exit 1
    fi
    print_pass "WebApp is reachable"

    # Check if WebAPI is reachable (for auth)
    print_info "Checking if WebAPI is reachable (for auth)..."
    if ! curl -s -f "${WEBAPI_BASE_URL}/health/liveness" > /dev/null 2>&1; then
        print_fail "WebAPI is not reachable at ${WEBAPI_BASE_URL}"
        print_info "Make sure the WebAPI is running for authentication tests."
        exit 1
    fi
    print_pass "WebAPI is reachable"

    # Run all test suites
    test_webapp_pages
    test_static_assets
    test_api_proxy
    setup_authentication
    test_admin_api_via_proxy
    test_authentication_flows
    test_error_scenarios

    # Print summary
    print_header "Test Summary"
    echo -e "Total tests:  ${TOTAL_TESTS}"
    echo -e "${GREEN}Passed:       ${PASSED_TESTS}${NC}"
    echo -e "${RED}Failed:       ${FAILED_TESTS}${NC}"

    if [ $FAILED_TESTS -eq 0 ]; then
        echo -e "\n${GREEN}All tests passed!${NC}"
        exit 0
    else
        echo -e "\n${RED}Some tests failed.${NC}"
        exit 1
    fi
}

# Run main function
main "$@"
