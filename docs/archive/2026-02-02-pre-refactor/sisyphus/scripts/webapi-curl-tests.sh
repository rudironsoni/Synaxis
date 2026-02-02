#!/bin/bash

################################################################################
# WebAPI Curl Test Script
#
# Tests all WebAPI endpoints with happy path and error scenarios.
# Returns exit code 0 on success, non-zero on failure.
#
# Usage:
#   ./webapi-curl-tests.sh [options]
#
# Options:
#   -u, --url <url>       API base URL (default: http://localhost:5000)
#   -e, --email <email>   Test email for auth (default: test@example.com)
#   -v, --verbose         Enable verbose output
#   -h, --help            Show this help message
################################################################################

set -e  # Exit on error

# Default configuration
API_BASE_URL="${API_BASE_URL:-http://localhost:5000}"
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
    local auth_header="$5"
    local description="$5"

    local url="${API_BASE_URL}${endpoint}"
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

################################################################################
# Authentication Setup
################################################################################

setup_authentication() {
    print_header "Setting up Authentication"

    print_test "POST /auth/dev-login - Get JWT token"
    local response
    response=$(curl -s -w '\n%{http_code}' -X POST \
        -H "Content-Type: application/json" \
        -d "{\"email\": \"${TEST_EMAIL}\"}" \
        "${API_BASE_URL}/auth/dev-login")

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
# Health Check Tests
################################################################################

test_health_checks() {
    print_header "Testing Health Check Endpoints"

    # Test liveness
    print_test "GET /health/liveness - Check API is alive"
    make_request "GET" "/health/liveness" "" "200" "Liveness check"

    # Test readiness
    print_test "GET /health/readiness - Check API is ready"
    make_request "GET" "/health/readiness" "" "200" "Readiness check"
}

################################################################################
# OpenAI Models Tests
################################################################################

test_models() {
    print_header "Testing OpenAI Models Endpoints"

    # List all models
    print_test "GET /openai/v1/models - List all models"
    make_request "GET" "/openai/v1/models" "" "200" "List models"

    # Get specific model (will likely fail with 404, but that's expected)
    print_test "GET /openai/v1/models/gpt-4 - Get specific model (may 404)"
    make_request "GET" "/openai/v1/models/gpt-4" "" "200" "Get specific model" || true

    # Test invalid model ID
    print_test "GET /openai/v1/models/invalid-model-id - Should return 404"
    make_request "GET" "/openai/v1/models/invalid-model-id" "" "404" "Invalid model ID returns 404" || true
}

################################################################################
# OpenAI Chat Completions Tests
################################################################################

test_chat_completions() {
    print_header "Testing OpenAI Chat Completions"

    local chat_request='{
        "model": "default",
        "messages": [
            {"role": "user", "content": "Say hello in one word."}
        ],
        "max_tokens": 10
    }'

    # Non-streaming chat completion
    print_test "POST /openai/v1/chat/completions - Non-streaming"
    make_request "POST" "/openai/v1/chat/completions" "$chat_request" "200" "Non-streaming chat completion"

    # Streaming chat completion
    print_test "POST /openai/v1/chat/completions - Streaming"
    local streaming_request=$(echo "$chat_request" | jq '. + {"stream": true}')
    local response
    response=$(curl -s -w '\n%{http_code}' -X POST \
        -H "Content-Type: application/json" \
        -d "$streaming_request" \
        "${API_BASE_URL}/openai/v1/chat/completions")

    local http_code=$(echo "$response" | tail -n1)
    local body=$(echo "$response" | sed '$d')

    if [ "$http_code" = "200" ]; then
        # Check if response contains SSE format
        if echo "$body" | grep -q "data:"; then
            print_pass "Streaming chat completion returns SSE format"
        else
            print_fail "Streaming response missing SSE format"
        fi
    else
        print_fail "Streaming chat completion failed (HTTP ${http_code})"
    fi

    # Test with invalid model
    print_test "POST /openai/v1/chat/completions - Invalid model"
    local invalid_request=$(echo "$chat_request" | jq '.model = "invalid-model-xyz"')
    make_request "POST" "/openai/v1/chat/completions" "$invalid_request" "400" "Invalid model returns 400" || true

    # Test with missing messages
    print_test "POST /openai/v1/chat/completions - Missing messages"
    local missing_messages_request='{"model": "default"}'
    make_request "POST" "/openai/v1/chat/completions" "$missing_messages_request" "400" "Missing messages returns 400" || true
}

################################################################################
# OpenAI Completions (Legacy) Tests
################################################################################

test_legacy_completions() {
    print_header "Testing OpenAI Legacy Completions"

    local completion_request='{
        "model": "default",
        "prompt": "Say hello",
        "max_tokens": 10
    }'

    # Non-streaming completion
    print_test "POST /openai/v1/completions - Non-streaming (legacy)"
    make_request "POST" "/openai/v1/completions" "$completion_request" "200" "Non-streaming legacy completion"

    # Streaming completion
    print_test "POST /openai/v1/completions - Streaming (legacy)"
    local streaming_request=$(echo "$completion_request" | jq '. + {"stream": true}')
    local response
    response=$(curl -s -w '\n%{http_code}' -X POST \
        -H "Content-Type: application/json" \
        -d "$streaming_request" \
        "${API_BASE_URL}/openai/v1/completions")

    local http_code=$(echo "$response" | tail -n1)
    local body=$(echo "$response" | sed '$d')

    if [ "$http_code" = "200" ]; then
        if echo "$body" | grep -q "data:"; then
            print_pass "Streaming legacy completion returns SSE format"
        else
            print_fail "Streaming response missing SSE format"
        fi
    else
        print_fail "Streaming legacy completion failed (HTTP ${http_code})"
    fi
}

################################################################################
# OpenAI Responses Tests
################################################################################

test_responses() {
    print_header "Testing OpenAI Responses Endpoint"

    local response_request='{
        "model": "default",
        "messages": [
            {"role": "user", "content": "What is 2+2?"}
        ],
        "max_tokens": 10
    }'

    # Non-streaming response
    print_test "POST /openai/v1/responses - Non-streaming"
    make_request "POST" "/openai/v1/responses" "$response_request" "200" "Non-streaming response"

    # Streaming response
    print_test "POST /openai/v1/responses - Streaming"
    local streaming_request=$(echo "$response_request" | jq '. + {"stream": true}')
    local response
    response=$(curl -s -w '\n%{http_code}' -X POST \
        -H "Content-Type: application/json" \
        -d "$streaming_request" \
        "${API_BASE_URL}/openai/v1/responses")

    local http_code=$(echo "$response" | tail -n1)
    local body=$(echo "$response" | sed '$d')

    if [ "$http_code" = "200" ]; then
        if echo "$body" | grep -q "data:"; then
            print_pass "Streaming response returns SSE format"
        else
            print_fail "Streaming response missing SSE format"
        fi
    else
        print_fail "Streaming response failed (HTTP ${http_code})"
    fi
}

################################################################################
# Admin Endpoints Tests
################################################################################

test_admin_endpoints() {
    print_header "Testing Admin Endpoints (Requires Auth)"

    if [ -z "$JWT_TOKEN" ]; then
        print_fail "JWT token not available. Skipping admin tests."
        return 1
    fi

    # Get providers
    print_test "GET /admin/providers - List providers (with auth)"
    make_request "GET" "/admin/providers" "" "200" "List providers" "auth"

    # Get health
    print_test "GET /admin/health - Get health status (with auth)"
    make_request "GET" "/admin/health" "" "200" "Get health status" "auth"

    print_test "PUT /admin/providers/Groq - Update provider (with auth)"
    local update_request='{
        "enabled": true,
        "tier": 0
    }'
    make_request "PUT" "/admin/providers/Groq" "$update_request" "200" "Update provider" "auth" || true

    # Test without authentication (should fail)
    print_test "GET /admin/providers - Without auth (should return 401)"
    local response
    response=$(curl -s -w '\n%{http_code}' -X GET \
        "${API_BASE_URL}/admin/providers")

    local http_code=$(echo "$response" | tail -n1)

    if [ "$http_code" = "401" ]; then
        print_pass "Admin endpoint correctly rejects unauthenticated request"
    else
        print_fail "Admin endpoint should return 401 without auth (got ${http_code})"
    fi
}

################################################################################
# Error Scenario Tests
################################################################################

test_error_scenarios() {
    print_header "Testing Error Scenarios"

    # Invalid JSON
    print_test "POST /openai/v1/chat/completions - Invalid JSON"
    local response
    response=$(curl -s -w '\n%{http_code}' -X POST \
        -H "Content-Type: application/json" \
        -d "{invalid json}" \
        "${API_BASE_URL}/openai/v1/chat/completions")

    local http_code=$(echo "$response" | tail -n1)

    if [ "$http_code" = "400" ]; then
        print_pass "Invalid JSON returns 400"
    else
        print_fail "Invalid JSON should return 400 (got ${http_code})"
    fi

    # Missing Content-Type
    print_test "POST /openai/v1/chat/completions - Missing Content-Type"
    local response
    response=$(curl -s -w '\n%{http_code}' -X POST \
        -d '{"model":"default","messages":[]}' \
        "${API_BASE_URL}/openai/v1/chat/completions")

    local http_code=$(echo "$response" | tail -n1)

    # This might still work or return 415, either is acceptable
    if [ "$http_code" = "415" ] || [ "$http_code" = "400" ]; then
        print_pass "Missing Content-Type handled correctly (${http_code})"
    else
        print_info "Missing Content-Type returned ${http_code} (may be acceptable)"
    fi

    # Non-existent endpoint
    print_test "GET /nonexistent - Non-existent endpoint"
    local response
    response=$(curl -s -w '\n%{http_code}' -X GET \
        "${API_BASE_URL}/nonexistent")

    local http_code=$(echo "$response" | tail -n1)

    if [ "$http_code" = "404" ]; then
        print_pass "Non-existent endpoint returns 404"
    else
        print_fail "Non-existent endpoint should return 404 (got ${http_code})"
    fi
}

################################################################################
# Main Test Runner
################################################################################

print_usage() {
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  -u, --url <url>       API base URL (default: http://localhost:5000)"
    echo "  -e, --email <email>   Test email for auth (default: test@example.com)"
    echo "  -v, --verbose         Enable verbose output"
    echo "  -h, --help            Show this help message"
    echo ""
    echo "Environment variables:"
    echo "  API_BASE_URL          API base URL"
    echo "  TEST_EMAIL            Test email for authentication"
}

parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -u|--url)
                API_BASE_URL="$2"
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

    print_header "WebAPI Curl Test Suite"
    print_info "API Base URL: ${API_BASE_URL}"
    print_info "Test Email: ${TEST_EMAIL}"
    print_info "Verbose: ${VERBOSE}"

    # Check if API is reachable
    print_info "Checking if API is reachable..."
    if ! curl -s -f "${API_BASE_URL}/health/liveness" > /dev/null 2>&1; then
        print_fail "API is not reachable at ${API_BASE_URL}"
        print_info "Make sure the API is running before running tests."
        exit 1
    fi
    print_pass "API is reachable"

    # Run all test suites
    test_health_checks
    test_models
    test_chat_completions
    test_legacy_completions
    test_responses
    setup_authentication
    test_admin_endpoints
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
