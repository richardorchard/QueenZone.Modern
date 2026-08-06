#Requires -Version 5.1
param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString,
    [string]$ExpectedDatabase = "queenzone_legacy_sync"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$builder = [System.Data.Common.DbConnectionStringBuilder]::new()
$builder.set_ConnectionString($ConnectionString)

function Get-ConnectionValue([string[]]$Keys) {
    foreach ($key in $Keys) {
        if ($builder.ContainsKey($key)) {
            return [string]$builder[$key]
        }
    }

    return ""
}

$server = Get-ConnectionValue @("Server", "Data Source", "Address", "Addr", "Network Address")
$database = Get-ConnectionValue @("Database", "Initial Catalog")

# Strip a trailing ",<port>" (e.g. "192.168.1.237,1433") so LAN addresses compare
# the same way regardless of whether a port was supplied.
function Remove-Port([string]$value) {
    return ($value -replace ',\d+$', '')
}

$allowedServers = @(
    "localhost\SQLEXPRESS",
    ".\SQLEXPRESS",
    "(local)\SQLEXPRESS",
    "127.0.0.1\SQLEXPRESS",
    # The Mac runner has no Integrated Security path to the Windows box, so it
    # reaches the same disposable SQL Express mirror over the LAN using the
    # queenzone_probe SQL login (see docs/agent-bitwarden-secrets.md). This is
    # still the local mirror, not Azure SQL or another remote server.
    "glory11",
    "glory11\SQLEXPRESS"
)

if (-not [string]::IsNullOrWhiteSpace($env:COMPUTERNAME)) {
    $allowedServers += "$($env:COMPUTERNAME)\SQLEXPRESS"
}

if (-not [string]::IsNullOrWhiteSpace($env:SQLEXPRESS_LAN_ADDRESS)) {
    $allowedServers += (Remove-Port $env:SQLEXPRESS_LAN_ADDRESS)
}

if ($database -ne $ExpectedDatabase) {
    throw "Write probe refused database '$database'. Expected the disposable mirror '$ExpectedDatabase'."
}

if ($allowedServers -notcontains (Remove-Port $server)) {
    throw "Write probe refused server '$server'. Use the local SQL Express mirror (or its LAN address for the Mac runner); Azure SQL and other remote servers are blocked."
}

Write-Host "Confirmed disposable SQL mirror: $server / $database"
