#!/usr/bin/env pwsh
# <copyright file="rollback-migration.ps1" company="Synaxis">
# Copyright (c) Synaxis. All rights reserved.
# </copyright>

<#
.SYNOPSIS
    Rolls back database migrations to a specified target migration.

.DESCRIPTION
    This script backs up the database before rolling back migrations to a specified target.
    It uses EF Core migrations and PostgreSQL for database operations.

.PARAMETER Migration
    The name of the migration to roll back to. Use "0" to roll back all migrations.

.PARAMETER ConnectionString
    The PostgreSQL connection string. Defaults to local development database.

.PARAMETER SkipBackup
    Skip the database backup step. Not recommended for production.

.EXAMPLE
    .\rollback-migration.ps1 -Migration "InitialMultiTenant"
    Rolls back to the InitialMultiTenant migration.

.EXAMPLE
    .\rollback-migration.ps1 -Migration "0" -ConnectionString "Host=prod;Database=synaxis;Username=postgres;Password=secret"
    Rolls back all migrations on the production database.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true, Position = 0, HelpMessage = "Target migration name or '0' for all")]
    [string]$Migration,

    [Parameter(HelpMessage = "PostgreSQL connection string")]
    [string]$ConnectionString = "Host=localhost;Database=synaxis;Username=postgres;Password=postgres",

    [Parameter(HelpMessage = "Skip database backup")]
    [switch]$SkipBackup,

    [Parameter(HelpMessage = "Backup directory path")]
    [string]$BackupDir = ".\backups"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Colors for output
$Colors = @{
    Info    = "Cyan"
    Success = "Green"
    Warning = "Yellow"
    Error   = "Red"
}

function Write-Status {
    param(
        [string]$Message,
        [string]$Level = "Info"
    )
    Write-Host $Message -ForegroundColor $Colors[$Level]
}

function Test-Command {
    param([string]$Command)
    $null -ne (Get-Command $Command -ErrorAction SilentlyContinue)
}

# Check prerequisites
Write-Status "Checking prerequisites..." "Info"

if (-not (Test-Command "dotnet")) {
    Write-Status "Error: dotnet CLI is not installed or not in PATH" "Error"
    exit 1
}

if (-not (Test-Command "pg_dump")) {
    Write-Status "Warning: pg_dump not found. PostgreSQL tools may not be installed." "Warning"
}

# Get script directory and project paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$InfrastructureProject = Join-Path $RootDir "src\Synaxis.Infrastructure\Synaxis.Infrastructure.csproj"

if (-not (Test-Path $InfrastructureProject)) {
    Write-Status "Error: Infrastructure project not found at $InfrastructureProject" "Error"
    exit 1
}

# Extract database name from connection string
$DatabaseName = "synaxis"
if ($ConnectionString -match "Database=(\w+)") {
    $DatabaseName = $Matches[1]
}

Write-Status "Target database: $DatabaseName" "Info"
Write-Status "Target migration: $Migration" "Info"

# Create backup directory
if (-not $SkipBackup) {
    $BackupPath = Join-Path $BackupDir (Get-Date -Format "yyyyMMdd_HHmmss")
    New-Item -ItemType Directory -Force -Path $BackupPath | Out-Null

    Write-Status "Creating database backup to $BackupPath..." "Info"

    if (Test-Command "pg_dump") {
        try {
            $BackupFile = Join-Path $BackupPath "${DatabaseName}_pre_rollback.sql"

            # Parse connection string for pg_dump
            $Host = "localhost"
            $User = "postgres"

            if ($ConnectionString -match "Host=([^;]+)") { $Host = $Matches[1] }
            if ($ConnectionString -match "Username=([^;]+)") { $User = $Matches[1] }
            if ($ConnectionString -match "User Id=([^;]+)") { $User = $Matches[1] }

            $env:PGPASSWORD = "postgres"
            if ($ConnectionString -match "Password=([^;]+)") { $env:PGPASSWORD = $Matches[1] }

            pg_dump -h $Host -U $User -d $DatabaseName -f $BackupFile --if-exists --clean

            Write-Status "Backup created successfully: $BackupFile" "Success"
        }
        catch {
            Write-Status "Warning: Failed to create PostgreSQL backup: $_" "Warning"
            Write-Status "Continuing without backup..." "Warning"
        }
        finally {
            Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
        }
    }
    else {
        Write-Status "Warning: pg_dump not available. Creating EF Core migration list backup..." "Warning"
        dotnet ef migrations list --project $InfrastructureProject > (Join-Path $BackupPath "migrations_list.txt")
    }
}
else {
    Write-Status "Skipping backup as requested" "Warning"
}

# Confirm rollback
if (-not $PSCmdlet.ShouldProcess("database migration to $Migration", "Rollback")) {
    Write-Status "Rollback cancelled by user" "Warning"
    exit 0
}

# Perform rollback
Write-Status "Rolling back to migration '$Migration'..." "Info"

try {
    dotnet ef database update $Migration --project $InfrastructureProject

    if ($LASTEXITCODE -ne 0) {
        throw "EF Core database update failed with exit code $LASTEXITCODE"
    }

    Write-Status "Successfully rolled back to migration '$Migration'" "Success"
}
catch {
    Write-Status "Error: Failed to roll back migration: $_" "Error"
    exit 1
}

# Verify rollback
Write-Status "Verifying database state..." "Info"

try {
    $Migrations = dotnet ef migrations list --project $InfrastructureProject 2>&1
    Write-Status "Current migrations:" "Info"
    Write-Output $Migrations
}
catch {
    Write-Status "Warning: Could not verify migrations: $_" "Warning"
}

Write-Status "Rollback completed successfully!" "Success"
