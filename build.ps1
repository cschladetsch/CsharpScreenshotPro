#!/usr/bin/env pwsh
param(
    [switch]$Clean,
    [switch]$Run,
    [switch]$Help,
    [switch]$h
)

# Show help if requested
if ($Help -or $h) {
    Write-Host ""
    Write-Host "ScreenshotPro Build Script" -ForegroundColor Cyan
    Write-Host "=========================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: " -NoNewline
    Write-Host ".\build.ps1 " -ForegroundColor Yellow -NoNewline
    Write-Host "[options]"
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Green
    Write-Host "  -Clean      Clean the solution before building"
    Write-Host "  -Run        Run the application after successful build"
    Write-Host "  -Help, -h   Show this help message"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Green
    Write-Host "  .\build.ps1                    " -ForegroundColor Yellow -NoNewline
    Write-Host "# Quick build"
    Write-Host "  .\build.ps1 -Clean            " -ForegroundColor Yellow -NoNewline
    Write-Host "# Clean build"
    Write-Host "  .\build.ps1 -Run              " -ForegroundColor Yellow -NoNewline
    Write-Host "# Build and run"
    Write-Host "  .\build.ps1 -Clean -Run       " -ForegroundColor Yellow -NoNewline
    Write-Host "# Clean, build, and run"
    Write-Host ""
    return
}

# Navigate to script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "Building ScreenshotPro..." -ForegroundColor Cyan
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "🧹 Cleaning solution..." -ForegroundColor Yellow
    try {
        dotnet clean ScreenshotPro.sln
        if ($LASTEXITCODE -ne 0) {
            throw "Clean failed with exit code $LASTEXITCODE"
        }
        Write-Host ""
    }
    catch {
        Write-Host "❌ Clean failed: $_" -ForegroundColor Red
        if (-not $Run) {
            Read-Host "Press Enter to continue"
        }
        exit 1
    }
}

# Build the solution
Write-Host "🔨 Building solution..." -ForegroundColor Yellow
try {
    dotnet build ScreenshotPro.sln
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
}
catch {
    Write-Host "❌ Build failed: $_" -ForegroundColor Red
    if (-not $Run) {
        Read-Host "Press Enter to continue"
    }
    exit 1
}

Write-Host ""
Write-Host "✅ Build completed successfully!" -ForegroundColor Green
Write-Host ""

# Run if requested
if ($Run) {
    if (Test-Path "run.ps1") {
        Write-Host "🚀 Starting application using run.ps1..." -ForegroundColor Cyan
        .\run.ps1
    }
    elseif (Test-Path "run.bat") {
        Write-Host "🚀 Starting application using run.bat..." -ForegroundColor Cyan
        & ".\run.bat"
    }
    else {
        Write-Host "🚀 Running application directly..." -ForegroundColor Cyan
        dotnet run --project ScreenshotPro.UI
    }
}
else {
    Read-Host "Press Enter to continue"
}