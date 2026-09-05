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
# The live Azure SQL source also carries contained users, logins, permissions,
# and role memberships that SQL Express cannot host - contained-user CREATE USER
# WITH PASSWORD is legal only in a contained database, and Azure principals
# have no Express counterpart. Their schemas still need owners when Users are
# excluded, so the script creates loginless placeholder owners in the staging
# mirror before Publish. This avoids SQL72014 / Msg 15151 without copying a
# production principal or credential. Probe access is granted
# after Publish to the Express-local queenzone_probe login, not by replaying
# Azure security objects, so Users/Logins/Permissions/RoleMembership
# are excluded here too. (sqlpackage's type name is RoleMembership, singular.)
# /Action:Export doesn't support excluding object types at all; /Action:Extract
# doesn't either (verified against this sqlpackage version's own /? help, not
# assumed); but /Action:Publish does via
# /p:ExcludeObjectTypes=Views;Users;Logins;Permissions;RoleMembership, and
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
$stagingDatabase = "${TargetDatabase}_refresh"
$stagingPromoted = $false

if ($TargetDatabase -notmatch '^[A-Za-z0-9_]+$' -or
    $stagingDatabase -notmatch '^[A-Za-z0-9_]+$' -or
    $ProbeLoginName -notmatch '^[A-Za-z0-9_]+$') {
    throw "Mirror database and probe-login names may contain only letters, numbers, and underscores."
}

function Get-SourceSchemaUserNames([string] $ConnectionString) {
    $connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = @"
SELECT DISTINCT principal.name
FROM sys.schemas schema_info
JOIN sys.database_principals principal ON principal.principal_id=schema_info.principal_id
WHERE principal.type <> 'R'
  AND principal.name NOT IN ('dbo','guest','sys','INFORMATION_SCHEMA');
"@
        $reader = $command.ExecuteReader()
        try {
            while ($reader.Read()) { Write-Output $reader.GetString(0) }
        }
        finally {
            $reader.Dispose()
            $command.Dispose()
        }
    }
    finally {
        $connection.Dispose()
    }
}

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
    $schemaUserNames = @(Get-SourceSchemaUserNames $sourceConnectionString)

    Write-Host "Extracting live legacy database (schema + data) to $dacpacPath..."
    dotnet tool run sqlpackage /Action:Extract `
        /SourceConnectionString:"$sourceConnectionString" `
        /TargetFile:"$dacpacPath" `
        /p:ExtractAllTableData=True `
        /p:VerifyExtraction=False
    if ($LASTEXITCODE -ne 0) { throw "sqlpackage extract failed with exit code $LASTEXITCODE" }

    Write-Host "Recreating staging database $stagingDatabase..."
    $dropSql = @"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '$stagingDatabase')
BEGIN
    ALTER DATABASE [$stagingDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$stagingDatabase];
END
CREATE DATABASE [$stagingDatabase];
"@
    sqlcmd -S "localhost\$InstanceName" -b -Q $dropSql
    if ($LASTEXITCODE -ne 0) { throw "Staging database creation failed with exit code $LASTEXITCODE" }

    foreach ($schemaUserName in $schemaUserNames) {
        $ownerIdentifier = $schemaUserName.Replace(']', ']]')
        $ownerLiteral = $schemaUserName.Replace("'", "''")
        $ownerSql = @"
USE [$stagingDatabase];
IF DATABASE_PRINCIPAL_ID(N'$ownerLiteral') IS NULL
    CREATE USER [$ownerIdentifier] WITHOUT LOGIN;
"@
        sqlcmd -S "localhost\$InstanceName" -b -Q $ownerSql
        if ($LASTEXITCODE -ne 0) { throw "Staging schema-owner creation failed with exit code $LASTEXITCODE" }
    }

    Write-Host "Publishing dacpac into SQLEXPRESS as staging database $stagingDatabase (excluding views and Azure security objects)..."
    dotnet tool run sqlpackage /Action:Publish `
        /SourceFile:"$dacpacPath" `
        /TargetConnectionString:"Server=localhost\$InstanceName;Database=$stagingDatabase;Integrated Security=True;TrustServerCertificate=True" `
        /p:ExcludeObjectTypes="Views;Users;Logins;Permissions;RoleMembership" `
        /p:AllowIncompatiblePlatform=True
    if ($LASTEXITCODE -ne 0) { throw "sqlpackage publish failed with exit code $LASTEXITCODE" }

    $verifySql = @"
USE [$stagingDatabase];
IF OBJECT_ID(N'dbo.NEWS_T', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Q_ARTICLE_T', N'U') IS NULL
   OR OBJECT_ID(N'dbo.PIC_FILES_T', N'U') IS NULL
   OR OBJECT_ID(N'dbo.ModernForumThread', N'U') IS NULL
    THROW 50000, 'The staged mirror is missing required production tables.', 1;
"@
    sqlcmd -S "localhost\$InstanceName" -b -Q $verifySql
    if ($LASTEXITCODE -ne 0) { throw "Staged mirror verification failed with exit code $LASTEXITCODE" }

    Write-Host "Granting $ProbeLoginName access to the staged mirror..."
    $grantSql = @"
USE [$stagingDatabase];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$ProbeLoginName')
BEGIN
    CREATE USER [$ProbeLoginName] FOR LOGIN [$ProbeLoginName];
    ALTER ROLE db_owner ADD MEMBER [$ProbeLoginName];
END
"@
    sqlcmd -S "localhost\$InstanceName" -Q $grantSql

    Write-Host "Replacing $TargetDatabase with the verified staged mirror..."
    $promoteSql = @"
IF DB_ID(N'$stagingDatabase') IS NULL
    THROW 50000, 'The staged mirror database does not exist.', 1;
IF DB_ID(N'$TargetDatabase') IS NOT NULL
BEGIN
    ALTER DATABASE [$TargetDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$TargetDatabase];
END
ALTER DATABASE [$stagingDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
ALTER DATABASE [$stagingDatabase] MODIFY NAME = [$TargetDatabase];
ALTER DATABASE [$TargetDatabase] SET MULTI_USER;
"@
    sqlcmd -S "localhost\$InstanceName" -b -Q $promoteSql
    if ($LASTEXITCODE -ne 0) { throw "Mirror promotion failed with exit code $LASTEXITCODE" }
    $stagingPromoted = $true

    Write-Host "Sync complete: $TargetDatabase refreshed from the live legacy database."
}
finally {
    if (-not $stagingPromoted) {
        $cleanupSql = @"
IF DB_ID(N'$stagingDatabase') IS NOT NULL
BEGIN
    ALTER DATABASE [$stagingDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$stagingDatabase];
END
"@
        sqlcmd -S "localhost\$InstanceName" -Q $cleanupSql 2>$null
    }
    if (Test-Path $dacpacPath) {
        Remove-Item $dacpacPath -Force
    }
}
