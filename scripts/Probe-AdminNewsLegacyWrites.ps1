# Runs an opt-in create/publish/unpublish/delete probe against a real SQL Server.
# Requires both ConnectionStrings__QueenZoneLegacy and RUN_LEGACY_WRITE_PROBE=true.
#
# Prefer the disposable SQL Express mirror (queenzone_legacy_sync) when available —
# the same target nightly-legacy-checks.yml mutates after Sync-LegacyDbToSqlExpress.ps1.
# Only point this at live or shared Azure SQL when you deliberately intend to mutate it.
#
# Example (mirror on the Windows runner LAN, adjust host/password as needed):
#   $env:ConnectionStrings__QueenZoneLegacy = "Server=...;Database=queenzone_legacy_sync;..."
#   $env:RUN_LEGACY_WRITE_PROBE = "true"
#   .\scripts\Probe-AdminNewsLegacyWrites.ps1
#
# Nightly already runs this probe class (plus EfNewsSectionLiveProbeTests write Facts)
# against the mirror; use this script for ad-hoc pre-release checks.

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__QueenZoneLegacy)) {
    Write-Error "ConnectionStrings__QueenZoneLegacy is not set."
}

if ($env:RUN_LEGACY_WRITE_PROBE -ne "true") {
    Write-Error "Set RUN_LEGACY_WRITE_PROBE=true to run destructive write checks against the configured database."
}

dotnet test tests/QueenZone.Web.Tests/QueenZone.Web.Tests.csproj `
    --configuration $Configuration `
    --filter "FullyQualifiedName~EfAdminNewsRepositoryLegacyWriteProbeTests"
