[CmdletBinding()]
param(
    [switch]$UseRemoteBackend,
    [ValidateSet("production", "dev")]
    [string[]]$EnvironmentName = @("production", "dev")
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path $PSScriptRoot -Parent
$infraPath = Join-Path $repositoryRoot "infra"
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

foreach ($rootName in $EnvironmentName) {
    $rootPath = Join-Path $infraPath "environments/$rootName"
    $backendConfig = Join-Path $infraPath "backend/$rootName.backend.hcl"
    $initArguments = @("-chdir=$rootPath", "init", "-input=false", "-reconfigure")
    if ($UseRemoteBackend) {
        $initArguments += "-backend-config=$backendConfig"
    }
    else {
        $initArguments += "-backend=false"
    }

    & $tofu.Source @initArguments
    if ($LASTEXITCODE -ne 0) {
        throw "OpenTofu $rootName initialisation failed."
    }

    & $tofu.Source "-chdir=$rootPath" validate
    if ($LASTEXITCODE -ne 0) {
        throw "OpenTofu $rootName validation failed."
    }

}

$webModulePath = Join-Path $infraPath "modules/azure-web"
& $tofu.Source "-chdir=$webModulePath" init -backend=false -input=false
if ($LASTEXITCODE -ne 0) { throw "Azure web module test initialisation failed." }
& $tofu.Source "-chdir=$webModulePath" test
if ($LASTEXITCODE -ne 0) { throw "Azure web module contract tests failed." }

Write-Output "OpenTofu format, safety, initialisation, and root/module validation checks passed."
