#!/usr/bin/env pwsh
# Script to apply the migration to add UserId and IsAnonymous fields to MapLocations table

Write-Host "Starting migration for UserId in MapLocations table..."

# Get the path to the database
$databasePath = Join-Path $PSScriptRoot "..\Data\maphive.db"
$migrationPath = Join-Path $PSScriptRoot "..\Data\Migrations\AddUserIdToMapLocations.sql"

if (-not (Test-Path $databasePath)) {
    Write-Error "Database file not found at $databasePath"
    exit 1
}

if (-not (Test-Path $migrationPath)) {
    Write-Error "Migration file not found at $migrationPath"
    exit 1
}

# Read the migration script
$migrationScript = Get-Content -Path $migrationPath -Raw

# Execute the migration
try {
    # Make sure sqlite3 command is available
    if (-not (Get-Command "sqlite3" -ErrorAction SilentlyContinue)) {
        Write-Error "sqlite3 command not found. Please install SQLite command-line tools."
        exit 1
    }

    # Execute the migration
    $migrationScript | sqlite3 $databasePath

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Migration completed successfully!"
        exit 0
    } else {
        Write-Error "Migration failed. Exit code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
} catch {
    Write-Error "An error occurred during migration: $_"
    exit 1
} 