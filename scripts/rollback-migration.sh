#!/bin/bash
# <copyright file="rollback-migration.sh" company="Synaxis">
# Copyright (c) Synaxis. All rights reserved.
# </copyright>

# rollback-migration.sh
# Rolls back database migrations to a specified target migration.
#
# Usage:
#   ./rollback-migration.sh <migration_name>
#   ./rollback-migration.sh 0  # Roll back all migrations
#
# Environment Variables:
#   CONNECTION_STRING - PostgreSQL connection string (default: Host=localhost;Database=synaxis;Username=postgres;Password=postgres)
#   SKIP_BACKUP       - Set to "true" to skip backup (not recommended for production)
#   BACKUP_DIR        - Directory for backups (default: ./backups)

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

# Check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Show usage
usage() {
    cat << EOF
Usage: $(basename "$0") <migration_name>

Rolls back database migrations to a specified target migration.

Arguments:
  migration_name    Target migration name, or '0' to roll back all migrations

Options:
  -h, --help        Show this help message
  -s, --skip-backup Skip database backup (not recommended)
  -c, --connection  PostgreSQL connection string

Environment Variables:
  CONNECTION_STRING   PostgreSQL connection string
  SKIP_BACKUP         Set to "true" to skip backup
  BACKUP_DIR          Directory for backups (default: ./backups)

Examples:
  $(basename "$0") InitialMultiTenant
  $(basename "$0") 0
  CONNECTION_STRING="Host=prod;Database=synaxis;Username=postgres;Password=secret" $(basename "$0") InitialMultiTenant
EOF
}

# Parse command line arguments
SKIP_BACKUP_FLAG=false
CONNECTION_STRING_ARG=""

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            usage
            exit 0
            ;;
        -s|--skip-backup)
            SKIP_BACKUP_FLAG=true
            shift
            ;;
        -c|--connection)
            CONNECTION_STRING_ARG="$2"
            shift 2
            ;;
        -*)
            log_error "Unknown option: $1"
            usage
            exit 1
            ;;
        *)
            MIGRATION="$1"
            shift
            ;;
    esac
done

# Validate arguments
if [[ -z "${MIGRATION:-}" ]]; then
    log_error "Migration name is required"
    usage
    exit 1
fi

# Set variables
CONNECTION_STRING="${CONNECTION_STRING_ARG:-${CONNECTION_STRING:-"Host=localhost;Database=synaxis;Username=postgres;Password=postgres"}}"
SKIP_BACKUP="${SKIP_BACKUP:-false}"
if [[ "$SKIP_BACKUP_FLAG" == true ]]; then
    SKIP_BACKUP="true"
fi
BACKUP_DIR="${BACKUP_DIR:-"./backups"}"

# Get script directory and project paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
INFRASTRUCTURE_PROJECT="$ROOT_DIR/src/Synaxis.Infrastructure/Synaxis.Infrastructure.csproj"

log_info "Starting migration rollback..."
log_info "Target migration: $MIGRATION"

# Check prerequisites
log_info "Checking prerequisites..."

if ! command_exists dotnet; then
    log_error "dotnet CLI is not installed or not in PATH"
    exit 1
fi

if ! command_exists pg_dump; then
    log_warning "pg_dump not found. PostgreSQL tools may not be installed."
fi

if [[ ! -f "$INFRASTRUCTURE_PROJECT" ]]; then
    log_error "Infrastructure project not found at $INFRASTRUCTURE_PROJECT"
    exit 1
fi

# Extract database name from connection string
DATABASE_NAME="synaxis"
if [[ "$CONNECTION_STRING" =~ Database=([^;]+) ]]; then
    DATABASE_NAME="${BASH_REMATCH[1]}"
fi

log_info "Target database: $DATABASE_NAME"

# Create backup
if [[ "$SKIP_BACKUP" != "true" ]]; then
    BACKUP_PATH="$BACKUP_DIR/$(date +%Y%m%d_%H%M%S)"
    mkdir -p "$BACKUP_PATH"

    log_info "Creating database backup to $BACKUP_PATH..."

    if command_exists pg_dump; then
        # Parse connection string
        DB_HOST="localhost"
        DB_USER="postgres"
        DB_PASS="postgres"

        if [[ "$CONNECTION_STRING" =~ Host=([^;]+) ]]; then
            DB_HOST="${BASH_REMATCH[1]}"
        fi
        if [[ "$CONNECTION_STRING" =~ Username=([^;]+) ]]; then
            DB_USER="${BASH_REMATCH[1]}"
        elif [[ "$CONNECTION_STRING" =~ "User Id"=([^;]+) ]]; then
            DB_USER="${BASH_REMATCH[1]}"
        fi
        if [[ "$CONNECTION_STRING" =~ Password=([^;]+) ]]; then
            DB_PASS="${BASH_REMATCH[1]}"
        fi

        BACKUP_FILE="$BACKUP_PATH/${DATABASE_NAME}_pre_rollback.sql"
        export PGPASSWORD="$DB_PASS"

        if pg_dump -h "$DB_HOST" -U "$DB_USER" -d "$DATABASE_NAME" -f "$BACKUP_FILE" --if-exists --clean 2>/dev/null; then
            log_success "Backup created successfully: $BACKUP_FILE"
        else
            log_warning "Failed to create PostgreSQL backup"
            log_warning "Continuing without backup..."
        fi

        unset PGPASSWORD
    else
        log_warning "pg_dump not available. Creating EF Core migration list backup..."
        dotnet ef migrations list --project "$INFRASTRUCTURE_PROJECT" > "$BACKUP_PATH/migrations_list.txt" 2>/dev/null || true
    fi
else
    log_warning "Skipping backup as requested"
fi

# Confirm rollback (interactive mode)
if [[ -t 0 ]]; then
    echo ""
    read -p "Are you sure you want to roll back to migration '$MIGRATION'? [y/N] " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        log_warning "Rollback cancelled by user"
        exit 0
    fi
fi

# Perform rollback
log_info "Rolling back to migration '$MIGRATION'..."

if ! dotnet ef database update "$MIGRATION" --project "$INFRASTRUCTURE_PROJECT"; then
    log_error "Failed to roll back migration"
    exit 1
fi

log_success "Successfully rolled back to migration '$MIGRATION'"

# Verify rollback
log_info "Verifying database state..."

if MIGRATIONS=$(dotnet ef migrations list --project "$INFRASTRUCTURE_PROJECT" 2>/dev/null); then
    log_info "Current migrations:"
    echo "$MIGRATIONS"
else
    log_warning "Could not verify migrations"
fi

log_success "Rollback completed successfully!"
