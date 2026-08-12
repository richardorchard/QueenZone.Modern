#Requires -Version 5.1
<#
.SYNOPSIS
    Runs the QueenZone search reindex worker for manual or scheduled execution.

.DESCRIPTION
    Wraps `dotnet run --project src/QueenZone.SearchReindex.Worker -- reindex` for Task
    Scheduler / cron use. `ConnectionStrings__QueenZoneLegacy` (or appsettings.Local.json)
    provides the database connection; without one the worker falls back to in-memory sample
    data.

    See docs/architecture/search-reindex-worker.md for scheduling options.

.EXAMPLE
    .\scripts\Run-SearchReindexWorker.ps1 -Scheduled

.EXAMPLE
    .\scripts\Run-SearchReindexWorker.ps1 -Force
#>
[CmdletBinding()]
param(
    [switch]$Scheduled,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $repoRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error 'dotnet SDK not found. Install from https://dotnet.microsoft.com/download'
}

$workerArgs = @('run', '--project', 'src/QueenZone.SearchReindex.Worker', '--', 'reindex')

if ($Scheduled) {
    $workerArgs += '--scheduled'
}
if ($Force) {
    $workerArgs += '--force'
}

Write-Host "QueenZone search reindex: dotnet $($workerArgs -join ' ')"
& dotnet @workerArgs
exit $LASTEXITCODE
