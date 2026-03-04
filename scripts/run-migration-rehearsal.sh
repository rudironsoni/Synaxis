#!/bin/bash
# <copyright file="run-migration-rehearsal.sh" company="Synaxis">
# Copyright (c) Synaxis. All rights reserved.
# </copyright>

# run-migration-rehearsal.sh
# Executes migration rehearsals in staging to validate procedures.
#
# Usage:
#   ./run-migration-rehearsal.sh [options]
#
# Options:
#   -e, --environment     Target environment (default: staging)
#   -s, --scenario        Specific scenario to run (happy-path|failure|partial|performance|all)
#   -o, --output          Output directory for results (default: ./rehearsal-results)
#   -c, --connection      Database connection string
#   -v, --verbose         Enable verbose output
#   -h, --help            Show this help message
#
# Environment Variables:
#   REHEARSAL_ENVIRONMENT     Target environment
#   REHEARSAL_CONNECTION      Database connection string
#   REHEARSAL_OUTPUT_DIR      Output directory
#   REHEARSAL_VERBOSE         Enable verbose output (true/false)

set -euo pipefail

# Script metadata
readonly SCRIPT_VERSION="1.0.0"
readonly SCRIPT_NAME="run-migration-rehearsal"

# Colors for output
readonly COLOR_INFO='\033[0;36m'    # Cyan
readonly COLOR_SUCCESS='\033[0;32m' # Green
readonly COLOR_WARNING='\033[1;33m' # Yellow
readonly COLOR_ERROR='\033[0;31m'   # Red
readonly COLOR_RESET='\033[0m'

# Logging functions
log_info() {
    echo -e "${COLOR_INFO}[INFO]${COLOR_RESET} $1"
}

log_success() {
    echo -e "${COLOR_SUCCESS}[SUCCESS]${COLOR_RESET} $1"
}

log_warning() {
    echo -e "${COLOR_WARNING}[WARNING]${COLOR_RESET} $1"
}

log_error() {
    echo -e "${COLOR_ERROR}[ERROR]${COLOR_RESET} $1"
}

log_section() {
    echo ""
    echo -e "${COLOR_INFO}==============================================${COLOR_RESET}"
    echo -e "${COLOR_INFO}$1${COLOR_RESET}"
    echo -e "${COLOR_INFO}==============================================${COLOR_RESET}"
}

# Default values
ENVIRONMENT="${REHEARSAL_ENVIRONMENT:-staging}"
SCENARIO="${REHEARSAL_SCENARIO:-all}"
OUTPUT_DIR="${REHEARSAL_OUTPUT_DIR:-./rehearsal-results}"
CONNECTION_STRING="${REHEARSAL_CONNECTION_STRING:-}"
VERBOSE="${REHEARSAL_VERBOSE:-false}"

# Get script directory and project paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
INFRASTRUCTURE_PROJECT="$ROOT_DIR/src/Synaxis.Infrastructure/Synaxis.Infrastructure.csproj"
TEST_PROJECT="$ROOT_DIR/tests/Synaxis.Infrastructure.UnitTests/Synaxis.Infrastructure.UnitTests.csproj"

# Show usage
usage() {
    cat << EOF
Usage: $(basename "$0") [OPTIONS]

Executes migration rehearsals in staging to validate procedures.

OPTIONS:
    -e, --environment ENV       Target environment (default: staging)
    -s, --scenario SCENARIO   Specific scenario to run:
                                happy-path, failure, partial, performance, all
    -o, --output DIR          Output directory (default: ./rehearsal-results)
    -c, --connection STRING   Database connection string
    -v, --verbose             Enable verbose output
    -h, --help                Show this help message
    --version                 Show version information

ENVIRONMENT VARIABLES:
    REHEARSAL_ENVIRONMENT     Target environment
    REHEARSAL_CONNECTION      Database connection string
    REHEARSAL_OUTPUT_DIR      Output directory
    REHEARSAL_VERBOSE         Enable verbose output (true/false)

EXAMPLES:
    # Run all scenarios in staging
    $(basename "$0")

    # Run only happy path rehearsal
    $(basename "$0") -s happy-path

    # Run with custom connection string
    $(basename "$0") -c "Host=staging-db;Database=synaxis;Username=postgres;Password=secret"

    # Run specific scenarios with verbose output
    $(basename "$0") -s failure -s partial -v

    # Run with custom output directory
    $(basename "$0") -o /path/to/output

EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -s|--scenario)
            SCENARIO="$2"
            shift 2
            ;;
        -o|--output)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        -c|--connection)
            CONNECTION_STRING="$2"
            shift 2
            ;;
        -v|--verbose)
            VERBOSE="true"
            shift
            ;;
        --version)
            echo "$SCRIPT_NAME version $SCRIPT_VERSION"
            exit 0
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        -*)
            log_error "Unknown option: $1"
            usage
            exit 1
            ;;
        *)
            log_error "Unexpected argument: $1"
            usage
            exit 1
            ;;
    esac
done

# Validate scenario
valid_scenarios=("happy-path" "failure" "partial" "performance" "all")
if [[ ! " ${valid_scenarios[@]} " =~ " ${SCENARIO} " ]]; then
    log_error "Invalid scenario: $SCENARIO"
    log_error "Valid scenarios: ${valid_scenarios[*]}"
    exit 1
fi

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Check prerequisites
check_prerequisites() {
    log_section "CHECKING PREREQUISITES"

    local failed=0

    # Check dotnet CLI
    if ! command -v dotnet &> /dev/null; then
        log_error "dotnet CLI is not installed or not in PATH"
        failed=1
    else
        log_info "dotnet CLI: $(dotnet --version)"
    fi

    # Check PostgreSQL tools
    if ! command -v pg_dump &> /dev/null; then
        log_warning "pg_dump not found - database backup may be limited"
    else
        log_info "pg_dump: $(pg_dump --version | head -1)"
    fi

    # Check infrastructure project
    if [[ ! -f "$INFRASTRUCTURE_PROJECT" ]]; then
        log_error "Infrastructure project not found: $INFRASTRUCTURE_PROJECT"
        failed=1
    else
        log_info "Infrastructure project: $INFRASTRUCTURE_PROJECT"
    fi

    # Check test project
    if [[ ! -f "$TEST_PROJECT" ]]; then
        log_error "Test project not found: $TEST_PROJECT"
        failed=1
    else
        log_info "Test project: $TEST_PROJECT"
    fi

    if [[ $failed -eq 1 ]]; then
        log_error "Prerequisite check failed"
        exit 1
    fi

    log_success "All prerequisites satisfied"
}

# Run happy path rehearsal
run_happy_path_rehearsal() {
    log_section "HAPPY PATH REHEARSAL"

    log_info "Executing complete migration runbook..."
    log_info "Environment: $ENVIRONMENT"
    log_info "Output directory: $OUTPUT_DIR"

    local start_time=$(date +%s)

    # Run the happy path tests
    if [[ "$VERBOSE" == "true" ]]; then
        dotnet test "$TEST_PROJECT" \
            --filter "FullyQualifiedName~MigrationRehearsalTests.HappyPathRehearsal_ExecutesAllSteps" \
            --logger "console;verbosity=detailed" \
            --results-directory "$OUTPUT_DIR" \
            2>&1 | tee "$OUTPUT_DIR/happy-path-output.log"
    else
        dotnet test "$TEST_PROJECT" \
            --filter "FullyQualifiedName~MigrationRehearsalTests.HappyPathRehearsal_ExecutesAllSteps" \
            --results-directory "$OUTPUT_DIR" \
            > "$OUTPUT_DIR/happy-path-output.log" 2>&1
    fi

    local exit_code=${PIPESTATUS[0]}
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))

    if [[ $exit_code -eq 0 ]]; then
        log_success "Happy path rehearsal completed successfully in ${duration}s"
        echo "{\"scenario\": \"happy-path\", \"status\": \"passed\", \"duration\": $duration}" > "$OUTPUT_DIR/happy-path-result.json"
        return 0
    else
        log_error "Happy path rehearsal failed after ${duration}s"
        echo "{\"scenario\": \"happy-path\", \"status\": \"failed\", \"duration\": $duration}" > "$OUTPUT_DIR/happy-path-result.json"
        return 1
    fi
}

# Run failure scenario rehearsal
run_failure_scenario_rehearsal() {
    log_section "FAILURE SCENARIO REHEARSAL"

    log_info "Simulating database migration failures..."
    log_info "Testing rollback procedures..."

    local start_time=$(date +%s)

    if [[ "$VERBOSE" == "true" ]]; then
        dotnet test "$TEST_PROJECT" \
            --filter "FullyQualifiedName~MigrationRehearsalTests.FailureScenarioRehearsal_VerifiesRollback" \
            --logger "console;verbosity=detailed" \
            --results-directory "$OUTPUT_DIR" \
            2>&1 | tee "$OUTPUT_DIR/failure-scenario-output.log"
    else
        dotnet test "$TEST_PROJECT" \
            --filter "FullyQualifiedName~MigrationRehearsalTests.FailureScenarioRehearsal_VerifiesRollback" \
            --results-directory "$OUTPUT_DIR" \
            > "$OUTPUT_DIR/failure-scenario-output.log" 2>&1
    fi

    local exit_code=${PIPESTATUS[0]}
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))

    if [[ $exit_code -eq 0 ]]; then
        log_success "Failure scenario rehearsal completed successfully in ${duration}s"
        echo "{\"scenario\": \"failure\", \"status\": \"passed\", \"duration\": $duration}" > "$OUTPUT_DIR/failure-scenario-result.json"
        return 0
    else
        log_error "Failure scenario rehearsal failed after ${duration}s"
        echo "{\"scenario\": \"failure\", \"status\": \"failed\", \"duration\": $duration}" > "$OUTPUT_DIR/failure-scenario-result.json"
        return 1
    fi
}

# Run partial failure rehearsal
run_partial_failure_rehearsal() {
    log_section "PARTIAL FAILURE REHEARSAL"

    log_info "Simulating service failures during rollout..."
    log_info "Testing graceful degradation..."

    local start_time=$(date +%s)

    if [[ "$VERBOSE" == "true" ]]; then
        dotnet test "$TEST_PROJECT" \
            --filter "FullyQualifiedName~MigrationRehearsalTests.PartialFailureRehearsal_VerifiesGracefulDegradation" \
            --logger "console;verbosity=detailed" \
            --results-directory "$OUTPUT_DIR" \
            2>&1 | tee "$OUTPUT_DIR/partial-failure-output.log"
    else
        dotnet test "$TEST_PROJECT" \
            --filter "FullyQualifiedName~MigrationRehearsalTests.PartialFailureRehearsal_VerifiesGracefulDegradation" \
            --results-directory "$OUTPUT_DIR" \
            > "$OUTPUT_DIR/partial-failure-output.log" 2>&1
    fi

    local exit_code=${PIPESTATUS[0]}
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))

    if [[ $exit_code -eq 0 ]]; then
        log_success "Partial failure rehearsal completed successfully in ${duration}s"
        echo "{\"scenario\": \"partial\", \"status\": \"passed\", \"duration\": $duration}" > "$OUTPUT_DIR/partial-failure-result.json"
        return 0
    else
        log_error "Partial failure rehearsal failed after ${duration}s"
        echo "{\"scenario\": \"partial\", \"status\": \"failed\", \"duration\": $duration}" > "$OUTPUT_DIR/partial-failure-result.json"
        return 1
    fi
}

# Run performance baseline rehearsal
run_performance_baseline_rehearsal() {
    log_section "PERFORMANCE BASELINE REHEARSAL"

    log_info "Establishing performance baselines..."
    log_info "Comparing pre/post migration metrics..."

    local start_time=$(date +%s)

    if [[ "$VERBOSE" == "true" ]]; then
        dotnet test "$TEST_PROJECT" \
            --filter "FullyQualifiedName~MigrationRehearsalTests.PerformanceBaselineRehearsal_CapturesMetrics" \
            --logger "console;verbosity=detailed" \
            --results-directory "$OUTPUT_DIR" \
            2>&1 | tee "$OUTPUT_DIR/performance-baseline-output.log"
    else
        dotnet test "$TEST_PROJECT" \
            --filter "FullyQualifiedName~MigrationRehearsalTests.PerformanceBaselineRehearsal_CapturesMetrics" \
            --results-directory "$OUTPUT_DIR" \
            > "$OUTPUT_DIR/performance-baseline-output.log" 2>&1
    fi

    local exit_code=${PIPESTATUS[0]}
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))

    if [[ $exit_code -eq 0 ]]; then
        log_success "Performance baseline rehearsal completed successfully in ${duration}s"
        echo "{\"scenario\": \"performance\", \"status\": \"passed\", \"duration\": $duration}" > "$OUTPUT_DIR/performance-baseline-result.json"
        return 0
    else
        log_error "Performance baseline rehearsal failed after ${duration}s"
        echo "{\"scenario\": \"performance\", \"status\": \"failed\", \"duration\": $duration}" > "$OUTPUT_DIR/performance-baseline-result.json"
        return 1
    fi
}

# Run full rehearsal
run_full_rehearsal() {
    log_section "FULL MIGRATION REHEARSAL"

    log_info "Executing complete migration rehearsal suite..."
    log_info "This includes: Happy Path, Failure Scenarios, Partial Failure, Performance Baseline"

    local start_time=$(date +%s)

    if [[ "$VERBOSE" == "true" ]]; then
        dotnet test "$TEST_PROJECT" \
            --filter "FullyQualifiedName~MigrationRehearsalTests" \
            --logger "console;verbosity=detailed" \
            --results-directory "$OUTPUT_DIR" \
            2>&1 | tee "$OUTPUT_DIR/full-rehearsal-output.log"
    else
        dotnet test "$TEST_PROJECT" \
            --filter "FullyQualifiedName~MigrationRehearsalTests" \
            --results-directory "$OUTPUT_DIR" \
            > "$OUTPUT_DIR/full-rehearsal-output.log" 2>&1
    fi

    local exit_code=${PIPESTATUS[0]}
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))

    if [[ $exit_code -eq 0 ]]; then
        log_success "Full rehearsal completed successfully in ${duration}s"
        echo "{\"scenario\": \"full\", \"status\": \"passed\", \"duration\": $duration}" > "$OUTPUT_DIR/full-rehearsal-result.json"
        return 0
    else
        log_error "Full rehearsal failed after ${duration}s"
        echo "{\"scenario\": \"full\", \"status\": \"failed\", \"duration\": $duration}" > "$OUTPUT_DIR/full-rehearsal-result.json"
        return 1
    fi
}

# Generate summary report
generate_summary() {
    log_section "GENERATING SUMMARY REPORT"

    local report_file="$OUTPUT_DIR/rehearsal-summary-$(date +%Y%m%d-%H%M%S).md"

    cat > "$report_file" << EOF
# Migration Rehearsal Summary Report

**Generated:** $(date -u +"%Y-%m-%d %H:%M:%S UTC")  
**Environment:** $ENVIRONMENT  
**Scenario:** $SCENARIO

## Execution Summary

EOF

    # Add results for each scenario
    if [[ -f "$OUTPUT_DIR/happy-path-result.json" ]]; then
        cat >> "$report_file" << EOF
### Happy Path Rehearsal
- Status: $(jq -r '.status' "$OUTPUT_DIR/happy-path-result.json")
- Duration: $(jq -r '.duration' "$OUTPUT_DIR/happy-path-result.json")s

EOF
    fi

    if [[ -f "$OUTPUT_DIR/failure-scenario-result.json" ]]; then
        cat >> "$report_file" << EOF
### Failure Scenario Rehearsal
- Status: $(jq -r '.status' "$OUTPUT_DIR/failure-scenario-result.json")
- Duration: $(jq -r '.duration' "$OUTPUT_DIR/failure-scenario-result.json")s

EOF
    fi

    if [[ -f "$OUTPUT_DIR/partial-failure-result.json" ]]; then
        cat >> "$report_file" << EOF
### Partial Failure Rehearsal
- Status: $(jq -r '.status' "$OUTPUT_DIR/partial-failure-result.json")
- Duration: $(jq -r '.duration' "$OUTPUT_DIR/partial-failure-result.json")s

EOF
    fi

    if [[ -f "$OUTPUT_DIR/performance-baseline-result.json" ]]; then
        cat >> "$report_file" << EOF
### Performance Baseline Rehearsal
- Status: $(jq -r '.status' "$OUTPUT_DIR/performance-baseline-result.json")
- Duration: $(jq -r '.duration' "$OUTPUT_DIR/performance-baseline-result.json")s

EOF
    fi

    cat >> "$report_file" << EOF

## Output Files

- Happy Path Log: \`happy-path-output.log\`
- Failure Scenario Log: \`failure-scenario-output.log\`
- Partial Failure Log: \`partial-failure-output.log\`
- Performance Baseline Log: \`performance-baseline-output.log\`

## Next Steps

1. Review all log files for detailed results
2. Verify Go/No-Go decision criteria
3. Update runbook with lessons learned
4. Schedule stakeholder review

EOF

    log_success "Summary report generated: $report_file"
}

# Main execution
main() {
    log_section "MIGRATION REHEARSAL TOOL v$SCRIPT_VERSION"

    log_info "Environment: $ENVIRONMENT"
    log_info "Scenario: $SCENARIO"
    log_info "Output Directory: $OUTPUT_DIR"

    # Check prerequisites
    check_prerequisites

    # Track overall success
    local overall_success=0

    # Run requested scenarios
    case "$SCENARIO" in
        happy-path)
            run_happy_path_rehearsal || overall_success=1
            ;;
        failure)
            run_failure_scenario_rehearsal || overall_success=1
            ;;
        partial)
            run_partial_failure_rehearsal || overall_success=1
            ;;
        performance)
            run_performance_baseline_rehearsal || overall_success=1
            ;;
        all)
            run_happy_path_rehearsal || overall_success=1
            run_failure_scenario_rehearsal || overall_success=1
            run_partial_failure_rehearsal || overall_success=1
            run_performance_baseline_rehearsal || overall_success=1
            ;;
        *)
            log_error "Unknown scenario: $SCENARIO"
            exit 1
            ;;
    esac

    # Generate summary report
    generate_summary

    # Final status
    log_section "REHEARSAL COMPLETE"

    if [[ $overall_success -eq 0 ]]; then
        log_success "All requested rehearsals completed successfully"
        log_info "Results available in: $OUTPUT_DIR"
        exit 0
    else
        log_error "One or more rehearsals failed"
        log_info "Check logs in: $OUTPUT_DIR"
        exit 1
    fi
}

# Run main function
main "$@"
