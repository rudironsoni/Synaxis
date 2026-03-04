#!/usr/bin/env pwsh
# <copyright file="run-migration-rehearsal.ps1" company="Synaxis">
# Copyright (c) Synaxis. All rights reserved.
# </copyright>

<#
.SYNOPSIS
    Executes migration rehearsals in staging to validate procedures.

.DESCRIPTION
    This script orchestrates comprehensive migration rehearsals including:
    - Happy Path Rehearsal: Complete migration runbook execution
    - Failure Scenarios: Database migration failures and rollback testing
    - Partial Failures: Service degradation and network issues
    - Performance Baselines: Load testing before/after migration

.PARAMETER Environment
    Target environment for the rehearsal (default: staging).

.PARAMETER Scenario
    Specific scenario to run: happy-path, failure, partial, performance, or all.

.PARAMETER OutputDirectory
    Directory for rehearsal results (default: ./rehearsal-results).

.PARAMETER ConnectionString
    Database connection string for the target environment.

.PARAMETER VerboseOutput
    Enable verbose output for detailed logging.

.EXAMPLE
    .\run-migration-rehearsal.ps1
    Runs all rehearsal scenarios in the staging environment.

.EXAMPLE
    .\run-migration-rehearsal.ps1 -Scenario happy-path -VerboseOutput
    Runs only the happy path rehearsal with verbose output.

.EXAMPLE
    .\run-migration-rehearsal.ps1 -Environment production -ConnectionString "Host=prod-db;..."
    Runs all scenarios against the production environment.
#>

[CmdletBinding()]
param(
    [Parameter(HelpMessage = "Target environment")]
    [string]$Environment = $env:REHEARSAL_ENVIRONMENT ?? "staging",

    [Parameter(HelpMessage = "Scenario to run")]
    [ValidateSet("happy-path", "failure", "partial", "performance", "all")]
    [string]$Scenario = $env:REHEARSAL_SCENARIO ?? "all",

    [Parameter(HelpMessage = "Output directory")]
    [string]$OutputDirectory = $env:REHEARSAL_OUTPUT_DIR ?? "./rehearsal-results",

    [Parameter(HelpMessage = "Database connection string")]
    [string]$ConnectionString = $env:REHEARSAL_CONNECTION_STRING ?? "",

    [Parameter(HelpMessage = "Enable verbose output")]
    [switch]$VerboseOutput = [bool]::Parse($env:REHEARSAL_VERBOSE ?? "false")
)

# Script metadata
$ScriptVersion = "1.0.0"
$ScriptName = "run-migration-rehearsal"

# Colors for output
$Colors = @{
    Info = "Cyan"
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
}

function Write-Status {
    param(
        [string]$Message,
        [string]$Level = "Info"
    )
    Write-Host $Message -ForegroundColor $Colors[$Level]
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "==============================================" -ForegroundColor $Colors.Info
    Write-Host $Title -ForegroundColor $Colors.Info
    Write-Host "==============================================" -ForegroundColor $Colors.Info
}

# Get script paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$InfrastructureProject = Join-Path $RootDir "src\Synaxis.Infrastructure\Synaxis.Infrastructure.csproj"
$TestProject = Join-Path $RootDir "tests\Synaxis.Infrastructure.UnitTests\Synaxis.Infrastructure.UnitTests.csproj"

function Test-Prerequisites {
    Write-Section "CHECKING PREREQUISITES"

    $failed = $false

    # Check dotnet CLI
    try {
        $dotnetVersion = dotnet --version 2>$null
        if ($LASTEXITCODE -ne 0) { throw "dotnet not found" }
        Write-Status "dotnet CLI: $dotnetVersion" "Info"
    }
    catch {
        Write-Status "ERROR: dotnet CLI is not installed or not in PATH" "Error"
        $failed = $true
    }

    # Check PostgreSQL tools
    try {
        $pgDumpVersion = pg_dump --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Status "pg_dump: Available" "Info"
        }
        else {
            Write-Status "WARNING: pg_dump not found - database backup may be limited" "Warning"
        }
    }
    catch {
        Write-Status "WARNING: pg_dump not found - database backup may be limited" "Warning"
    }

    # Check infrastructure project
    if (-not (Test-Path $InfrastructureProject)) {
        Write-Status "ERROR: Infrastructure project not found: $InfrastructureProject" "Error"
        $failed = $true
    }
    else {
        Write-Status "Infrastructure project: $InfrastructureProject" "Info"
    }

    # Check test project
    if (-not (Test-Path $TestProject)) {
        Write-Status "ERROR: Test project not found: $TestProject" "Error"
        $failed = $true
    }
    else {
        Write-Status "Test project: $TestProject" "Info"
    }

    if ($failed) {
        Write-Status "Prerequisite check failed" "Error"
        exit 1
    }

    Write-Status "All prerequisites satisfied" "Success"
}

function Invoke-HappyPathRehearsal {
    Write-Section "HAPPY PATH REHEARSAL"

    Write-Status "Executing complete migration runbook..." "Info"
    Write-Status "Environment: $Environment" "Info"
    Write-Status "Output directory: $OutputDirectory" "Info"

    $startTime = Get-Date

    $dotnetArgs = @(
        "test", $TestProject,
        "--filter", "FullyQualifiedName~MigrationRehearsalTests.HappyPathRehearsal_ExecutesAllSteps",
        "--results-directory", $OutputDirectory
    )

    if ($VerboseOutput) {
        $dotnetArgs += @("--logger", "console;verbosity=detailed")
    }

    $outputLog = Join-Path $OutputDirectory "happy-path-output.log"
    $result = & dotnet @dotnetArgs 2>&1
    $result | Out-File $outputLog -Encoding UTF8

    $exitCode = $LASTEXITCODE
    $endTime = Get-Date
    $duration = [math]::Round(($endTime - $startTime).TotalSeconds, 2)

    $resultFile = Join-Path $OutputDirectory "happy-path-result.json"

    if ($exitCode -eq 0) {
        Write-Status "Happy path rehearsal completed successfully in ${duration}s" "Success"
        @{ scenario = "happy-path"; status = "passed"; duration = $duration } | ConvertTo-Json | Set-Content $resultFile
        return 0
    }
    else {
        Write-Status "Happy path rehearsal failed after ${duration}s" "Error"
        @{ scenario = "happy-path"; status = "failed"; duration = $duration } | ConvertTo-Json | Set-Content $resultFile
        return 1
    }
}

function Invoke-FailureScenarioRehearsal {
    Write-Section "FAILURE SCENARIO REHEARSAL"

    Write-Status "Simulating database migration failures..." "Info"
    Write-Status "Testing rollback procedures..." "Info"

    $startTime = Get-Date

    $dotnetArgs = @(
        "test", $TestProject,
        "--filter", "FullyQualifiedName~MigrationRehearsalTests.FailureScenarioRehearsal_VerifiesRollback",
        "--results-directory", $OutputDirectory
    )

    if ($VerboseOutput) {
        $dotnetArgs += @("--logger", "console;verbosity=detailed")
    }

    $outputLog = Join-Path $OutputDirectory "failure-scenario-output.log"
    $result = & dotnet @dotnetArgs 2>&1
    $result | Out-File $outputLog -Encoding UTF8

    $exitCode = $LASTEXITCODE
    $endTime = Get-Date
    $duration = [math]::Round(($endTime - $startTime).TotalSeconds, 2)

    $resultFile = Join-Path $OutputDirectory "failure-scenario-result.json"

    if ($exitCode -eq 0) {
        Write-Status "Failure scenario rehearsal completed successfully in ${duration}s" "Success"
        @{ scenario = "failure"; status = "passed"; duration = $duration } | ConvertTo-Json | Set-Content $resultFile
        return 0
    }
    else {
        Write-Status "Failure scenario rehearsal failed after ${duration}s" "Error"
        @{ scenario = "failure"; status = "failed"; duration = $duration } | ConvertTo-Json | Set-Content $resultFile
        return 1
    }
}

function Invoke-PartialFailureRehearsal {
    Write-Section "PARTIAL FAILURE REHEARSAL"

    Write-Status "Simulating service failures during rollout..." "Info"
    Write-Status "Testing graceful degradation..." "Info"

    $startTime = Get-Date

    $dotnetArgs = @(
        "test", $TestProject,
        "--filter", "FullyQualifiedName~MigrationRehearsalTests.PartialFailureRehearsal_VerifiesGracefulDegradation",
        "--results-directory", $OutputDirectory
    )

    if ($VerboseOutput) {
        $dotnetArgs += @("--logger", "console;verbosity=detailed")
    }

    $outputLog = Join-Path $OutputDirectory "partial-failure-output.log"
    $result = & dotnet @dotnetArgs 2>&1
    $result | Out-File $outputLog -Encoding UTF8

    $exitCode = $LASTEXITCODE
    $endTime = Get-Date
    $duration = [math]::Round(($endTime - $startTime).TotalSeconds, 2)

    $resultFile = Join-Path $OutputDirectory "partial-failure-result.json"

    if ($exitCode -eq 0) {
        Write-Status "Partial failure rehearsal completed successfully in ${duration}s" "Success"
        @{ scenario = "partial"; status = "passed"; duration = $duration } | ConvertTo-Json | Set-Content $resultFile
        return 0
    }
    else {
        Write-Status "Partial failure rehearsal failed after ${duration}s" "Error"
        @{ scenario = "partial"; status = "failed"; duration = $duration } | ConvertTo-Json | Set-Content $resultFile
        return 1
    }
}

function Invoke-PerformanceBaselineRehearsal {
    Write-Section "PERFORMANCE BASELINE REHEARSAL"

    Write-Status "Establishing performance baselines..." "Info"
    Write-Status "Comparing pre/post migration metrics..." "Info"

    $startTime = Get-Date

    $dotnetArgs = @(
        "test", $TestProject,
        "--filter", "FullyQualifiedName~MigrationRehearsalTests.PerformanceBaselineRehearsal_CapturesMetrics",
        "--results-directory", $OutputDirectory
    )

    if ($VerboseOutput) {
        $dotnetArgs += @("--logger", "console;verbosity=detailed")
    }

    $outputLog = Join-Path $OutputDirectory "performance-baseline-output.log"
    $result = & dotnet @dotnetArgs 2>&1
    $result | Out-File $outputLog -Encoding UTF8

    $exitCode = $LASTEXITCODE
    $endTime = Get-Date
    $duration = [math]::Round(($endTime - $startTime).TotalSeconds, 2)

    $resultFile = Join-Path $OutputDirectory "performance-baseline-result.json"

    if ($exitCode -eq 0) {
        Write-Status "Performance baseline rehearsal completed successfully in ${duration}s" "Success"
        @{ scenario = "performance"; status = "passed"; duration = $duration } | ConvertTo-Json | Set-Content $resultFile
        return 0
    }
    else {
        Write-Status "Performance baseline rehearsal failed after ${duration}s" "Error"
        @{ scenario = "performance"; status = "failed"; duration = $duration } | ConvertTo-Json | Set-Content $resultFile
        return 1
    }
}

function Invoke-FullRehearsal {
    Write-Section "FULL MIGRATION REHEARSAL"

    Write-Status "Executing complete migration rehearsal suite..." "Info"
    Write-Status "This includes: Happy Path, Failure Scenarios, Partial Failure, Performance Baseline" "Info"

    $startTime = Get-Date

    $dotnetArgs = @(
        "test", $TestProject,
        "--filter", "FullyQualifiedName~MigrationRehearsalTests",
        "--results-directory", $OutputDirectory
    )

    if ($VerboseOutput) {
        $dotnetArgs += @("--logger", "console;verbosity=detailed")
    }

    $outputLog = Join-Path $OutputDirectory "full-rehearsal-output.log"
    $result = & dotnet @dotnetArgs 2>&1
    $result | Out-File $outputLog -Encoding UTF8

    $exitCode = $LASTEXITCODE
    $endTime = Get-Date
    $duration = [math]::Round(($endTime - $startTime).TotalSeconds, 2)

    $resultFile = Join-Path $OutputDirectory "full-rehearsal-result.json"

    if ($exitCode -eq 0) {
        Write-Status "Full rehearsal completed successfully in ${duration}s" "Success"
        @{ scenario = "full"; status = "passed"; duration = $duration } | ConvertTo-Json | Set-Content $resultFile
        return 0
    }
    else {
        Write-Status "Full rehearsal failed after ${duration}s" "Error"
        @{ scenario = "full"; status = "failed"; duration = $duration } | ConvertTo-Json | Set-Content $resultFile
        return 1
    }
}

function New-SummaryReport {
    Write-Section "GENERATING SUMMARY REPORT"

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $reportFile = Join-Path $OutputDirectory "rehearsal-summary-$timestamp.md"

    $reportContent = @"
# Migration Rehearsal Summary Report

**Generated:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC")  
**Environment:** $Environment  
**Scenario:** $Scenario

## Execution Summary

"@

    # Add results for each scenario
    $happyPathResult = Join-Path $OutputDirectory "happy-path-result.json"
    if (Test-Path $happyPathResult) {
        $result = Get-Content $happyPathResult | ConvertFrom-Json
        $reportContent += @"
### Happy Path Rehearsal
- Status: $($result.status)
- Duration: $($result.duration)s

"@
    }

    $failureResult = Join-Path $OutputDirectory "failure-scenario-result.json"
    if (Test-Path $failureResult) {
        $result = Get-Content $failureResult | ConvertFrom-Json
        $reportContent += @"
### Failure Scenario Rehearsal
- Status: $($result.status)
- Duration: $($result.duration)s

"@
    }

    $partialResult = Join-Path $OutputDirectory "partial-failure-result.json"
    if (Test-Path $partialResult) {
        $result = Get-Content $partialResult | ConvertFrom-Json
        $reportContent += @"
### Partial Failure Rehearsal
- Status: $($result.status)
- Duration: $($result.duration)s

"@
    }

    $performanceResult = Join-Path $OutputDirectory "performance-baseline-result.json"
    if (Test-Path $performanceResult) {
        $result = Get-Content $performanceResult | ConvertFrom-Json
        $reportContent += @"
### Performance Baseline Rehearsal
- Status: $($result.status)
- Duration: $($result.duration)s

"@
    }

    $reportContent += @"

## Output Files

- Happy Path Log: ``happy-path-output.log``
- Failure Scenario Log: ``failure-scenario-output.log``
- Partial Failure Log: ``partial-failure-output.log``
- Performance Baseline Log: ``performance-baseline-output.log``

## Next Steps

1. Review all log files for detailed results
2. Verify Go/No-Go decision criteria
3. Update runbook with lessons learned
4. Schedule stakeholder review

"@

    $reportContent | Set-Content $reportFile -Encoding UTF8
    Write-Status "Summary report generated: $reportFile" "Success"
}

# Main execution
Write-Section "MIGRATION REHEARSAL TOOL v$ScriptVersion"

Write-Status "Environment: $Environment" "Info"
Write-Status "Scenario: $Scenario" "Info"
Write-Status "Output Directory: $OutputDirectory" "Info"

# Create output directory
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

# Check prerequisites
Test-Prerequisites

# Track overall success
$overallSuccess = $true

# Run requested scenarios
switch ($Scenario) {
    "happy-path" {
        $exitCode = Invoke-HappyPathRehearsal
        if ($exitCode -ne 0) { $overallSuccess = $false }
    }
    "failure" {
        $exitCode = Invoke-FailureScenarioRehearsal
        if ($exitCode -ne 0) { $overallSuccess = $false }
    }
    "partial" {
        $exitCode = Invoke-PartialFailureRehearsal
        if ($exitCode -ne 0) { $overallSuccess = $false }
    }
    "performance" {
        $exitCode = Invoke-PerformanceBaselineRehearsal
        if ($exitCode -ne 0) { $overallSuccess = $false }
    }
    "all" {
        $exitCode = Invoke-HappyPathRehearsal
        if ($exitCode -ne 0) { $overallSuccess = $false }

        $exitCode = Invoke-FailureScenarioRehearsal
        if ($exitCode -ne 0) { $overallSuccess = $false }

        $exitCode = Invoke-PartialFailureRehearsal
        if ($exitCode -ne 0) { $overallSuccess = $false }

        $exitCode = Invoke-PerformanceBaselineRehearsal
        if ($exitCode -ne 0) { $overallSuccess = $false }
    }
    default {
        Write-Status "Unknown scenario: $Scenario" "Error"
        exit 1
    }
}

# Generate summary report
New-SummaryReport

# Final status
Write-Section "REHEARSAL COMPLETE"

if ($overallSuccess) {
    Write-Status "All requested rehearsals completed successfully" "Success"
    Write-Status "Results available in: $OutputDirectory" "Info"
    exit 0
}
else {
    Write-Status "One or more rehearsals failed" "Error"
    Write-Status "Check logs in: $OutputDirectory" "Info"
    exit 1
}
