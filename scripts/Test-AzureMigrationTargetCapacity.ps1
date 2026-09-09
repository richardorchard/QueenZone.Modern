[CmdletBinding()]
param(
    [string]$SubscriptionId = $env:ARM_SUBSCRIPTION_ID,
    [string]$ResourceGroupName = "Queenzone-RG",
    [string]$Location = "canadaeast",
    [string]$ServicePlanName = "ASP-Queenzone-Prod",
    [string]$ServicePlanSku = "B1",
    [int]$ServicePlanWorkers = 1,
    [string]$SqlServerName = "queenzone-prod-sql",
    [switch]$SelfTest
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Assert-AppServiceCapacity {
    param(
        [Parameter(Mandatory)]$UsageResponse,
        [Parameter(Mandatory)][string]$Sku,
        [Parameter(Mandatory)][int]$Workers
    )

    $usage = @($UsageResponse.value | Where-Object { $_.name.value -eq $Sku }) | Select-Object -First 1
    if ($null -eq $usage) {
        $usage = @($UsageResponse.value | Where-Object {
                $_.name.value -eq "*" -and $_.name.localizedValue -eq "Total Regional VMs"
            }) | Select-Object -First 1
    }
    if ($null -eq $usage) {
        throw "Azure did not return an App Service quota entry for SKU '$Sku' or the Total Regional VMs allowance."
    }

    $available = [int64]$usage.limit - [int64]$usage.currentValue
    if ($available -lt $Workers) {
        throw "App Service capacity is unavailable: $Sku in the target region has $available available instance(s), but $Workers are required. Request a regional quota of at least $([int64]$usage.currentValue + $Workers)."
    }
}

function Assert-SqlCapacity {
    param([Parameter(Mandatory)]$Capabilities)

    if ($Capabilities.status -ne "Available") {
        $reason = if ([string]::IsNullOrWhiteSpace([string]$Capabilities.reason)) {
            "Azure reported status '$($Capabilities.status)'."
        }
        else {
            [string]$Capabilities.reason
        }

        throw "Azure SQL logical-server provisioning is unavailable in the target region. $reason"
    }
}

if ($SelfTest) {
    Assert-AppServiceCapacity -UsageResponse ([pscustomobject]@{
            value = @([pscustomobject]@{
                    name         = [pscustomobject]@{ value = "B1" }
                    currentValue = 0
                    limit        = 1
                })
        }) -Sku "B1" -Workers 1
    Assert-SqlCapacity -Capabilities ([pscustomobject]@{ status = "Available"; reason = $null })

    Assert-AppServiceCapacity -UsageResponse ([pscustomobject]@{
            value = @([pscustomobject]@{
                    name         = [pscustomobject]@{
                        value          = "*"
                        localizedValue = "Total Regional VMs"
                    }
                    currentValue = 0
                    limit        = 30
                })
        }) -Sku "B1" -Workers 1

    $capacityRejected = $false
    try {
        Assert-AppServiceCapacity -UsageResponse ([pscustomobject]@{
                value = @([pscustomobject]@{
                        name         = [pscustomobject]@{ value = "B1" }
                        currentValue = 0
                        limit        = 0
                    })
            }) -Sku "B1" -Workers 1
    }
    catch {
        $capacityRejected = $_.Exception.Message -like "App Service capacity is unavailable:*"
    }
    if (!$capacityRejected) { throw "Self-test failed: zero App Service capacity was not rejected." }

    $sqlRejected = $false
    try {
        Assert-SqlCapacity -Capabilities ([pscustomobject]@{
                status = "Visible"
                reason = "Provisioning is restricted in this region."
            })
    }
    catch {
        $sqlRejected = $_.Exception.Message -like "Azure SQL logical-server provisioning is unavailable*"
    }
    if (!$sqlRejected) { throw "Self-test failed: restricted SQL provisioning was not rejected." }

    Write-Output "Azure migration target capacity self-test passed."
    return
}

if ([string]::IsNullOrWhiteSpace($SubscriptionId)) {
    throw "ARM_SUBSCRIPTION_ID or -SubscriptionId is required."
}

$az = Get-Command az -ErrorAction Stop
$resourcesJson = & $az.Source resource list `
    --subscription $SubscriptionId `
    --resource-group $ResourceGroupName `
    --output json
if ($LASTEXITCODE -ne 0) { throw "Could not list target resource-group resources." }
$resources = @($resourcesJson | ConvertFrom-Json)
$failures = [System.Collections.Generic.List[string]]::new()

$servicePlanExists = $null -ne ($resources | Where-Object {
        $_.name -eq $ServicePlanName -and $_.type -eq "Microsoft.Web/serverFarms"
    } | Select-Object -First 1)
if ($servicePlanExists) {
    Write-Output "App Service plan '$ServicePlanName' already exists; regional create-capacity check skipped."
}
else {
    $webUsageUrl = "https://management.azure.com/subscriptions/$SubscriptionId/providers/Microsoft.Web/locations/$Location/usages?api-version=2025-05-01"
    $webUsageJson = & $az.Source rest --method get --url $webUsageUrl --output json
    if ($LASTEXITCODE -ne 0) { throw "Could not read App Service capacity for '$Location'." }
    try {
        Assert-AppServiceCapacity -UsageResponse ($webUsageJson | ConvertFrom-Json) -Sku $ServicePlanSku -Workers $ServicePlanWorkers
        Write-Output "App Service $ServicePlanSku capacity is available in '$Location'."
    }
    catch {
        $failures.Add($_.Exception.Message)
    }
}

$sqlServerExists = $null -ne ($resources | Where-Object {
        $_.name -eq $SqlServerName -and $_.type -eq "Microsoft.Sql/servers"
    } | Select-Object -First 1)
if ($sqlServerExists) {
    Write-Output "SQL server '$SqlServerName' already exists; regional provisioning check skipped."
}
else {
    $sqlCapabilitiesUrl = "https://management.azure.com/subscriptions/$SubscriptionId/providers/Microsoft.Sql/locations/$Location/capabilities?api-version=2025-01-01"
    $sqlCapabilitiesJson = & $az.Source rest --method get --url $sqlCapabilitiesUrl --output json
    if ($LASTEXITCODE -ne 0) { throw "Could not read Azure SQL capabilities for '$Location'." }
    try {
        Assert-SqlCapacity -Capabilities ($sqlCapabilitiesJson | ConvertFrom-Json)
        Write-Output "Azure SQL logical-server provisioning is available in '$Location'."
    }
    catch {
        $failures.Add($_.Exception.Message)
    }
}

if ($failures.Count -gt 0) {
    throw "Migration target capacity preflight failed:`n - $($failures -join "`n - ")"
}

Write-Output "Azure migration target capacity checks passed."
