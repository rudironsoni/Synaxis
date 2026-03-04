#!/bin/bash
# <copyright file="post-rollback-validation.sh" company="Synaxis">
# Copyright (c) Synaxis. All rights reserved.
# </copyright>

# post-rollback-validation.sh
# Validates system health after a rollback.
#
# Usage:
#   ./post-rollback-validation.sh
#
# Environment Variables:
#   API_ENDPOINT    API health endpoint (default: https://api.synaxis.io)
#   NAMESPACE       Kubernetes namespace (default: synaxis)
#   TIMEOUT         Request timeout in seconds (default: 10)

set -euo pipefail

# Colors for output
readonly COLOR_INFO='\033[0;36m'
readonly COLOR_SUCCESS='\033[0;32m'
readonly COLOR_WARNING='\033[1;33m'
readonly COLOR_ERROR='\033[0;31m'
readonly COLOR_RESET='\033[0m'

# Counters
CHECKS_PASSED=0
CHECKS_FAILED=0
CHECKS_WARNED=0

# Logging functions
log_info() {
    echo -e "${COLOR_INFO}[INFO]${COLOR_RESET} $1"
}

log_success() {
    echo -e "${COLOR_SUCCESS}[PASS]${COLOR_RESET} $1"
    CHECKS_PASSED=$((CHECKS_PASSED + 1))
}

log_warning() {
    echo -e "${COLOR_WARNING}[WARN]${COLOR_RESET} $1"
    CHECKS_WARNED=$((CHECKS_WARNED + 1))
}

log_error() {
    echo -e "${COLOR_ERROR}[FAIL]${COLOR_RESET} $1"
    CHECKS_FAILED=$((CHECKS_FAILED + 1))
}

# Configuration
API_ENDPOINT="${API_ENDPOINT:-https://api.synaxis.io}"
NAMESPACE="${NAMESPACE:-synaxis}"
TIMEOUT="${TIMEOUT:-10}"

log_info "Starting post-rollback validation..."
log_info "API Endpoint: $API_ENDPOINT"
log_info "Namespace: $NAMESPACE"
echo ""

# 1. Check kubectl connectivity
if command -v kubectl &> /dev/null; then
    log_info "[1/7] Checking Kubernetes connectivity..."
    if kubectl cluster-info &> /dev/null; then
        log_success "Connected to Kubernetes cluster"
    else
        log_error "Cannot connect to Kubernetes cluster"
    fi
else
    log_warning "kubectl not available, skipping K8s checks"
fi

# 2. Pod health check
if command -v kubectl &> /dev/null; then
    log_info "[2/7] Checking pod health..."
    if kubectl get namespace "$NAMESPACE" &> /dev/null; then
        UNREADY_PODS=$(kubectl get pods -n "$NAMESPACE" -o json 2>/dev/null | \
            jq -r '[.items[] | select(.status.phase != "Running" or (.status.containerStatuses? // empty) | any(.ready != true))] | length' 2>/dev/null || echo "0")
        
        TOTAL_PODS=$(kubectl get pods -n "$NAMESPACE" --no-headers 2>/dev/null | wc -l)
        
        if [[ "$UNREADY_PODS" -eq 0 && "$TOTAL_PODS" -gt 0 ]]; then
            log_success "All $TOTAL_PODS pods are ready"
        elif [[ "$TOTAL_PODS" -eq 0 ]]; then
            log_warning "No pods found in namespace $NAMESPACE"
        else
            log_error "$UNREADY_PODS out of $TOTAL_PODS pods are not ready"
            kubectl get pods -n "$NAMESPACE" --field-selector=status.phase!=Running 2>/dev/null || true
        fi
    else
        log_warning "Namespace $NAMESPACE not found"
    fi
fi

# 3. Health endpoint check
log_info "[3/7] Checking health endpoint..."
if command -v curl &> /dev/null; then
    HEALTH_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "${API_ENDPOINT}/health" --max-time "$TIMEOUT" 2>/dev/null || echo "000")
    
    if [[ "$HEALTH_STATUS" == "200" ]]; then
        log_success "Health endpoint responding (HTTP 200)"
    elif [[ "$HEALTH_STATUS" == "000" ]]; then
        log_error "Health endpoint unreachable"
    else
        log_error "Health endpoint returned HTTP $HEALTH_STATUS"
    fi
else
    log_warning "curl not available, skipping health check"
fi

# 4. Readiness endpoint check
log_info "[4/7] Checking readiness endpoint..."
if command -v curl &> /dev/null; then
    READY_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "${API_ENDPOINT}/ready" --max-time "$TIMEOUT" 2>/dev/null || echo "000")
    
    if [[ "$READY_STATUS" == "200" ]]; then
        log_success "Readiness endpoint responding (HTTP 200)"
    elif [[ "$READY_STATUS" == "000" ]]; then
        log_warning "Readiness endpoint unreachable"
    else
        log_error "Readiness endpoint returned HTTP $READY_STATUS"
    fi
fi

# 5. Deployment status
if command -v kubectl &> /dev/null; then
    log_info "[5/7] Checking deployment status..."
    if kubectl get namespace "$NAMESPACE" &> /dev/null; then
        UNAVAILABLE_DEPS=$(kubectl get deployments -n "$NAMESPACE" -o json 2>/dev/null | \
            jq -r '[.items[] | select(.status.unavailableReplicas? // 0 > 0)] | length' 2>/dev/null || echo "0")
        
        if [[ "$UNAVAILABLE_DEPS" -eq 0 ]]; then
            log_success "All deployments are available"
        else
            log_error "$UNAVAILABLE_DEPS deployments have unavailable replicas"
        fi
    fi
fi

# 6. Service endpoints
if command -v kubectl &> /dev/null; then
    log_info "[6/7] Checking service endpoints..."
    if kubectl get namespace "$NAMESPACE" &> /dev/null; then
        EMPTY_ENDPOINTS=$(kubectl get endpoints -n "$NAMESPACE" -o json 2>/dev/null | \
            jq -r '[.items[] | select(.subsets? // empty | length == 0)] | length' 2>/dev/null || echo "0")
        
        if [[ "$EMPTY_ENDPOINTS" -eq 0 ]]; then
            log_success "All services have endpoints"
        else
            log_warning "$EMPTY_ENDPOINTS services have no endpoints"
        fi
    fi
fi

# 7. Basic API response check
log_info "[7/7] Checking API response..."
if command -v curl &> /dev/null; then
    API_RESPONSE=$(curl -s "${API_ENDPOINT}/health" --max-time "$TIMEOUT" 2>/dev/null || echo "")
    
    if [[ -n "$API_RESPONSE" ]]; then
        if echo "$API_RESPONSE" | grep -q '"status".*"healthy"\|"status".*"Healthy"\|healthy\|Healthy' 2>/dev/null; then
            log_success "API reports healthy status"
        else
            log_warning "API responded but status unclear"
        fi
    else
        log_warning "No API response received"
    fi
fi

# Summary
echo ""
echo "========================================"
echo "           VALIDATION SUMMARY           "
echo "========================================"
echo -e "${COLOR_SUCCESS}Passed:  $CHECKS_PASSED${COLOR_RESET}"
echo -e "${COLOR_WARNING}Warnings: $CHECKS_WARNED${COLOR_RESET}"
echo -e "${COLOR_ERROR}Failed:  $CHECKS_FAILED${COLOR_RESET}"
echo "========================================"

if [[ "$CHECKS_FAILED" -eq 0 ]]; then
    echo ""
    log_success "All critical validation checks passed!"
    exit 0
else
    echo ""
    log_error "$CHECKS_FAILED validation check(s) failed"
    exit 1
fi
