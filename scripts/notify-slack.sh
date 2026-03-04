#!/bin/bash
# <copyright file="notify-slack.sh" company="Synaxis">
# Copyright (c) Synaxis. All rights reserved.
# </copyright>

# notify-slack.sh
# Sends rollback notifications to Slack.
#
# Usage:
#   ./notify-slack.sh <severity> <message>
#
# Arguments:
#   severity    P0, P1, P2, or RESOLVED
#   message     Notification message
#
# Environment Variables:
#   SLACK_WEBHOOK_URL    Slack webhook URL (required)
#   GITHUB_REPOSITORY    Repository name for context

set -euo pipefail

# Parse arguments
SEVERITY="${1:-}"
MESSAGE="${2:-}"

if [[ -z "$SEVERITY" || -z "$MESSAGE" ]]; then
    echo "Usage: $0 <severity> <message>"
    echo "Severity: P0, P1, P2, RESOLVED"
    exit 1
fi

# Configuration
WEBHOOK_URL="${SLACK_WEBHOOK_URL:-}"
REPO="${GITHUB_REPOSITORY:-Synaxis}"

if [[ -z "$WEBHOOK_URL" ]]; then
    echo "[ERROR] SLACK_WEBHOOK_URL environment variable not set"
    exit 1
fi

# Determine color based on severity
case "$SEVERITY" in
    P0)
        COLOR="danger"
        TITLE="🚨 P0 Emergency Rollback"
        ;;
    P1)
        COLOR="danger"
        TITLE="⚠️ P1 Critical Rollback"
        ;;
    P2)
        COLOR="warning"
        TITLE="🔶 P2 Standard Rollback"
        ;;
    RESOLVED)
        COLOR="good"
        TITLE="✅ Rollback Complete"
        ;;
    *)
        COLOR="#808080"
        TITLE="Rollback Notification"
        ;;
esac

# Build JSON payload
PAYLOAD=$(cat <<EOF
{
    "attachments": [
        {
            "color": "$COLOR",
            "title": "$TITLE",
            "text": "$MESSAGE",
            "footer": "$REPO Rollback System",
            "ts": $(date +%s),
            "fields": [
                {
                    "title": "Environment",
                    "value": "${ENVIRONMENT:-production}",
                    "short": true
                },
                {
                    "title": "Time",
                    "value": "$(date -Iseconds)",
                    "short": true
                }
            ]
        }
    ]
}
EOF
)

# Send notification
curl -s -X POST "$WEBHOOK_URL" \
    -H 'Content-Type: application/json' \
    -d "$PAYLOAD"

echo "[INFO] Slack notification sent"
