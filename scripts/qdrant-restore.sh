#!/bin/bash
set -e

# Configuration with defaults
QDRANT_HOST="${QDRANT_HOST:-qdrant}"
QDRANT_PORT="${QDRANT_PORT:-6333}"
QDRANT_API_KEY="${QDRANT_API_KEY}"
COLLECTION="${QDRANT_COLLECTION:-synaxis_semantic_cache}"
BACKUP_DIR="${BACKUP_DIR:-/backups}"

# Function to list available snapshots
list_snapshots() {
    echo "Available snapshots in ${BACKUP_DIR}:"
    echo "----------------------------------------"
    
    if [ ! -d "${BACKUP_DIR}" ]; then
        echo "Backup directory does not exist: ${BACKUP_DIR}"
        return 1
    fi
    
    snapshots=$(find "${BACKUP_DIR}" -name 'snapshot_*.snapshot' -type f | sort -r)
    
    if [ -z "$snapshots" ]; then
        echo "No snapshots found"
        return 1
    fi
    
    count=1
    echo "$snapshots" | while read -r snapshot; do
        filename=$(basename "$snapshot")
        size=$(du -h "$snapshot" | cut -f1)
        modified=$(date -r "$snapshot" "+%Y-%m-%d %H:%M:%S")
        echo "${count}. ${filename} (${size}) - ${modified}"
        count=$((count + 1))
    done
    
    return 0
}

# Function to restore a snapshot
restore_snapshot() {
    local snapshot_file=$1
    
    if [ ! -f "${snapshot_file}" ]; then
        echo "[$(date)] ERROR: Snapshot file not found: ${snapshot_file}"
        exit 1
    fi
    
    echo "[$(date)] Starting restoration of snapshot: $(basename ${snapshot_file})"
    echo "[$(date)] Target collection: ${COLLECTION}"
    
    # Upload snapshot to Qdrant
    echo "[$(date)] Uploading snapshot to Qdrant..."
    if [ -n "${QDRANT_API_KEY}" ]; then
        response=$(curl -s -w "\n%{http_code}" -X POST \
            "http://${QDRANT_HOST}:${QDRANT_PORT}/collections/${COLLECTION}/snapshots/upload" \
            -H "api-key: ${QDRANT_API_KEY}" \
            -H "Content-Type: application/octet-stream" \
            --data-binary "@${snapshot_file}")
    else
        response=$(curl -s -w "\n%{http_code}" -X POST \
            "http://${QDRANT_HOST}:${QDRANT_PORT}/collections/${COLLECTION}/snapshots/upload" \
            -H "Content-Type: application/octet-stream" \
            --data-binary "@${snapshot_file}")
    fi
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" -ne 200 ] && [ "$http_code" -ne 201 ]; then
        echo "[$(date)] ERROR: Failed to upload snapshot. HTTP code: ${http_code}"
        echo "[$(date)] Response: ${body}"
        exit 1
    fi
    
    echo "[$(date)] Snapshot uploaded successfully"
    
    # Wait a moment for restoration to complete
    echo "[$(date)] Waiting for restoration to complete..."
    sleep 2
    
    # Validate restoration
    echo "[$(date)] Validating restoration..."
    if [ -n "${QDRANT_API_KEY}" ]; then
        collection_info=$(curl -s -X GET \
            "http://${QDRANT_HOST}:${QDRANT_PORT}/collections/${COLLECTION}" \
            -H "api-key: ${QDRANT_API_KEY}")
    else
        collection_info=$(curl -s -X GET \
            "http://${QDRANT_HOST}:${QDRANT_PORT}/collections/${COLLECTION}")
    fi
    
    # Extract points count from response
    points_count=$(echo "$collection_info" | grep -o '"points_count":[0-9]*' | cut -d':' -f2)
    
    if [ -n "$points_count" ]; then
        echo "[$(date)] Validation successful!"
        echo "[$(date)] Collection ${COLLECTION} has ${points_count} points"
        echo "[$(date)] Restoration completed successfully"
        return 0
    else
        echo "[$(date)] WARNING: Could not validate collection state"
        echo "[$(date)] Collection response: ${collection_info}"
        return 1
    fi
}

# Main script logic
echo "==================================="
echo "Qdrant Snapshot Restoration Tool"
echo "==================================="
echo ""

# Check if snapshot file was provided as argument
if [ -n "$1" ]; then
    # Direct restore with provided path
    if [ "$1" = "-l" ] || [ "$1" = "--list" ]; then
        list_snapshots
        exit 0
    elif [ -f "$1" ]; then
        restore_snapshot "$1"
        exit 0
    elif [ -f "${BACKUP_DIR}/$1" ]; then
        restore_snapshot "${BACKUP_DIR}/$1"
        exit 0
    else
        echo "ERROR: Snapshot file not found: $1"
        exit 1
    fi
fi

# Interactive mode - list and select
if ! list_snapshots; then
    exit 1
fi

echo ""
echo "Enter the snapshot filename to restore (or 'q' to quit):"
read -r selection

if [ "$selection" = "q" ] || [ -z "$selection" ]; then
    echo "Restoration cancelled"
    exit 0
fi

# Check if selection is a full path or just filename
if [ -f "$selection" ]; then
    restore_snapshot "$selection"
elif [ -f "${BACKUP_DIR}/${selection}" ]; then
    restore_snapshot "${BACKUP_DIR}/${selection}"
else
    echo "ERROR: Invalid selection: ${selection}"
    exit 1
fi
