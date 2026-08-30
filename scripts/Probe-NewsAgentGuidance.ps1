# Runs an opt-in news-agent editorial guidance publish/rollback/restore-default probe
# against the local SQL Express mirror.
# Requires both ConnectionStrings__QueenZoneLegacy and RUN_NEWS_AGENT_GUIDANCE_PROBE=true.
#
# Exercises EfNewsAgentGuidanceRepository's Publish/Rollback/RestoreCompiledDefault paths
# under the real SqlServerRetryingExecutionStrategy (Sqlite/in-memory providers never
# configure a retrying strategy, so they cannot catch a method that opens a transaction
# directly instead of routing it through Database.CreateExecutionStrategy()), then restores
# the probed type's pre-existing draft/published rows exactly.
#
# Example:
#   $env:ConnectionStrings__QueenZoneLegacy = "Server=localhost\SQLEXPRESS;Database=queenzone_legacy_sync;Integrated Security=True;TrustServerCertificate=True"
#   $env:RUN_NEWS_AGENT_GUIDANCE_PROBE = "true"
#   .\scripts\Probe-NewsAgentGuidance.ps1

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__QueenZoneLegacy)) {
    Write-Error "ConnectionStrings__QueenZoneLegacy is not set."
}

if ($env:RUN_NEWS_AGENT_GUIDANCE_PROBE -ne "true") {
    Write-Error "Set RUN_NEWS_AGENT_GUIDANCE_PROBE=true to run mutable guidance write checks against the configured database."
}

& "$PSScriptRoot/Assert-SqlExpressMirrorConnection.ps1" `
    -ConnectionString $env:ConnectionStrings__QueenZoneLegacy

dotnet test tests/QueenZone.Web.Tests/QueenZone.Web.Tests.csproj `
    --configuration $Configuration `
    --filter "FullyQualifiedName~EfNewsAgentGuidanceLiveProbeTests"
