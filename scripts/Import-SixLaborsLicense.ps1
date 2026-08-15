[CmdletBinding()]
param(
    [switch] $PersistForUser
)

$ErrorActionPreference = "Stop"

$projectId = "1c16fd2d-4bfb-4eb7-8357-b49400233490"
$secretKey = "SIXLABORS_LICENSE_KEY"
$isWindowsHost = $env:OS -eq "Windows_NT"

if ([string]::IsNullOrWhiteSpace($env:BWS_ACCESS_TOKEN) -and $isWindowsHost) {
    $env:BWS_ACCESS_TOKEN = [Environment]::GetEnvironmentVariable("BWS_ACCESS_TOKEN", "User")
}

if ([string]::IsNullOrWhiteSpace($env:BWS_ACCESS_TOKEN)) {
    throw "BWS_ACCESS_TOKEN is unavailable. Configure the authorised machine account for this host; never paste its token into chat."
}

$bws = Get-Command bws -ErrorAction SilentlyContinue
if (-not $bws) {
    throw "The Bitwarden Secrets Manager CLI (bws) is not on PATH. See docs/agent-bitwarden-secrets.md."
}

$json = & $bws.Source secret list $projectId --output json
if ($LASTEXITCODE -ne 0) {
    throw "bws could not read the Queenzone Development project. Check this host's machine account access."
}

$secrets = $json | ConvertFrom-Json
$matchingSecrets = @(
    # Pipe the parsed array separately. In current PowerShell, piping ConvertFrom-Json
    # directly can pass the whole top-level JSON array to Where-Object as one item.
    $secrets | Where-Object { $_.key -eq $secretKey }
)

if ($matchingSecrets.Count -ne 1) {
    throw "Expected exactly one $secretKey entry in the Queenzone Development project; found $($matchingSecrets.Count)."
}

$licenseValue = [string] $matchingSecrets[0].value
if ([string]::IsNullOrWhiteSpace($licenseValue)) {
    throw "$secretKey exists but has no value."
}

$env:SIXLABORS_LICENSE_KEY = $licenseValue

if ($PersistForUser) {
    if (-not $isWindowsHost) {
        throw "-PersistForUser is supported only on Windows. On macOS, import the licence into each PowerShell session instead of writing it to a shell profile."
    }

    [Environment]::SetEnvironmentVariable($secretKey, $licenseValue, "User")
}

$scope = if ($PersistForUser) { "process and Windows user environment" } else { "current process" }
Write-Output "$secretKey loaded into the $scope (length $($licenseValue.Length))."
