#Requires -Version 5.1
<#
.SYNOPSIS
    Checks for one web-queued news gathering request and processes it locally.

.DESCRIPTION
    Intended for Windows Task Scheduler. The command contacts the shared database
    using the worker's local configuration, records a runner heartbeat, and claims at
    most one pending request. Claimed requests run source gathering and triage only;
    AI drafts remain explicit per-candidate editor actions in QueenZone admin.

.EXAMPLE
    .\scripts\Process-NewsAgentRunRequests.ps1

.EXAMPLE
    .\scripts\Process-NewsAgentRunRequests.ps1 -RunnerId "news-pc"
#>
[CmdletBinding()]
param(
    [string]$RunnerId = $env:COMPUTERNAME
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $repoRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error 'dotnet SDK not found. Install from https://dotnet.microsoft.com/download'
}

if ([string]::IsNullOrWhiteSpace($RunnerId)) {
    $RunnerId = 'windows-news-runner'
}

$workerArgs = @(
    'run',
    '--project',
    'src/QueenZone.NewsAgent.Worker',
    '--',
    'process-news-requests',
    '--runner-id',
    $RunnerId
)

Write-Host "QueenZone queued news requests: runner $RunnerId"
& dotnet @workerArgs
exit $LASTEXITCODE
