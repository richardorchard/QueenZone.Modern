# Refreshes a local SQL Express copy of the live legacy/deploy Azure SQL
# database (queenzone-db, Basic tier) via a sqlpackage bacpac export/import,
# so nightly probes run against a same-day snapshot instead of the live
# database. Run nightly by .github/workflows/nightly-legacy-checks.yml on the
# Windows runner, where SQL Express lives.
#
# Requires ConnectionStrings__QueenZoneLegacy (source, Azure SQL) set in the
# environment. Requires sqlpackage on PATH (already installed here as a
# dotnet global tool: `dotnet tool install -g microsoft.sqlpackage`).
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

$bacpacPath = Join-Path ([System.IO.Path]::GetTempPath()) "queenzone-legacy-$(Get-Date -Format 'yyyyMMdd-HHmmss').bacpac"

# Defensive cleanup: the finally block below deletes this run's own bacpac,
# but a hard-killed run (workflow cancellation, runner crash) can skip that
# and leave one behind. Sweep anything older than 6 hours - safely older
# than any run in progress - so those don't quietly accumulate in %TEMP%.
Get-ChildItem -Path ([System.IO.Path]::GetTempPath()) -Filter "queenzone-legacy-*.bacpac" -ErrorAction SilentlyContinue |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddHours(-6) } |
    ForEach-Object {
        Write-Host "Removing stale leftover bacpac from an interrupted run: $($_.Name)"
        Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
    }

try {
    Write-Host "Exporting live legacy database to $bacpacPath..."
    sqlpackage /Action:Export `
        /SourceConnectionString:"$sourceConnectionString" `
        /TargetFile:"$bacpacPath"
    if ($LASTEXITCODE -ne 0) { throw "sqlpackage export failed with exit code $LASTEXITCODE" }

    $targetConnectionString = "Server=localhost\$InstanceName;Database=master;Integrated Security=True;TrustServerCertificate=True"

    Write-Host "Dropping any existing $TargetDatabase on SQLEXPRESS for a clean import..."
    $dropSql = @"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '$TargetDatabase')
BEGIN
    ALTER DATABASE [$TargetDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$TargetDatabase];
END
"@
    sqlcmd -S "localhost\$InstanceName" -Q $dropSql

    Write-Host "Importing bacpac into SQLEXPRESS as $TargetDatabase..."
    sqlpackage /Action:Import `
        /SourceFile:"$bacpacPath" `
        /TargetConnectionString:"Server=localhost\$InstanceName;Database=$TargetDatabase;Integrated Security=True;TrustServerCertificate=True"
    if ($LASTEXITCODE -ne 0) { throw "sqlpackage import failed with exit code $LASTEXITCODE" }

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
    if (Test-Path $bacpacPath) {
        Remove-Item $bacpacPath -Force
    }
}
