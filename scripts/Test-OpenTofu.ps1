[CmdletBinding()]
param(
    [switch]$UseRemoteBackend
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path $PSScriptRoot -Parent
$infraPath = Join-Path $repositoryRoot "infra"
$productionPath = Join-Path $infraPath "environments/production"
$backendConfig = Join-Path $infraPath "backend/production.backend.hcl"
$expectedVersion = (Get-Content -LiteralPath (Join-Path $repositoryRoot ".opentofu-version") -Raw).Trim()
$tofu = Get-Command tofu -ErrorAction Stop
$actualVersion = (& $tofu.Source version -json | ConvertFrom-Json).terraform_version
if ($actualVersion -ne $expectedVersion) {
    throw "OpenTofu $expectedVersion is required; found $actualVersion."
}

& $tofu.Source fmt -check -recursive $infraPath
if ($LASTEXITCODE -ne 0) {
    throw "OpenTofu formatting failed."
}

& (Join-Path $PSScriptRoot "Test-OpenTofuSafety.ps1") -InfraPath $infraPath
if ($LASTEXITCODE -ne 0) {
    throw "OpenTofu safety checks failed."
}

& (Join-Path $PSScriptRoot "Test-CloudflareOriginCidrs.ps1") -InfraPath $infraPath
if ($LASTEXITCODE -ne 0) {
    throw "Cloudflare origin CIDR coverage check failed."
}

$initArguments = @("-chdir=$productionPath", "init", "-input=false", "-reconfigure")
if ($UseRemoteBackend) {
    $initArguments += "-backend-config=$backendConfig"
}
else {
    $initArguments += "-backend=false"
}

& $tofu.Source @initArguments
if ($LASTEXITCODE -ne 0) {
    throw "OpenTofu production initialisation failed."
}

& $tofu.Source "-chdir=$productionPath" validate
if ($LASTEXITCODE -ne 0) {
    throw "OpenTofu production validation failed."
}

Write-Output "OpenTofu format, safety, initialisation, and root/module validation checks passed."
