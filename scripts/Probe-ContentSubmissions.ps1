# Runs opt-in photo and article submission write probes against the local SQL Express mirror.
# Requires both ConnectionStrings__QueenZoneLegacy and RUN_CONTENT_SUBMISSION_PROBE=true.
#
# Example:
#   $env:ConnectionStrings__QueenZoneLegacy = "Server=localhost\SQLEXPRESS;Database=queenzone_legacy_sync;Integrated Security=True;TrustServerCertificate=True"
#   $env:RUN_CONTENT_SUBMISSION_PROBE = "true"
#   .\scripts\Probe-ContentSubmissions.ps1

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__QueenZoneLegacy)) {
    Write-Error "ConnectionStrings__QueenZoneLegacy is not set."
}

if ($env:RUN_CONTENT_SUBMISSION_PROBE -ne "true") {
    Write-Error "Set RUN_CONTENT_SUBMISSION_PROBE=true to run mutable content-submission checks against the configured database."
}

& "$PSScriptRoot/Assert-SqlExpressMirrorConnection.ps1" `
    -ConnectionString $env:ConnectionStrings__QueenZoneLegacy

dotnet test tests/QueenZone.Web.Tests/QueenZone.Web.Tests.csproj `
    --configuration $Configuration `
    --filter "FullyQualifiedName~EfContentSubmissionLiveProbeTests"
