#!/bin/bash
set -e

# Configuration with defaults
QDRANT_HOST="${QDRANT_HOST:-qdrant}"
QDRANT_PORT="${QDRANT_PORT:-6333}"
QDRANT_API_KEY="${QDRANT_API_KEY}"
COLLECTION="${QDRANT_COLLECTION:-synaxis_semantic_cache}"
BACKUP_DIR="${BACKUP_DIR:-/backups}"
RETENTION_HOURS="${RETENTION_HOURS:-24}"

# Create backup directory if it doesn't exist
mkdir -p "${BACKUP_DIR}"

# Generate timestamp
timestamp=$(date +%Y%m%d_%H%M%S)
snapshot_name="snapshot_${timestamp}"
backup_file="${BACKUP_DIR}/${snapshot_name}.snapshot"

echo "[$(date)] Starting Qdrant backup for collection: ${COLLECTION}"
echo "[$(date)] Backup directory: ${BACKUP_DIR}"
echo "[$(date)] Retention policy: ${RETENTION_HOURS} hours"

# Create snapshot via Qdrant API
echo "[$(date)] Creating snapshot..."
if [ -n "${QDRANT_API_KEY}" ]; then
    response=$(curl -s -w "\n%{http_code}" -X POST \
        "http://${QDRANT_HOST}:${QDRANT_PORT}/collections/${COLLECTION}/snapshots" \
        -H "api-key: ${QDRANT_API_KEY}" \
        -H "Content-Type: application/json")
else
    response=$(curl -s -w "\n%{http_code}" -X POST \
        "http://${QDRANT_HOST}:${QDRANT_PORT}/collections/${COLLECTION}/snapshots" \
        -H "Content-Type: application/json")
fi

http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | sed '$d')

if [ "$http_code" -ne 200 ] && [ "$http_code" -ne 201 ]; then
    echo "[$(date)] ERROR: Failed to create snapshot. HTTP code: ${http_code}"
    echo "[$(date)] Response: ${body}"
    exit 1
fi

# Extract snapshot name from response
snapshot_api_name=$(echo "$body" | grep -o '"name":"[^"]*"' | cut -d'"' -f4)
if [ -z "$snapshot_api_name" ]; then
    echo "[$(date)] ERROR: Could not extract snapshot name from API response"
    exit 1
fi

echo "[$(date)] Snapshot created: ${snapshot_api_name}"

# Download the snapshot
echo "[$(date)] Downloading snapshot to ${backup_file}..."
if [ -n "${QDRANT_API_KEY}" ]; then
    curl -s -X GET \
        "http://${QDRANT_HOST}:${QDRANT_PORT}/collections/${COLLECTION}/snapshots/${snapshot_api_name}" \
        -H "api-key: ${QDRANT_API_KEY}" \
        -o "${backup_file}"
else
    curl -s -X GET \
        "http://${QDRANT_HOST}:${QDRANT_PORT}/collections/${COLLECTION}/snapshots/${snapshot_api_name}" \
        -o "${backup_file}"
fi

# Verify backup file was created
if [ ! -f "${backup_file}" ]; then
    echo "[$(date)] ERROR: Backup file was not created"
    exit 1
fi

backup_size=$(du -h "${backup_file}" | cut -f1)
echo "[$(date)] Backup downloaded successfully: ${backup_file} (${backup_size})"

# Cleanup old backups based on retention policy
echo "[$(date)] Cleaning up backups older than ${RETENTION_HOURS} hours..."
retention_minutes=$((RETENTION_HOURS * 60))
deleted_count=$(find "${BACKUP_DIR}" -name 'snapshot_*.snapshot' -mmin +${retention_minutes} -type f | wc -l)

if [ "$deleted_count" -gt 0 ]; then
    find "${BACKUP_DIR}" -name 'snapshot_*.snapshot' -mmin +${retention_minutes} -type f -delete
    echo "[$(date)] Deleted ${deleted_count} old backup(s)"
else
    echo "[$(date)] No old backups to clean up"
fi

# List current backups
backup_count=$(find "${BACKUP_DIR}" -name 'snapshot_*.snapshot' -type f | wc -l)
echo "[$(date)] Total backups in directory: ${backup_count}"

echo "[$(date)] Backup completed successfully: ${snapshot_name}.snapshot"
