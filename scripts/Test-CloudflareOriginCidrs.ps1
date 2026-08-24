[CmdletBinding()]
param(
    [string]$InfraPath = (Join-Path $PSScriptRoot "../infra")
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$azureWebMain = Join-Path $InfraPath "modules/azure-web/main.tf"
if (-not (Test-Path -LiteralPath $azureWebMain)) {
    throw "Azure web module not found at $azureWebMain."
}

$hcl = Get-Content -LiteralPath $azureWebMain -Raw
$allowed = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($match in [regex]::Matches($hcl, 'ip_address\s*=\s*"(?<cidrs>[^"]+)"')) {
    foreach ($cidr in $match.Groups["cidrs"].Value.Split(",", [System.StringSplitOptions]::RemoveEmptyEntries)) {
        [void]$allowed.Add($cidr.Trim())
    }
}

if ($allowed.Count -eq 0) {
    throw "No App Service Cloudflare ip_address entries were found in $azureWebMain."
}

$published = Invoke-RestMethod -Uri "https://api.cloudflare.com/client/v4/ips"
if (-not $published.success) {
    throw "Cloudflare IP list request failed."
}

$missing = [System.Collections.Generic.List[string]]::new()
foreach ($cidr in @($published.result.ipv4_cidrs) + @($published.result.ipv6_cidrs)) {
    if (-not $allowed.Contains([string]$cidr)) {
        $missing.Add([string]$cidr)
    }
}

if ($missing.Count -gt 0) {
    $missing | ForEach-Object { Write-Error "App Service origin allow list is missing Cloudflare CIDR $_." }
    exit 1
}

Write-Output ("Cloudflare origin CIDR check passed. Allowed {0} ranges covering {1} published IPv4 and {2} published IPv6 prefixes." -f $allowed.Count, @($published.result.ipv4_cidrs).Count, @($published.result.ipv6_cidrs).Count)
