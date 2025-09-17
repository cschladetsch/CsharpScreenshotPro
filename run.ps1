#!/usr/bin/env pwsh
param(
    [string]$Project = "ScreenshotPro.UI",
    [string]$Configuration = "Debug",
    [switch]$Help,
    [switch]$h
)

# Show help if requested
if ($Help -or $h) {
    Write-Host ""
    Write-Host "ScreenshotPro Run Script" -ForegroundColor Cyan
    Write-Host "========================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: " -NoNewline
    Write-Host ".\run.ps1 " -ForegroundColor Yellow -NoNewline
    Write-Host "[options]"
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Green
    Write-Host "  -Project <name>         Project to run (default: ScreenshotPro.UI)"
    Write-Host "  -Configuration <config> Build configuration (default: Debug)"
    Write-Host "  -Help, -h               Show this help message"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Green
    Write-Host "  .\run.ps1                              " -ForegroundColor Yellow -NoNewline
    Write-Host "# Run UI project in Debug"
    Write-Host "  .\run.ps1 -Configuration Release       " -ForegroundColor Yellow -NoNewline
    Write-Host "# Run in Release mode"
    Write-Host "  .\run.ps1 -Project ScreenshotPro.Tests " -ForegroundColor Yellow -NoNewline
    Write-Host "# Run tests"
    Write-Host ""
    return
}

# Navigate to script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "Running ScreenshotPro..." -ForegroundColor Cyan
Write-Host "Project: " -NoNewline
Write-Host $Project -ForegroundColor Yellow
Write-Host "Configuration: " -NoNewline
Write-Host $Configuration -ForegroundColor Yellow
Write-Host ""

# Check if project exists
$projectPath = Join-Path $scriptDir $Project
if (-not (Test-Path "$projectPath\*.csproj")) {
    Write-Host "❌ Project '$Project' not found in current directory" -ForegroundColor Red
    Write-Host "Available projects:" -ForegroundColor Yellow
    Get-ChildItem -Path . -Filter "*.csproj" -Recurse | ForEach-Object {
        $relativePath = Resolve-Path -Relative $_.Directory
        Write-Host "  $relativePath" -ForegroundColor Gray
    }
    Read-Host "Press Enter to continue"
    exit 1
}

try {
    Write-Host "🚀 Starting application..." -ForegroundColor Green
    Write-Host "Using launch settings from $Project\Properties\launchSettings.json" -ForegroundColor Gray
    Write-Host ""

    dotnet run --project $Project --configuration $Configuration

    if ($LASTEXITCODE -ne 0) {
        throw "Application exited with code $LASTEXITCODE"
    }
}
catch {
    Write-Host ""
    Write-Host "❌ Run failed: $_" -ForegroundColor Red
    Read-Host "Press Enter to continue"
    exit 1
}

Write-Host ""
Write-Host "✅ Application finished" -ForegroundColor Green