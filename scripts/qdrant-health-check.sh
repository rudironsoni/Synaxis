#!/bin/bash

# Configuration with defaults
QDRANT_HOST="${QDRANT_HOST:-qdrant}"
QDRANT_PORT="${QDRANT_PORT:-6333}"
QDRANT_API_KEY="${QDRANT_API_KEY}"
COLLECTION="${QDRANT_COLLECTION:-synaxis_semantic_cache}"
VERBOSE="${VERBOSE:-false}"

# Exit codes
EXIT_OK=0
EXIT_ERROR=1
EXIT_WARNING=2

# Function to log output
log() {
    if [ "$VERBOSE" = "true" ]; then
        echo "[$(date)] $1"
    fi
}

log_error() {
    echo "[$(date)] ERROR: $1" >&2
}

log_warning() {
    echo "[$(date)] WARNING: $1" >&2
}

# Check Qdrant API health endpoint
check_api_health() {
    log "Checking Qdrant API health..."
    
    response=$(curl -s -w "\n%{http_code}" -X GET \
        "http://${QDRANT_HOST}:${QDRANT_PORT}/healthz" \
        --connect-timeout 5 \
        --max-time 10 2>&1)
    
    if [ $? -ne 0 ]; then
        log_error "Cannot connect to Qdrant at ${QDRANT_HOST}:${QDRANT_PORT}"
        return $EXIT_ERROR
    fi
    
    http_code=$(echo "$response" | tail -n1)
    
    if [ "$http_code" -eq 200 ]; then
        log "API health check passed (HTTP ${http_code})"
        return $EXIT_OK
    else
        log_error "API health check failed (HTTP ${http_code})"
        return $EXIT_ERROR
    fi
}

# Check if collection exists
check_collection_exists() {
    log "Checking if collection '${COLLECTION}' exists..."
    
    if [ -n "${QDRANT_API_KEY}" ]; then
        response=$(curl -s -w "\n%{http_code}" -X GET \
            "http://${QDRANT_HOST}:${QDRANT_PORT}/collections/${COLLECTION}" \
            -H "api-key: ${QDRANT_API_KEY}" \
            --connect-timeout 5 \
            --max-time 10 2>&1)
    else
        response=$(curl -s -w "\n%{http_code}" -X GET \
            "http://${QDRANT_HOST}:${QDRANT_PORT}/collections/${COLLECTION}" \
            --connect-timeout 5 \
            --max-time 10 2>&1)
    fi
    
    if [ $? -ne 0 ]; then
        log_error "Cannot query collection"
        return $EXIT_ERROR
    fi
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" -eq 200 ]; then
        log "Collection exists and is accessible"
        
        # Extract metrics if verbose
        if [ "$VERBOSE" = "true" ]; then
            points_count=$(echo "$body" | grep -o '"points_count":[0-9]*' | cut -d':' -f2)
            vectors_count=$(echo "$body" | grep -o '"vectors_count":[0-9]*' | cut -d':' -f2)
            segments_count=$(echo "$body" | grep -o '"segments_count":[0-9]*' | cut -d':' -f2)
            
            log "Collection metrics:"
            log "  - Points: ${points_count:-N/A}"
            log "  - Vectors: ${vectors_count:-N/A}"
            log "  - Segments: ${segments_count:-N/A}"
        fi
        
        return $EXIT_OK
    elif [ "$http_code" -eq 404 ]; then
        log_warning "Collection '${COLLECTION}' does not exist"
        return $EXIT_WARNING
    else
        log_error "Failed to check collection (HTTP ${http_code})"
        return $EXIT_ERROR
    fi
}

# Get cluster info
check_cluster_info() {
    log "Checking cluster information..."
    
    if [ -n "${QDRANT_API_KEY}" ]; then
        response=$(curl -s -w "\n%{http_code}" -X GET \
            "http://${QDRANT_HOST}:${QDRANT_PORT}/cluster" \
            -H "api-key: ${QDRANT_API_KEY}" \
            --connect-timeout 5 \
            --max-time 10 2>&1)
    else
        response=$(curl -s -w "\n%{http_code}" -X GET \
            "http://${QDRANT_HOST}:${QDRANT_PORT}/cluster" \
            --connect-timeout 5 \
            --max-time 10 2>&1)
    fi
    
    if [ $? -ne 0 ]; then
        log_warning "Cannot query cluster info"
        return $EXIT_WARNING
    fi
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" -eq 200 ]; then
        if [ "$VERBOSE" = "true" ]; then
            peer_count=$(echo "$body" | grep -o '"peer_id"' | wc -l)
            log "Cluster info:"
            log "  - Peers: ${peer_count}"
        fi
        return $EXIT_OK
    else
        log_warning "Could not retrieve cluster info (HTTP ${http_code})"
        return $EXIT_WARNING
    fi
}

# Get metrics
get_metrics() {
    log "Retrieving metrics..."
    
    if [ -n "${QDRANT_API_KEY}" ]; then
        response=$(curl -s -w "\n%{http_code}" -X GET \
            "http://${QDRANT_HOST}:${QDRANT_PORT}/metrics" \
            -H "api-key: ${QDRANT_API_KEY}" \
            --connect-timeout 5 \
            --max-time 10 2>&1)
    else
        response=$(curl -s -w "\n%{http_code}" -X GET \
            "http://${QDRANT_HOST}:${QDRANT_PORT}/metrics" \
            --connect-timeout 5 \
            --max-time 10 2>&1)
    fi
    
    http_code=$(echo "$response" | tail -n1)
    
    if [ "$http_code" -eq 200 ] && [ "$VERBOSE" = "true" ]; then
        log "Metrics endpoint is accessible"
    fi
}

# Main health check execution
main() {
    log "=========================================="
    log "Qdrant Health Check"
    log "Host: ${QDRANT_HOST}:${QDRANT_PORT}"
    log "Collection: ${COLLECTION}"
    log "=========================================="
    
    exit_code=$EXIT_OK
    
    # Check API health (critical)
    if ! check_api_health; then
        exit_code=$EXIT_ERROR
    fi
    
    # Check collection exists (critical if exit code still OK)
    if [ $exit_code -eq $EXIT_OK ]; then
        check_collection_exists
        collection_status=$?
        
        if [ $collection_status -eq $EXIT_ERROR ]; then
            exit_code=$EXIT_ERROR
        elif [ $collection_status -eq $EXIT_WARNING ] && [ $exit_code -eq $EXIT_OK ]; then
            exit_code=$EXIT_WARNING
        fi
    fi
    
    # Optional checks (non-critical)
    if [ "$VERBOSE" = "true" ]; then
        check_cluster_info
        get_metrics
    fi
    
    # Final status
    if [ $exit_code -eq $EXIT_OK ]; then
        log "=========================================="
        log "Health check PASSED"
        log "=========================================="
    elif [ $exit_code -eq $EXIT_WARNING ]; then
        log_warning "Health check completed with WARNINGS"
    else
        log_error "Health check FAILED"
    fi
    
    exit $exit_code
}

# Run main function
main
