# One-time setup: makes the local SQLEXPRESS instance on this machine (GLORY11)
# reachable from another machine on the LAN (the Mac Mini self-hosted runner),
# for the nightly legacy-DB-check pipeline in
# .github/workflows/nightly-legacy-checks.yml.
#
# This changes firewall and SQL Server network/auth configuration on this
# machine. Review it, then run it yourself in an elevated (Administrator)
# PowerShell prompt:
#
#   .\scripts\Enable-SqlExpressRemoteAccess.ps1 -AllowedSourceAddress <mac-lan-ip>
#
# Not something an automated agent should run unattended — that's why this is
# a script for you to read and execute, not something run on your behalf.

#Requires -RunAsAdministrator

param(
    # The Mac runner's LAN IP (recommended: a DHCP reservation on your router
    # so this doesn't drift). Falls back to the whole 192.168.1.0/24 subnet if
    # not supplied, which is looser than necessary — prefer passing the
    # specific IP.
    [string]$AllowedSourceAddress = "192.168.1.0/24",
    [string]$InstanceName = "SQLEXPRESS",
    [int]$Port = 1433,
    [string]$ProbeLoginName = "queenzone_probe",
    [string]$ProbeLoginPassword = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 24 | ForEach-Object { [char]$_ }),
    # The Windows account the actions-runner service runs as (check with
    # `Get-CimInstance Win32_Service -Filter "Name LIKE '%actions.runner%'"`).
    # Needs dbcreator so Sync-LegacyDbToSqlExpress.ps1 can drop/recreate the
    # mirror database each night via Windows-integrated auth on localhost.
    [string]$RunnerServiceAccount = "NT AUTHORITY\NETWORK SERVICE"
)

$ErrorActionPreference = "Stop"

$regBase = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server"
$instanceKey = Get-ItemProperty $regBase -ErrorAction Stop
$fullInstanceId = $instanceKey.InstalledInstances |
    ForEach-Object { (Get-ItemProperty "$regBase\Instance Names\SQL" -ErrorAction Stop).$_ ; $_ } |
    Where-Object { $_ -eq $InstanceName } |
    Select-Object -First 1
$mssqlInstanceId = (Get-ItemProperty "$regBase\Instance Names\SQL").$InstanceName
if (-not $mssqlInstanceId) {
    throw "Could not resolve instance ID for '$InstanceName'. Check -InstanceName."
}

$tcpBase = "$regBase\$mssqlInstanceId\MSSQLServer\SuperSocketNetLib\Tcp"
$loginModeBase = "$regBase\$mssqlInstanceId\MSSQLServer"

Write-Host "Enabling TCP/IP on $InstanceName and setting a static port ($Port)..."
Set-ItemProperty -Path $tcpBase -Name "Enabled" -Value 1
Set-ItemProperty -Path "$tcpBase\IPAll" -Name "TcpPort" -Value "$Port"
Set-ItemProperty -Path "$tcpBase\IPAll" -Name "TcpDynamicPorts" -Value ""

Write-Host "Enabling SQL Server mixed-mode authentication (Windows + SQL logins)..."
# LoginMode 2 = Mixed. Needed because the Mac isn't domain-joined, so
# Windows-integrated auth isn't usable from it over the network.
Set-ItemProperty -Path $loginModeBase -Name "LoginMode" -Value 2

Write-Host "Restarting SQL Server ($InstanceName) to apply network/auth changes..."
Restart-Service -Name "MSSQL`$$InstanceName" -Force
Start-Sleep -Seconds 5

Write-Host "Adding a Windows Firewall rule for TCP $Port from $AllowedSourceAddress..."
$ruleName = "SQL Server ($InstanceName) - LAN nightly probes"
Remove-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
New-NetFirewallRule -DisplayName $ruleName `
    -Direction Inbound -Protocol TCP -LocalPort $Port `
    -RemoteAddress $AllowedSourceAddress -Action Allow | Out-Null

Write-Host "Creating a least-privilege SQL login ($ProbeLoginName) for the nightly job..."
# PRINT + substring match instead of parsing a COUNT(1) column - sqlcmd's
# column/header output is easy to get subtly wrong (blank lines, padding)
# and got this detection wrong on the first attempt.
$existsCheckOutput = sqlcmd -S "localhost\$InstanceName" -h -1 -W -Q "SET NOCOUNT ON; IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$ProbeLoginName') PRINT 'PROBE_LOGIN_EXISTS' ELSE PRINT 'PROBE_LOGIN_MISSING'"
$probeLoginExisted = ($existsCheckOutput -join "`n") -match 'PROBE_LOGIN_EXISTS'
$createLoginSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$ProbeLoginName')
BEGIN
    CREATE LOGIN [$ProbeLoginName] WITH PASSWORD = N'$ProbeLoginPassword', CHECK_POLICY = ON;
END
"@
sqlcmd -S "localhost\$InstanceName" -Q $createLoginSql

# dbcreator: CREATE/ALTER/DROP DATABASE for the sync script's drop-and-reimport
# cycle. securityadmin: the bacpac import recreates a CREATE LOGIN for a
# database user carried over from the Azure SQL source (contained database
# users get translated into a login+user pair on non-Azure targets like SQL
# Express) - dbcreator alone doesn't cover creating server-level logins.
# Not sysadmin: this is the minimum found necessary by actually running the
# import and reacting to each permission error, not a guess.
#
# Grants any Windows service running as this shared builtin account these
# roles on this SQL Server instance - acceptable here since it's a
# single-purpose dev/CI box, but worth knowing if another service ever runs
# as the same account.
$rolesNeeded = @("dbcreator", "securityadmin")
Write-Host "Granting $RunnerServiceAccount $($rolesNeeded -join ', ') so it can refresh the mirror DB nightly..."
$grantLoginSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$RunnerServiceAccount')
BEGIN
    CREATE LOGIN [$RunnerServiceAccount] FROM WINDOWS;
END
"@
sqlcmd -S "localhost\$InstanceName" -Q $grantLoginSql
foreach ($role in $rolesNeeded) {
    $grantRoleSql = @"
IF NOT EXISTS (
    SELECT 1 FROM sys.server_role_members rm
    JOIN sys.server_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.server_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = '$role' AND m.name = '$RunnerServiceAccount'
)
BEGIN
    ALTER SERVER ROLE $role ADD MEMBER [$RunnerServiceAccount];
END
"@
    sqlcmd -S "localhost\$InstanceName" -Q $grantRoleSql
}

Write-Host ""
Write-Host "Done. Next steps:"
$step = 1
if ($probeLoginExisted) {
    Write-Host "$step. $ProbeLoginName already existed - its password was NOT changed (the"
    Write-Host "   random one generated for this run was never applied, ignore it). Nothing"
    Write-Host "   to update in Bitwarden if you already saved it from an earlier run."
} else {
    Write-Host "$step. Save this new login to Bitwarden (matches the existing convention used by"
    Write-Host "   BITWARDEN_APP_SERVICE_DEPLOY_SECRETS for the Azure SQL connection string):"
    Write-Host "     Login:    $ProbeLoginName"
    Write-Host "     Password: $ProbeLoginPassword"
}
$step++
Write-Host "$step. The sync script (Sync-LegacyDbToSqlExpress.ps1) grants this login access"
Write-Host "   to the synced database each time it refreshes it - no extra GRANT step needed."
$step++
Write-Host "$step. Confirm this machine's LAN IP is stable (DHCP reservation on your router) -"
Write-Host "   currently 192.168.1.237 (GLORY11). The nightly workflow will connect to"
Write-Host "   that address."
$step++
Write-Host "$step. From the Mac, sanity-check reachability once both sides are configured:"
Write-Host "     nc -zv 192.168.1.237 $Port"
