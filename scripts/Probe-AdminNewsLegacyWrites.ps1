# Runs an opt-in create/publish/unpublish/delete probe against the local SQL Express mirror.
# Requires both ConnectionStrings__QueenZoneLegacy and RUN_LEGACY_WRITE_PROBE=true.
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
    --filter "FullyQualifiedName~EfAdminNewsRepositoryLegacyWriteProbeTests"
