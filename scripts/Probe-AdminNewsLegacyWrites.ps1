# Runs opt-in admin news write probes against the local SQL Express mirror.
# Requires both ConnectionStrings__QueenZoneLegacy and RUN_LEGACY_WRITE_PROBE=true.
#
# Covers the same write surface as nightly legacy-write-probes:
#   - EfAdminNewsRepositoryLegacyWriteProbeTests (create/publish/unpublish/delete)
#   - EfNewsSectionLiveProbeTests write Facts (transaction rollback visibility +
#     full lifecycle create/edit/publish/unpublish/delete)
#
# The public read-only Fact in EfNewsSectionLiveProbeTests stays on the Mac
# legacy-read-probes job; this script filters only the Admin_news_* write Facts.
# Discovery promotion remains opt-in only (see #503).
#
# Example:
#   $env:ConnectionStrings__QueenZoneLegacy = "Server=localhost\SQLEXPRESS;Database=queenzone_legacy_sync;Integrated Security=True;TrustServerCertificate=True"
#   $env:RUN_LEGACY_WRITE_PROBE = "true"
#   .\scripts\Probe-AdminNewsLegacyWrites.ps1

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

& "$PSScriptRoot/Assert-SqlExpressMirrorConnection.ps1" `
    -ConnectionString $env:ConnectionStrings__QueenZoneLegacy

dotnet test tests/QueenZone.Web.Tests/QueenZone.Web.Tests.csproj `
    --configuration $Configuration `
    --filter "FullyQualifiedName~EfAdminNewsRepositoryLegacyWriteProbeTests|FullyQualifiedName~EfNewsSectionLiveProbeTests.Admin_news_"
