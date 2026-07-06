#Requires -Version 7.0
# Kuzu.Net — "drop into the repo, run one command, the thing works" entry point
# (workspace root-run.ps1 mandate, Stage-0 shape: library-only companion).
# Happy path: dotnet tool restore -> fantomas --check -> dotnet build.
# There is no test project yet, so -SkipTests is accepted for interface
# stability but currently a no-op; when an Expecto runner lands under
# tests/, wire it in here.
[CmdletBinding()]
param(
    [switch] $SkipFormat,
    [switch] $SkipBuild,
    [switch] $SkipTests
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "=== dotnet tool restore (fantomas) ===" -ForegroundColor Cyan
dotnet tool restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipFormat) {
    Write-Host "=== fantomas --check src ===" -ForegroundColor Cyan
    dotnet fantomas --check src
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Formatting check failed. Run 'dotnet fantomas src' to fix." -ForegroundColor Yellow
        exit $LASTEXITCODE
    }
}

if (-not $SkipBuild) {
    Write-Host "=== dotnet build Kuzu.Net.slnx ===" -ForegroundColor Cyan
    dotnet build Kuzu.Net.slnx
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (-not $SkipTests) {
    # No test project exists yet (library-only Stage 0) — nothing to run.
    Write-Host "=== tests: none yet (library-only repo) ===" -ForegroundColor DarkGray
}

Write-Host "run.ps1 happy path complete." -ForegroundColor Green
