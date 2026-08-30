<#
.SYNOPSIS
  Builds the Web.Tests class-duration map used by CI shard balancing.

.DESCRIPTION
  Reads VSTest TRX files, sums observed duration by xUnit test class, and writes
  a stable JSON map in milliseconds. When an existing map is supplied, a 70/30
  exponential moving average dampens hosted-runner noise.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Reports,

    [Parameter(Mandatory = $true)]
    [string] $Output,

    [Parameter()]
    [string] $Existing = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$totals = @{}
$trxFiles = @(Get-ChildItem -Path $Reports -Filter *.trx -File -Recurse |
        Where-Object { $_.Name -like "QueenZone.Web.Tests-*" })
if ($trxFiles.Count -eq 0) {
    throw "No QueenZone.Web.Tests TRX files found under '$Reports'."
}

foreach ($trxFile in $trxFiles) {
    [xml] $document = Get-Content -LiteralPath $trxFile.FullName -Raw
    $namespace = New-Object System.Xml.XmlNamespaceManager($document.NameTable)
    $namespace.AddNamespace("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

    $classesByTestId = @{}
    foreach ($definition in $document.SelectNodes("//t:TestDefinitions/t:UnitTest", $namespace)) {
        $method = $definition.SelectSingleNode("t:TestMethod", $namespace)
        if ($null -eq $method -or [string]::IsNullOrWhiteSpace($method.className)) {
            continue
        }
        $className = ([string]$method.className -split '\.')[-1]
        $classesByTestId[[string]$definition.id] = $className
    }

    foreach ($result in $document.SelectNodes("//t:Results/t:UnitTestResult", $namespace)) {
        $testId = [string]$result.testId
        if (-not $classesByTestId.ContainsKey($testId)) {
            continue
        }
        $duration = [TimeSpan]::Parse([string]$result.duration, [Globalization.CultureInfo]::InvariantCulture)
        $className = $classesByTestId[$testId]
        if (-not $totals.ContainsKey($className)) {
            $totals[$className] = 0.0
        }
        $totals[$className] += $duration.TotalMilliseconds
    }
}

if ($totals.Count -eq 0) {
    throw "TRX files contained no class-linked test durations."
}

$existingDurations = @{}
if (-not [string]::IsNullOrWhiteSpace($Existing) -and (Test-Path -LiteralPath $Existing -PathType Leaf)) {
    $existingDurations = Get-Content -LiteralPath $Existing -Raw | ConvertFrom-Json -AsHashtable
}

$outputMap = [ordered]@{}
foreach ($className in @($totals.Keys | Sort-Object)) {
    $observed = [long][Math]::Max(1, [Math]::Round($totals[$className]))
    if ($existingDurations.ContainsKey($className)) {
        $observed = [long][Math]::Round(([long]$existingDurations[$className] * 0.7) + ($observed * 0.3))
    }
    $outputMap[$className] = $observed
}

$parent = Split-Path -Parent $Output
if (-not [string]::IsNullOrWhiteSpace($parent)) {
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
}
$outputMap | ConvertTo-Json | Set-Content -LiteralPath $Output -Encoding utf8
Write-Host "Wrote $($outputMap.Count) observed class durations to $Output."
