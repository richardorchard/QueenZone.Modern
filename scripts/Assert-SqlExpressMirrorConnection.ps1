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
$allowedServers = @(
    "localhost\SQLEXPRESS",
    ".\SQLEXPRESS",
    "(local)\SQLEXPRESS",
    "127.0.0.1\SQLEXPRESS"
)

if (-not [string]::IsNullOrWhiteSpace($env:COMPUTERNAME)) {
    $allowedServers += "$($env:COMPUTERNAME)\SQLEXPRESS"
}

if ($database -ne $ExpectedDatabase) {
    throw "Write probe refused database '$database'. Expected the disposable mirror '$ExpectedDatabase'."
}

if ($allowedServers -notcontains $server) {
    throw "Write probe refused server '$server'. Use the local SQL Express mirror; Azure SQL and remote servers are blocked."
}

Write-Host "Confirmed disposable SQL mirror: $server / $database"
