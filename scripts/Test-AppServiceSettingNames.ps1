[CmdletBinding()]
param(
    [string]$SiteName = "queenzone-dev",
    [string]$ResourceGroup = "Queenzone-RG",
    [string]$InventoryPath = (Join-Path $PSScriptRoot "../infra/import/github-bitwarden.json")
)

# Scope item 5 of #618 (ADR 0008): detect a missing required App Service
# setting *name* without ever reading or printing a value. Only names are
# requested from Azure (--query "[].name") and only names are compared.
#
# `az webapp config appsettings list` requires Reader-level access to the
# site's config, which the deploy environment's OIDC identity already has
# (see .github/workflows/deploy.yml's "configure-app-settings" job).

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$az = Get-Command az -ErrorAction Stop

$inventory = Get-Content -LiteralPath $InventoryPath -Raw | ConvertFrom-Json
$requiredNames = @($inventory.appServiceSettingNames) | Sort-Object -Unique

if ($requiredNames.Count -eq 0) {
    throw "$InventoryPath has no appServiceSettingNames entries; nothing to check."
}

$liveNamesJson = & $az.Source webapp config appsettings list `
    --name $SiteName `
    --resource-group $ResourceGroup `
    --query "[].name" `
    -o json
if ($LASTEXITCODE -ne 0) {
    throw "az webapp config appsettings list failed for $SiteName/$ResourceGroup."
}
$liveNames = @(($liveNamesJson | ConvertFrom-Json)) | Sort-Object -Unique

$missing = @($requiredNames | Where-Object { $_ -notin $liveNames })
$unexpected = @($liveNames | Where-Object { $_ -notin $requiredNames })

if ($missing.Count -gt 0) {
    Write-Error "Missing required App Service setting name(s) on ${SiteName}: $($missing -join ', ')"
}

if ($unexpected.Count -gt 0) {
    # Not a failure: new settings show up ahead of the inventory doc being
    # updated. Surface them so the inventory/ADR stay honest over time.
    Write-Warning "App Service setting name(s) on $SiteName not present in $InventoryPath (update the inventory if intentional): $($unexpected -join ', ')"
}

if ($missing.Count -gt 0) {
    exit 1
}

Write-Output "All $($requiredNames.Count) required App Service setting names are present on $SiteName."
