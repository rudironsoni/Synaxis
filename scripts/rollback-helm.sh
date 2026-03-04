#!/bin/bash
# <copyright file="rollback-helm.sh" company="Synaxis">
# Copyright (c) Synaxis. All rights reserved.
# </copyright>

# rollback-helm.sh
# Rolls back a Helm release to a previous revision.
#
# Usage:
#   ./rollback-helm.sh [revision]
#
# Arguments:
#   revision    Target revision number, or omit for previous revision
#
# Environment Variables:
#   RELEASE     Helm release name (default: synaxis)
#   NAMESPACE   Kubernetes namespace (default: synaxis)
#   TIMEOUT     Helm timeout (default: 5m)

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
RELEASE="${RELEASE:-synaxis}"
NAMESPACE="${NAMESPACE:-synaxis}"
TIMEOUT="${TIMEOUT:-5m}"
REVISION="${1:-0}"

# Check prerequisites
if ! command -v helm &> /dev/null; then
    log_error "helm not found in PATH"
    exit 1
fi

# Check if release exists
if ! helm status "$RELEASE" -n "$NAMESPACE" &> /dev/null; then
    log_error "Helm release '$RELEASE' not found in namespace '$NAMESPACE'"
    exit 1
fi

log_info "Starting Helm rollback..."
log_info "Release: $RELEASE"
log_info "Namespace: $NAMESPACE"

# Show release history
log_info "Release history:"
helm history "$RELEASE" -n "$NAMESPACE" --max=10

# Confirm rollback (interactive if TTY)
if [[ -t 0 && "$REVISION" == "0" ]]; then
    echo ""
    read -p "Rollback to previous revision? [y/N] " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        log_warning "Rollback cancelled by user"
        exit 0
    fi
fi

# Execute rollback
log_info "Initiating rollback..."
if [[ "$REVISION" == "0" ]]; then
    log_info "Rolling back to previous revision..."
    helm rollback "$RELEASE" -n "$NAMESPACE" --timeout="$TIMEOUT"
else
    log_info "Rolling back to revision $REVISION..."
    helm rollback "$RELEASE" "$REVISION" -n "$NAMESPACE" --timeout="$TIMEOUT"
fi

# Verify rollback
log_info "Verifying rollback..."
echo ""
helm status "$RELEASE" -n "$NAMESPACE"

echo ""
log_info "Running Helm tests (if available)..."
helm test "$RELEASE" -n "$NAMESPACE" --timeout="$TIMEOUT" || log_warning "Helm tests failed or not available"

echo ""
log_success "Helm rollback completed successfully!"
