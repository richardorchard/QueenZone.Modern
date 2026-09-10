# Runs an opt-in, read-only member public activity feed probe against the local SQL Express mirror.
# Requires both ConnectionStrings__QueenZoneLegacy and RUN_MEMBER_ACTIVITY_PROBE=true.
#
# The unit tests for this repository run on SQLite, which cannot ORDER BY a DateTimeOffset and so
# takes a client-side sort branch. This probe exercises the SQL Server branch: the UNION ALL over
# ArticleSubmissions / NewsSuggestions / PhotoSubmissions, ordered and paged server-side.
#
# Writes nothing, so there are no probe rows to clean up.
#
# Example:
#   $env:ConnectionStrings__QueenZoneLegacy = "Server=localhost\SQLEXPRESS;Database=queenzone_legacy_sync;Integrated Security=True;TrustServerCertificate=True"
#   $env:RUN_MEMBER_ACTIVITY_PROBE = "true"
#   .\scripts\Probe-MemberActivity.ps1

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__QueenZoneLegacy)) {
    Write-Error "ConnectionStrings__QueenZoneLegacy is not set."
}

if ($env:RUN_MEMBER_ACTIVITY_PROBE -ne "true") {
    Write-Error "Set RUN_MEMBER_ACTIVITY_PROBE=true to run the member activity feed checks against the configured database."
}

& "$PSScriptRoot/Assert-SqlExpressMirrorConnection.ps1" `
    -ConnectionString $env:ConnectionStrings__QueenZoneLegacy

dotnet test tests/QueenZone.Web.Tests/QueenZone.Web.Tests.csproj `
    --configuration $Configuration `
    --filter "FullyQualifiedName~EfMemberPublicActivityLiveProbeTests"
