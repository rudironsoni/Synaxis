#requires -Version 5.1
<#
.SYNOPSIS
    Synaxis Database Migration Runner
.DESCRIPTION
    Executes SQL migration scripts in order and tracks applied migrations.
    Supports PostgreSQL databases.
.PARAMETER Server
    PostgreSQL server hostname (default: localhost)
.PARAMETER Port
    PostgreSQL server port (default: 5432)
.PARAMETER Database
    Database name (default: synaxis)
.PARAMETER Username
    PostgreSQL username
.PARAMETER Password
    PostgreSQL password
.PARAMETER Schema
    Schema to use (default: public)
.PARAMETER MigrationsPath
    Path to migration scripts folder (default: ./)
.PARAMETER TargetVersion
    Target migration version to run up to (default: all)
.PARAMETER DryRun
    Show what would be executed without running migrations
.PARAMETER Force
    Force re-run of already applied migrations (use with caution!)
.EXAMPLE
    .\run_migrations.ps1 -Database synaxis_prod -Username postgres -Password secret123
.EXAMPLE
    .\run_migrations.ps1 -TargetVersion 003 -DryRun
#>
[CmdletBinding()]
param(
    [string]$Server = "localhost",
    [int]$Port = 5432,
    [string]$Database = "synaxis",
    [string]$Username = "",
    [string]$Password = "",
    [string]$Schema = "public",
    [string]$MigrationsPath = $PSScriptRoot,
    [string]$TargetVersion = "",
    [switch]$DryRun,
    [switch]$Force
)

# Script version
$ScriptVersion = "1.0.0"

Write-Host @"
========================================
Synaxis Migration Runner v$ScriptVersion
========================================
"@ -ForegroundColor Cyan

# Validate parameters
if ([string]::IsNullOrEmpty($Username)) {
    Write-Error "Username is required. Use -Username parameter."
    exit 1
}

# Build connection string
$env:PGPASSWORD = $Password
$psqlBaseArgs = @(
    "-h", $Server,
    "-p", $Port,
    "-U", $Username,
    "-d", $Database,
    "-v", "ON_ERROR_STOP=1",
    "--quiet"
)

# Function to execute SQL file
function Invoke-SqlFile {
    param(
        [string]$FilePath,
        [string]$Description = ""
    )
    
    if ($DryRun) {
        Write-Host "[DRY RUN] Would execute: $FilePath" -ForegroundColor Yellow
        return $true
    }
    
    Write-Host "Executing: $Description" -ForegroundColor Blue -NoNewline
    
    try {
        $output = & psql @psqlBaseArgs -f $FilePath 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host " [OK]" -ForegroundColor Green
            return $true
        } else {
            Write-Host " [FAILED]" -ForegroundColor Red
            Write-Error "Migration failed: $output"
            return $false
        }
    }
    catch {
        Write-Host " [ERROR]" -ForegroundColor Red
        Write-Error "Exception: $_"
        return $false
    }
}

# Function to get applied migrations
function Get-AppliedMigrations {
    $query = "SELECT version FROM schema_migrations ORDER BY version;"
    $result = & psql @psqlBaseArgs -t -c $query 2>&1
    return $result | Where-Object { $_.Trim() -ne '' } | ForEach-Object { $_.Trim() }
}

# Function to get migration files
function Get-MigrationFiles {
    param([string]$Path)
    
    return Get-ChildItem -Path $Path -Filter "*.sql" | 
        Where-Object { $_.Name -match '^\d{3}_.*\.sql$' } |
        Sort-Object Name |
        ForEach-Object {
            $version = ($_.Name -split '_')[0]
            [PSCustomObject]@{
                Version = $version
                FileName = $_.Name
                FullPath = $_.FullName
                Description = ($_.Name -replace '^\d{3}_', '' -replace '\.sql$', '')
            }
        }
}

# Ensure schema_migrations table exists
Write-Host "Checking schema_migrations table..." -ForegroundColor Blue
$checkTable = & psql @psqlBaseArgs -t -c "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_name = 'schema_migrations');" 2>&1

if ($checkTable -notmatch 't') {
    Write-Host "Creating schema_migrations table..." -ForegroundColor Yellow
    $initFile = Join-Path $MigrationsPath "000_CreateSchemaMigrationsTable.sql"
    if (Test-Path $initFile) {
        Invoke-SqlFile -FilePath $initFile -Description "000: CreateSchemaMigrationsTable"
    } else {
        Write-Error "Migration tracking table script not found: $initFile"
        exit 1
    }
}

# Get list of applied migrations
$appliedMigrations = Get-AppliedMigrations
Write-Host "Applied migrations: $($appliedMigrations -join ', ')" -ForegroundColor Gray

# Get all migration files
$migrationFiles = Get-MigrationFiles -Path $MigrationsPath

if ($migrationFiles.Count -eq 0) {
    Write-Warning "No migration files found in $MigrationsPath"
    exit 0
}

Write-Host "Found $($migrationFiles.Count) migration files" -ForegroundColor Gray

# Determine which migrations to run
$migrationsToRun = @()
foreach ($migration in $migrationFiles) {
    $shouldRun = $false
    
    if ($Force) {
        $shouldRun = $true
    } elseif ($appliedMigrations -notcontains $migration.Version) {
        $shouldRun = $true
    }
    
    if ($TargetVersion -and $migration.Version -gt $TargetVersion) {
        $shouldRun = $false
    }
    
    if ($shouldRun) {
        $migrationsToRun += $migration
    }
}

if ($migrationsToRun.Count -eq 0) {
    Write-Host "No migrations to run. Database is up to date!" -ForegroundColor Green
    exit 0
}

Write-Host "Migrations to execute: $($migrationsToRun.Count)" -ForegroundColor Cyan

# Execute migrations
$successCount = 0
$failCount = 0

foreach ($migration in $migrationsToRun) {
    $isReRun = $appliedMigrations -contains $migration.Version
    $action = if ($isReRun) { "RE-RUNNING" } else { "RUNNING" }
    
    Write-Host "`n$action: $($migration.Version) - $($migration.Description)" -ForegroundColor $(if ($isReRun) { "Magenta" } else { "Cyan" })
    
    $startTime = Get-Date
    $result = Invoke-SqlFile -FilePath $migration.FullPath -Description "$($migration.Version): $($migration.Description)"
    $endTime = Get-Date
    
    if ($result) {
        $successCount++
        $duration = [math]::Round(($endTime - $startTime).TotalMilliseconds)
        
        if (-not $DryRun) {
            # Update execution time
            $updateQuery = "UPDATE schema_migrations SET execution_time_ms = $duration WHERE version = '$($migration.Version)';"
            & psql @psqlBaseArgs -c $updateQuery | Out-Null
        }
    } else {
        $failCount++
        Write-Error "Migration $($migration.Version) failed. Stopping."
        break
    }
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "MIGRATION SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Successful: $successCount" -ForegroundColor Green
if ($failCount -gt 0) {
    Write-Host "Failed: $failCount" -ForegroundColor Red
}

if ($DryRun) {
    Write-Host "`n[DRY RUN - No changes were made]" -ForegroundColor Yellow
}

Write-Host "`nCurrent schema version: $(($appliedMigrations + $migrationsToRun.Version | Sort-Object -Unique | Select-Object -Last 1))" -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan

exit $failCount
