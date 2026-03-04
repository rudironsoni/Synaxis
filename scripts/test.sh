#!/bin/bash
# Test execution script with reliability improvements
# Usage: ./scripts/test.sh [Unit|Integration|E2E|All]

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Default test category
TEST_CATEGORY="${1:-Unit}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Running $TEST_CATEGORY tests...${NC}"

# Create results directory
mkdir -p "$PROJECT_ROOT/TestResults"
mkdir -p "$PROJECT_ROOT/TestResults/logs"

# Set environment variables for CI reliability
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export TESTCONTAINERS_RYUK_DISABLED=true

# Build the solution first
echo -e "${YELLOW}Building solution...${NC}"
dotnet build "$PROJECT_ROOT/Synaxis.sln" --configuration Release --no-restore

# Run tests based on category
case $TEST_CATEGORY in
    Unit)
        echo -e "${YELLOW}Running Unit Tests...${NC}"
        dotnet test "$PROJECT_ROOT/Synaxis.sln" \
            --configuration Release \
            --no-build \
            --filter "Category=Unit" \
            --settings "$PROJECT_ROOT/coverlet.runsettings" \
            --logger "trx;LogFileName=Unit-TestResults.trx" \
            --logger "console;verbosity=detailed" \
            --blame-hang-timeout 5m \
            --blame-crash \
            --collect:"XPlat Code Coverage" \
            --results-directory "$PROJECT_ROOT/TestResults" \
            --diag "$PROJECT_ROOT/TestResults/logs/unit-logs.txt" \
            || { echo -e "${RED}Unit tests failed!${NC}"; exit 1; }
        ;;
    
    Integration)
        echo -e "${YELLOW}Running Integration Tests...${NC}"
        dotnet test "$PROJECT_ROOT/Synaxis.sln" \
            --configuration Release \
            --no-build \
            --filter "Category=Integration" \
            --settings "$PROJECT_ROOT/coverlet.runsettings" \
            --logger "trx;LogFileName=Integration-TestResults.trx" \
            --logger "console;verbosity=detailed" \
            --blame-hang-timeout 10m \
            --blame-crash \
            --collect:"XPlat Code Coverage" \
            --results-directory "$PROJECT_ROOT/TestResults" \
            --diag "$PROJECT_ROOT/TestResults/logs/integration-logs.txt" \
            || { echo -e "${RED}Integration tests failed!${NC}"; exit 1; }
        ;;
    
    E2E)
        echo -e "${YELLOW}Running E2E Tests...${NC}"
        dotnet test "$PROJECT_ROOT/Synaxis.sln" \
            --configuration Release \
            --no-build \
            --filter "Category=E2E" \
            --settings "$PROJECT_ROOT/coverlet.runsettings" \
            --logger "trx;LogFileName=E2E-TestResults.trx" \
            --logger "console;verbosity=detailed" \
            --blame-hang-timeout 15m \
            --blame-crash \
            --collect:"XPlat Code Coverage" \
            --results-directory "$PROJECT_ROOT/TestResults" \
            --diag "$PROJECT_ROOT/TestResults/logs/e2e-logs.txt" \
            || { echo -e "${RED}E2E tests failed!${NC}"; exit 1; }
        ;;
    
    All|all)
        echo -e "${YELLOW}Running All Tests...${NC}"
        dotnet test "$PROJECT_ROOT/Synaxis.sln" \
            --configuration Release \
            --no-build \
            --settings "$PROJECT_ROOT/coverlet.runsettings" \
            --logger "trx;LogFileName=All-TestResults.trx" \
            --logger "console;verbosity=detailed" \
            --blame-hang-timeout 30m \
            --blame-crash \
            --collect:"XPlat Code Coverage" \
            --results-directory "$PROJECT_ROOT/TestResults" \
            --diag "$PROJECT_ROOT/TestResults/logs/all-logs.txt" \
            || { echo -e "${RED}Tests failed!${NC}"; exit 1; }
        ;;
    
    *)
        echo "Usage: $0 [Unit|Integration|E2E|All]"
        echo ""
        echo "Options:"
        echo "  Unit         - Run unit tests only (fast, no external dependencies)"
        echo "  Integration  - Run integration tests (requires Docker)"
        echo "  E2E          - Run end-to-end tests (requires full environment)"
        echo "  All          - Run all tests"
        exit 1
        ;;
esac

echo -e "${GREEN}Tests completed successfully!${NC}"
echo "Test results available at: $PROJECT_ROOT/TestResults/"
echo "Coverage reports available at: $PROJECT_ROOT/TestResults/*/coverage.*"
