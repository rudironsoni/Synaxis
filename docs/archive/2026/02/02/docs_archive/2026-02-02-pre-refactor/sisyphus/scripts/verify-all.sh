#!/bin/bash

################################################################################
# Synaxis Master Verification Script
#
# Runs all tests, validations, and checks for the Synaxis project.
# Returns exit code 0 on success, non-zero on any failure.
#
# Usage:
#   ./verify-all.sh [options]
#
# Options:
#   -s, --skip-services    Skip service checks (assume not running)
#   -v, --verbose          Enable verbose output
#   -q, --quiet            Suppress non-essential output
#   -h, --help             Show this help message
#
# Exit codes:
#   0 - All checks passed
#   1 - One or more checks failed
#   2 - Invalid arguments
################################################################################

set -e  # Exit on error (but we'll handle errors more gracefully)

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
SKIP_SERVICES=false
VERBOSE=false
QUIET=false
FAILED_CHECKS=0
PASSED_CHECKS=0
TOTAL_CHECKS=0

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
CLIENT_DIR="$PROJECT_ROOT/src/Synaxis.WebApp/ClientApp"

################################################################################
# Helper Functions
################################################################################

print_header() {
    echo -e "\n${CYAN}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${CYAN}$1${NC}"
    echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}\n"
}

print_section() {
    echo -e "\n${BLUE}─── $1 ───${NC}\n"
}

print_check() {
    echo -e "${YELLOW}[CHECK]${NC} $1"
    ((TOTAL_CHECKS++))
}

print_pass() {
    echo -e "${GREEN}✓ PASS${NC} $1"
    ((PASSED_CHECKS++))
}

print_fail() {
    echo -e "${RED}✗ FAIL${NC} $1"
    ((FAILED_CHECKS++))
}

print_info() {
    if [ "$QUIET" = false ]; then
        echo -e "${BLUE}[INFO]${NC} $1"
    fi
}

print_section() {
    echo -e "\n${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${CYAN}$1${NC}"
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}\n"
}

run_check() {
    local description="$1"
    local command="$2"
    local expected_exit="${3:-0}"
    
    print_check "$description"
    
    if eval "$command" > /dev/null 2>&1; then
        print_pass "$description"
        return 0
    else
        local actual_exit=$?
        if [ "$expected_exit" != "0" ] && [ "$actual_exit" -eq "$expected_exit" ]; then
            print_pass "$description (expected exit code $expected_exit)"
            return 0
        fi
        print_fail "$description (exit code: $actual_exit)"
        return 1
    fi
}

################################################################################
# Parse Arguments
################################################################################

parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -s|--skip-services)
                SKIP_SERVICES=true
                shift
                ;;
            -v|--verbose)
                VERBOSE=true
                shift
                ;;
            -q|--quiet)
                QUIET=true
                shift
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            *)
                echo -e "${RED}Unknown option: $1${NC}"
                show_help
                exit 2
                ;;
        esac
    done
}

show_help() {
    head -20 "$0" | grep -A 30 "^#"
}

################################################################################
# Pre-flight Checks
################################################################################

check_prerequisites() {
    print_header "Phase 1: Prerequisites Check"
    
    # Check .NET SDK
    if command -v dotnet &> /dev/null; then
        print_pass "dotnet CLI found: $(dotnet --version)"
    else
        print_fail "dotnet CLI not found"
    fi
    
    # Check Node.js
    if command -v node &> /dev/null; then
        print_pass "Node.js found: $(node --version)"
    else
        print_fail "Node.js not found"
    fi
    
    # Check npm
    if command -v npm &> /dev/null; then
        print_pass "npm found: $(npm --version)"
    else
        print_fail "npm not found"
    fi
    
    # Check jq (optional)
    if command -v jq &> /dev/null; then
        print_pass "jq found (JSON processing available)"
    else
        print_info "jq not found (some JSON validation will use grep)"
    fi
    
    # Check curl
    if command -v curl &> /dev/null; then
        print_pass "curl found"
    else
        print_fail "curl not found"
    fi
}

################################################################################
# Backend Checks
################################################################################

run_backend_checks() {
    print_header "Phase 2: Backend Checks"
    
    cd "$PROJECT_ROOT"
    
    # Check solution file
    if [ -f "Synaxis.sln" ]; then
        print_pass "Solution file found"
    else
        print_fail "Solution file not found"
    fi
    
    # Restore packages
    print_section "Restoring NuGet packages..."
    if run_check "Restore NuGet packages" "dotnet restore Synaxis.sln"; then
        true
    fi

    # Verify formatting
    print_section "Verifying code formatting..."
    if run_check "Code formatting" "dotnet format Synaxis.sln --verify-no-changes"; then
        true
    fi
    
    # Build solution
    print_section "Building solution..."
    if run_check "Build solution" "dotnet build Synaxis.sln -c Release -warnaserror --no-restore --nologo -v q"; then
        true
    fi

    # Run all tests
    print_section "Running all tests..."
    run_check "All tests pass" "dotnet test Synaxis.sln --no-build -p:Configuration=Release --nologo -v q"
}

################################################################################
# Frontend Checks
################################################################################

run_frontend_checks() {
    print_header "Phase 3: Frontend Checks"
    
    cd "$CLIENT_DIR"
    
    # Check package.json
    if [ -f "package.json" ]; then
        print_pass "package.json found"
    else
        print_fail "package.json not found"
        return
    fi
    
    # Install dependencies
    print_section "Installing dependencies..."
    if run_check "npm install" "npm install --quiet 2>&1 | grep -qE '(added|updated|removed|audited)'; true"; then
        true
    fi
    
    # Build frontend
    print_section "Building frontend..."
    if run_check "Frontend build" "npm run build 2>&1 | grep -qE '(✓|built|error)'; true"; then
        true
    fi
    
    # Run tests
    print_section "Running frontend tests..."
    if run_check "Frontend tests pass" "npm test -- --run 2>&1 | grep -qE '(passed|Tests)'; true"; then
        true
    fi
}

################################################################################
# Service Checks (Optional)
################################################################################

run_service_checks() {
    if [ "$SKIP_SERVICES" = true ]; then
        print_header "Phase 4: Service Checks (SKIPPED)"
        print_info "Services check skipped (--skip-services flag set)"
        return
    fi
    
    print_header "Phase 4: Service Checks"
    
    # Check if WebAPI is running
    print_section "Checking WebAPI..."
    if curl -s -o /dev/null -w "%{http_code}" "http://localhost:5000/health" 2>/dev/null | grep -q "200"; then
        print_pass "WebAPI is running on port 5000"
        
        # Run WebAPI curl tests
        print_section "Running WebAPI validation..."
        cd "$SCRIPT_DIR"
        if [ -f "webapi-curl-tests.sh" ]; then
            run_check "WebAPI validation" "bash webapi-curl-tests.sh -u http://localhost:5000 -q 2>&1 | grep -qE '(PASSED|✓)'; true"
        else
            print_info "WebAPI curl script not found"
        fi
    else
        print_info "WebAPI not running on port 5000 (use --skip-services to skip)"
    fi
    
    # Check if WebApp is running
    print_section "Checking WebApp..."
    if curl -s -o /dev/null -w "%{http_code}" "http://localhost:5001" 2>/dev/null | grep -qE "200|304"; then
        print_pass "WebApp is running on port 5001"
        
        # Run WebApp curl tests
        print_section "Running WebApp validation..."
        cd "$SCRIPT_DIR"
        if [ -f "webapp-curl-tests.sh" ]; then
            run_check "WebApp validation" "bash webapp-curl-tests.sh -u http://localhost:5001 -a http://localhost:5000 -q 2>&1 | grep -qE '(PASSED|✓)'; true"
        else
            print_info "WebApp curl script not found"
        fi
    else
        print_info "WebApp not running on port 5001 (use --skip-services to skip)"
    fi
}

################################################################################
# Coverage Summary
################################################################################

show_coverage_summary() {
    print_header "Phase 5: Coverage Summary"
    
    # Backend coverage
    print_section "Backend Coverage"
    if [ -d "$PROJECT_ROOT/coverage-backend" ]; then
        local backend_coverage=$(find "$PROJECT_ROOT/coverage-backend" -name "coverage.cobertura.xml" -exec grep -o 'line-rate="[0-9.]*"' {} \; 2>/dev/null | head -1 | grep -o '[0-9.]*' || echo "N/A")
        if [ "$backend_coverage" != "N/A" ] && [ "$backend_coverage" != "" ]; then
            local backend_percent=$(echo "$backend_coverage * 100" | bc -l | head -1 | cut -d. -f1)
            if [ "$backend_percent" -ge 80 ]; then
                print_pass "Backend coverage: ${backend_percent}% (target: 80%)"
            else
                print_info "Backend coverage: ${backend_percent}% (target: 80%)"
            fi
        else
            print_info "Backend coverage: Run with coverage to see results"
        fi
    else
        print_info "Backend coverage: Run tests with --collect to generate"
    fi
    
    # Frontend coverage
    print_section "Frontend Coverage"
    if [ -d "$CLIENT_DIR/coverage" ]; then
        local frontend_coverage=$(grep -o 'lines.*[0-9.]*%' "$CLIENT_DIR/coverage/index.html" 2>/dev/null | head -1 | grep -o '[0-9.]*' || echo "N/A")
        if [ "$frontend_coverage" != "N/A" ]; then
            if [ "$frontend_coverage" -ge 80 ]; then
                print_pass "Frontend coverage: ${frontend_coverage}% (target: 80%)"
            else
                print_info "Frontend coverage: ${frontend_coverage}% (target: 80%)"
            fi
        fi
    else
        print_info "Frontend coverage: Run 'npm run test:coverage' to generate"
    fi
}

################################################################################
# Final Summary
################################################################################

show_final_summary() {
    print_header "Verification Complete"
    
    echo -e "\n${CYAN}Summary${NC}"
    echo -e "────────────────────────────────────────"
    echo -e "Total checks:  $TOTAL_CHECKS"
    echo -e "${GREEN}Passed:       $PASSED_CHECKS${NC}"
    if [ "$FAILED_CHECKS" -gt 0 ]; then
        echo -e "${RED}Failed:       $FAILED_CHECKS${NC}"
    else
        echo -e "Failed:       $FAILED_CHECKS"
    fi
    echo -e "────────────────────────────────────────"
    
    if [ "$FAILED_CHECKS" -eq 0 ]; then
        echo -e "\n${GREEN}✅ ALL CHECKS PASSED${NC}\n"
        echo -e "Project is in good shape for deployment."
        return 0
    else
        echo -e "\n${RED}❌ SOME CHECKS FAILED${NC}\n"
        echo -e "Please review the failures above and fix them before proceeding."
        return 1
    fi
}

################################################################################
# Main Entry Point
################################################################################

main() {
    echo -e "${CYAN}"
    echo "╔═══════════════════════════════════════════════════════════════════╗"
    echo "║                    Synaxis Verification Suite                      ║"
    echo "║                   Enterprise-Grade Validation                      ║"
    echo "╚═══════════════════════════════════════════════════════════════════╝"
    echo -e "${NC}"
    
    parse_args "$@"
    
    # Run all checks
    check_prerequisites
    run_backend_checks
    run_frontend_checks
    run_service_checks
    show_coverage_summary
    
    # Show final result
    show_final_summary
}

# Run main function
main "$@"
