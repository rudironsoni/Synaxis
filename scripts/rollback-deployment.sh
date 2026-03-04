#!/bin/bash
# <copyright file="rollback-deployment.sh" company="Synaxis">
# Copyright (c) Synaxis. All rights reserved.
# </copyright>

# rollback-deployment.sh
# Rolls back a Kubernetes deployment to a previous revision.
#
# Usage:
#   ./rollback-deployment.sh [revision]
#
# Arguments:
#   revision    Target revision number, or omit for previous revision
#
# Environment Variables:
#   DEPLOYMENT    Deployment name (default: synaxis-api)
#   NAMESPACE     Kubernetes namespace (default: synaxis)
#   TIMEOUT       Rollout timeout in seconds (default: 300)

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
DEPLOYMENT="${DEPLOYMENT:-synaxis-api}"
NAMESPACE="${NAMESPACE:-synaxis}"
TIMEOUT="${TIMEOUT:-300}"
REVISION="${1:-0}"

# Check prerequisites
if ! command -v kubectl &> /dev/null; then
    log_error "kubectl not found in PATH"
    exit 1
fi

# Verify namespace exists
if ! kubectl get namespace "$NAMESPACE" &> /dev/null; then
    log_error "Namespace '$NAMESPACE' not found"
    exit 1
fi

# Verify deployment exists
if ! kubectl get deployment "$DEPLOYMENT" -n "$NAMESPACE" &> /dev/null; then
    log_error "Deployment '$DEPLOYMENT' not found in namespace '$NAMESPACE'"
    exit 1
fi

log_info "Starting deployment rollback..."
log_info "Deployment: $DEPLOYMENT"
log_info "Namespace: $NAMESPACE"

# Show rollout history
log_info "Rollout history:"
kubectl rollout history deployment/"$DEPLOYMENT" -n "$NAMESPACE" | tail -10

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
    kubectl rollout undo deployment/"$DEPLOYMENT" -n "$NAMESPACE"
else
    log_info "Rolling back to revision $REVISION..."
    kubectl rollout undo deployment/"$DEPLOYMENT" -n "$NAMESPACE" --to-revision="$REVISION"
fi

# Monitor rollback progress
log_info "Monitoring rollback progress (timeout: ${TIMEOUT}s)..."
if ! kubectl rollout status deployment/"$DEPLOYMENT" -n "$NAMESPACE" --timeout="${TIMEOUT}s"; then
    log_error "Rollout did not complete within timeout"
    log_info "Check status with: kubectl get pods -n $NAMESPACE"
    exit 1
fi

# Verify deployment
log_info "Verifying deployment..."
echo ""
kubectl get deployment/"$DEPLOYMENT" -n "$NAMESPACE" -o wide

echo ""
kubectl get pods -n "$NAMESPACE" -l app.kubernetes.io/component=api -o wide

echo ""
log_success "Deployment rollback completed successfully!"
