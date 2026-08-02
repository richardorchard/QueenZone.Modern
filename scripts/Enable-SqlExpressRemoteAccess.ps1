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
    [string]$ProbeLoginPassword = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 24 | ForEach-Object { [char]$_ })
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
$createLoginSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$ProbeLoginName')
BEGIN
    CREATE LOGIN [$ProbeLoginName] WITH PASSWORD = N'$ProbeLoginPassword', CHECK_POLICY = ON;
END
"@
sqlcmd -S "localhost\$InstanceName" -Q $createLoginSql

Write-Host ""
Write-Host "Done. Next steps:"
Write-Host "1. Save this login to Bitwarden (matches the existing convention used by"
Write-Host "   BITWARDEN_APP_SERVICE_DEPLOY_SECRETS for the Azure SQL connection string):"
Write-Host "     Login:    $ProbeLoginName"
Write-Host "     Password: $ProbeLoginPassword"
Write-Host "2. The sync script (Sync-LegacyDbToSqlExpress.ps1) grants this login access"
Write-Host "   to the synced database each time it refreshes it - no extra GRANT step needed."
Write-Host "3. Confirm this machine's LAN IP is stable (DHCP reservation on your router) -"
Write-Host "   currently 192.168.1.237 (GLORY11). The nightly workflow will connect to"
Write-Host "   that address."
Write-Host "4. From the Mac, sanity-check reachability once both sides are configured:"
Write-Host "     nc -zv 192.168.1.237 $Port"
