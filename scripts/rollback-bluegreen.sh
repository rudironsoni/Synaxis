#!/bin/bash
# <copyright file="rollback-bluegreen.sh" company="Synaxis">
# Copyright (c) Synaxis. All rights reserved.
# </copyright>

# rollback-bluegreen.sh
# Rolls back Blue/Green deployment by switching traffic to stable version.
#
# Usage:
#   ./rollback-bluegreen.sh [current_version]
#
# Arguments:
#   current_version   Current active version ('green' or 'blue'). Default: green
#
# Environment Variables:
#   NAMESPACE         Kubernetes namespace (default: synaxis)
#   SERVICE_NAME      Kubernetes service name (default: synaxis-api)
#   KUBECONFIG        Path to kubeconfig file

set -euo pipefail

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

# Configuration
NAMESPACE="${NAMESPACE:-synaxis}"
SERVICE_NAME="${SERVICE_NAME:-synaxis-api}"
CURRENT_VERSION="${1:-green}"

# Validate current version
if [[ "$CURRENT_VERSION" != "green" && "$CURRENT_VERSION" != "blue" ]]; then
    log_error "Invalid version: $CURRENT_VERSION. Must be 'green' or 'blue'"
    exit 1
fi

# Determine target version
TARGET_VERSION="blue"
if [[ "$CURRENT_VERSION" == "blue" ]]; then
    TARGET_VERSION="green"
fi

log_info "Starting Blue/Green rollback..."
log_info "Current version: $CURRENT_VERSION"
log_info "Target version: $TARGET_VERSION"

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

# Verify service exists
if ! kubectl get service "$SERVICE_NAME" -n "$NAMESPACE" &> /dev/null; then
    log_error "Service '$SERVICE_NAME' not found in namespace '$NAMESPACE'"
    exit 1
fi

# Verify target deployment exists
if ! kubectl get deployment "${SERVICE_NAME}-${TARGET_VERSION}" -n "$NAMESPACE" &> /dev/null; then
    log_error "Target deployment '${SERVICE_NAME}-${TARGET_VERSION}' not found"
    exit 1
fi

# Check target deployment health
log_info "Checking target deployment health..."
READY_REPLICAS=$(kubectl get deployment "${SERVICE_NAME}-${TARGET_VERSION}" -n "$NAMESPACE" -o jsonpath='{.status.readyReplicas}' 2>/dev/null || echo "0")
DESIRED_REPLICAS=$(kubectl get deployment "${SERVICE_NAME}-${TARGET_VERSION}" -n "$NAMESPACE" -o jsonpath='{.spec.replicas}' 2>/dev/null || echo "0")

if [[ "$READY_REPLICAS" -lt "$DESIRED_REPLICAS" ]]; then
    log_warning "Target deployment has $READY_REPLICAS/$DESIRED_REPLICAS ready replicas"
    log_info "Waiting for target deployment to be ready..."
    kubectl rollout status deployment/"${SERVICE_NAME}-${TARGET_VERSION}" -n "$NAMESPACE" --timeout=300s
fi

# Update service selector to point to target version
log_info "Switching traffic to $TARGET_VERSION..."
kubectl patch service "$SERVICE_NAME" -n "$NAMESPACE" --type='json' -p="[{
    \"op\": \"replace\",
    \"path\": \"/spec/selector/version\",
    \"value\": \"$TARGET_VERSION\"
}]"

# Verify traffic shift
log_info "Verifying traffic shift..."
sleep 5

ENDPOINTS=$(kubectl get endpoints "$SERVICE_NAME" -n "$NAMESPACE" -o jsonpath='{range .subsets[*].addresses[*]}{.ip}{"\n"}{end}' 2>/dev/null || echo "")
if [[ -z "$ENDPOINTS" ]]; then
    log_error "No endpoints found for service after traffic shift"
    exit 1
fi

log_info "Service endpoints:"
kubectl get endpoints "$SERVICE_NAME" -n "$NAMESPACE" -o wide

# Scale down current (problematic) deployment
log_info "Scaling down $CURRENT_VERSION deployment..."
kubectl scale deployment "${SERVICE_NAME}-${CURRENT_VERSION}" -n "$NAMESPACE" --replicas=0

# Wait for scale down
log_info "Waiting for pods to terminate..."
sleep 10

# Final verification
log_info "Final status:"
echo ""
echo "=== Service ==="
kubectl get service "$SERVICE_NAME" -n "$NAMESPACE" -o wide
echo ""
echo "=== Target Pods ==="
kubectl get pods -n "$NAMESPACE" -l "app=${SERVICE_NAME},version=${TARGET_VERSION}" -o wide
echo ""
echo "=== Current Pods (should be 0) ==="
kubectl get pods -n "$NAMESPACE" -l "app=${SERVICE_NAME},version=${CURRENT_VERSION}" -o wide || true

echo ""
log_success "Blue/Green rollback completed successfully!"
log_info "Traffic is now routing to $TARGET_VERSION"
