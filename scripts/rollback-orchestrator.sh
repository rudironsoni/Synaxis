#!/bin/bash
# <copyright file="rollback-orchestrator.sh" company="Synaxis">
# Copyright (c) Synaxis. All rights reserved.
# </copyright>

# rollback-orchestrator.sh
# Orchestrates a complete rollback across all components.
#
# Usage:
#   ./rollback-orchestrator.sh <scenario> [options]
#
# Arguments:
#   scenario    Rollback scenario: app-bluegreen, app-rolling, database, infrastructure
#
# Options:
#   --target <value>      Target version/revision/migration
#   --namespace <value>   Kubernetes namespace (default: synaxis)
#   --skip-validation     Skip post-rollback validation
#   --notify              Send Slack notification
#
# Environment Variables:
#   NAMESPACE             Kubernetes namespace
#   SLACK_WEBHOOK_URL     Slack webhook for notifications
#   API_ENDPOINT          API endpoint for validation

set -euo pipefail

# Colors for output
readonly COLOR_INFO='\033[0;36m'
readonly COLOR_SUCCESS='\033[0;32m'
readonly COLOR_WARNING='\033[1;33m'
readonly COLOR_ERROR='\033[0;31m'
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

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
NAMESPACE="${NAMESPACE:-synaxis}"
API_ENDPOINT="${API_ENDPOINT:-https://api.synaxis.io}"
SKIP_VALIDATION=false
NOTIFY=false
TARGET=""

# Parse arguments
SCENARIO="${1:-}"
shift || true

while [[ $# -gt 0 ]]; do
    case $1 in
        --target)
            TARGET="$2"
            shift 2
            ;;
        --namespace)
            NAMESPACE="$2"
            shift 2
            ;;
        --skip-validation)
            SKIP_VALIDATION=true
            shift
            ;;
        --notify)
            NOTIFY=true
            shift
            ;;
        --help)
            show_usage
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

show_usage() {
    cat <<EOF
Usage: $0 <scenario> [options]

Scenarios:
  app-bluegreen     Rollback Blue/Green deployment
  app-rolling       Rollback rolling deployment
  helm              Rollback Helm release
  database          Rollback database migration
  infrastructure    Rollback Terraform infrastructure

Options:
  --target <value>      Target version/revision/migration
  --namespace <value>   Kubernetes namespace (default: synaxis)
  --skip-validation     Skip post-rollback validation
  --notify              Send Slack notification

Examples:
  $0 app-bluegreen --target green --notify
  $0 database --target InitialMultiTenant
  $0 infrastructure --target production
EOF
}

if [[ -z "$SCENARIO" ]]; then
    log_error "Scenario required"
    show_usage
    exit 1
fi

# Start time
START_TIME=$(date +%s)

log_info "======================================"
log_info "   SYNAXIS ROLLBACK ORCHESTRATOR"
log_info "======================================"
log_info "Scenario: $SCENARIO"
log_info "Target: ${TARGET:-auto}"
log_info "Namespace: $NAMESPACE"
log_info "Started: $(date -Iseconds)"
echo ""

# Notify start
if [[ "$NOTIFY" == true ]]; then
    "$SCRIPT_DIR/notify-slack.sh" "P1" "Rollback initiated: $SCENARIO scenario" || true
fi

# Execute rollback based on scenario
case "$SCENARIO" in
    app-bluegreen)
        if [[ -z "$TARGET" ]]; then
            TARGET="blue"  # Default: rollback to blue
        fi
        log_info "Executing Blue/Green rollback to $TARGET..."
        "$SCRIPT_DIR/rollback-bluegreen.sh" "$TARGET" || exit 1
        ;;
    
    app-rolling)
        log_info "Executing rolling deployment rollback..."
        if [[ -n "$TARGET" ]]; then
            "$SCRIPT_DIR/rollback-deployment.sh" "$TARGET" || exit 1
        else
            "$SCRIPT_DIR/rollback-deployment.sh" || exit 1
        fi
        ;;
    
    helm)
        log_info "Executing Helm rollback..."
        if [[ -n "$TARGET" ]]; then
            "$SCRIPT_DIR/rollback-helm.sh" "$TARGET" || exit 1
        else
            "$SCRIPT_DIR/rollback-helm.sh" || exit 1
        fi
        ;;
    
    database)
        if [[ -z "$TARGET" ]]; then
            log_error "Migration target required for database rollback"
            log_info "Usage: $0 database --target <migration_name>"
            exit 1
        fi
        log_info "Executing database migration rollback to $TARGET..."
        "$SCRIPT_DIR/rollback-migration.sh" "$TARGET" || exit 1
        ;;
    
    infrastructure)
        if [[ -z "$TARGET" ]]; then
            log_error "Environment target required for infrastructure rollback"
            log_info "Usage: $0 infrastructure --target <environment>"
            exit 1
        fi
        log_info "Executing infrastructure rollback for $TARGET..."
        "$SCRIPT_DIR/terraform-rollback.sh" "$TARGET" || exit 1
        ;;
    
    *)
        log_error "Unknown scenario: $SCENARIO"
        show_usage
        exit 1
        ;;
esac

# Calculate duration
END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

echo ""
log_success "Rollback execution completed in ${DURATION}s"

# Post-rollback validation
if [[ "$SKIP_VALIDATION" == false ]]; then
    echo ""
    log_info "Running post-rollback validation..."
    if "$SCRIPT_DIR/post-rollback-validation.sh"; then
        log_success "Validation passed"
        VALIDATION_RESULT="passed"
    else
        log_error "Validation failed"
        VALIDATION_RESULT="failed"
    fi
else
    log_warning "Validation skipped"
    VALIDATION_RESULT="skipped"
fi

# Summary
echo ""
log_info "======================================"
log_info "           ROLLBACK SUMMARY           "
log_info "======================================"
log_info "Scenario: $SCENARIO"
log_info "Duration: ${DURATION} seconds"
log_info "Validation: $VALIDATION_RESULT"
log_info "Completed: $(date -Iseconds)"
log_info "======================================"

# Notify completion
if [[ "$NOTIFY" == true ]]; then
    NOTIFY_MESSAGE="Rollback completed: $SCENARIO scenario\nDuration: ${DURATION}s\nValidation: $VALIDATION_RESULT"
    if [[ "$VALIDATION_RESULT" == "passed" ]]; then
        "$SCRIPT_DIR/notify-slack.sh" "RESOLVED" "$NOTIFY_MESSAGE" || true
    else
        "$SCRIPT_DIR/notify-slack.sh" "P1" "$NOTIFY_MESSAGE" || true
    fi
fi

if [[ "$VALIDATION_RESULT" == "failed" ]]; then
    exit 1
fi

log_success "Rollback orchestration complete!"
