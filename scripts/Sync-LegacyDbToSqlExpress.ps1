# Refreshes a local SQL Express copy of the live legacy/deploy Azure SQL
# database (queenzone-db, Basic tier), so nightly probes run against a
# same-day snapshot instead of the live database. Run nightly by
# .github/workflows/nightly-legacy-checks.yml on the Windows runner, where
# SQL Express lives.
#
# Uses sqlpackage Extract (with ExtractAllTableData, producing a schema+data
# dacpac) + Publish, not the more obvious Export/Import bacpac pair. Reason:
# the legacy schema has pre-existing broken forum views (e.g. Q_FORUM_TOPIC_V)
# referencing at least one table (dbo.Q_FORUM_TOPIC_T) that doesn't actually
# exist in the source - not an ordering/validation quirk SQL Server's
# deferred name resolution papers over, but a genuinely dead reference. The
# probe tests this mirror serves (EfAdminNewsRepositoryLegacyProbeTests,
# EfAdminNewsRepositoryLegacyWriteProbeTests, EfNewsSectionLiveProbeTests)
# query news tables directly via EF/SQL - no view in the schema is on that
# path - so views aren't needed here at all.
# /Action:Export doesn't support excluding object types at all; /Action:Extract
# doesn't either (verified against this sqlpackage version's own /? help, not
# assumed); but /Action:Publish does via /p:ExcludeObjectTypes=Views, and
# empirically DOES restore the embedded table data from an ExtractAllTableData
# dacpac (verified locally: NEWS_T came through with 5268 rows, ViewCount 0) -
# that combination isn't obviously documented, hence this much explanation.
#
# Requires ConnectionStrings__QueenZoneLegacy (source, Azure SQL) set in the
# environment. Invokes sqlpackage as a local dotnet tool (.config/dotnet-tools.json,
# restored via `dotnet tool restore` before this script runs) rather than assuming
# it's on PATH - the GitHub Actions runner service on this machine runs as
# NT AUTHORITY\NETWORK SERVICE, a different profile than the interactive user
# account a global `dotnet tool install -g` would have put it on PATH for.
#
# Ordinary data-sync automation, not a system/security-config change - unlike
# Enable-SqlExpressRemoteAccess.ps1 (run once, manually, before this is used).

param(
    [string]$InstanceName = "SQLEXPRESS",
    [string]$TargetDatabase = "queenzone_legacy_sync",
    [string]$ProbeLoginName = "queenzone_probe"
)

$ErrorActionPreference = "Stop"

$sourceConnectionString = $env:ConnectionStrings__QueenZoneLegacy
if ([string]::IsNullOrWhiteSpace($sourceConnectionString)) {
    Write-Error "ConnectionStrings__QueenZoneLegacy is not set."
}

$dacpacPath = Join-Path ([System.IO.Path]::GetTempPath()) "queenzone-legacy-$(Get-Date -Format 'yyyyMMdd-HHmmss').dacpac"

# Defensive cleanup: the finally block below deletes this run's own dacpac,
# but a hard-killed run (workflow cancellation, runner crash) can skip that
# and leave one behind. Sweep anything older than 6 hours - safely older
# than any run in progress - so those don't quietly accumulate in %TEMP%.
Get-ChildItem -Path ([System.IO.Path]::GetTempPath()) -Filter "queenzone-legacy-*.dacpac" -ErrorAction SilentlyContinue |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddHours(-6) } |
    ForEach-Object {
        Write-Host "Removing stale leftover dacpac from an interrupted run: $($_.Name)"
        Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
    }

try {
    Write-Host "Extracting live legacy database (schema + data) to $dacpacPath..."
    dotnet tool run sqlpackage /Action:Extract `
        /SourceConnectionString:"$sourceConnectionString" `
        /TargetFile:"$dacpacPath" `
        /p:ExtractAllTableData=True `
        /p:VerifyExtraction=False
    if ($LASTEXITCODE -ne 0) { throw "sqlpackage extract failed with exit code $LASTEXITCODE" }

    Write-Host "Dropping any existing $TargetDatabase on SQLEXPRESS for a clean publish..."
    $dropSql = @"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '$TargetDatabase')
BEGIN
    ALTER DATABASE [$TargetDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$TargetDatabase];
END
"@
    sqlcmd -S "localhost\$InstanceName" -Q $dropSql

    Write-Host "Publishing dacpac into SQLEXPRESS as $TargetDatabase (excluding views)..."
    dotnet tool run sqlpackage /Action:Publish `
        /SourceFile:"$dacpacPath" `
        /TargetConnectionString:"Server=localhost\$InstanceName;Database=$TargetDatabase;Integrated Security=True;TrustServerCertificate=True" `
        /p:ExcludeObjectTypes=Views `
        /p:AllowIncompatiblePlatform=True
    if ($LASTEXITCODE -ne 0) { throw "sqlpackage publish failed with exit code $LASTEXITCODE" }

    Write-Host "Granting $ProbeLoginName access to the refreshed $TargetDatabase..."
    $grantSql = @"
USE [$TargetDatabase];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$ProbeLoginName')
BEGIN
    CREATE USER [$ProbeLoginName] FOR LOGIN [$ProbeLoginName];
    ALTER ROLE db_owner ADD MEMBER [$ProbeLoginName];
END
"@
    sqlcmd -S "localhost\$InstanceName" -Q $grantSql

    Write-Host "Sync complete: $TargetDatabase refreshed from the live legacy database."
}
finally {
    if (Test-Path $dacpacPath) {
        Remove-Item $dacpacPath -Force
    }
}
