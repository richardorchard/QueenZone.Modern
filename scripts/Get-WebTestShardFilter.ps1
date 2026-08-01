<#
.SYNOPSIS
  Builds a deterministic xUnit --filter for one mixed shard of QueenZone.Web.Tests.

.DESCRIPTION
  Discovers public test classes under tests/QueenZone.Web.Tests and assigns each to a
  shard with a greedy weight balance. WebApplicationFactory / QueenZoneWebApplicationFactory
  classes get a higher weight so heavy HTTP integration tests are spread across shards
  while still mixing with light unit tests in every shard.

  IMPORTANT (see issue #442): do NOT partition as "all unit vs all WAF". Isolating every
  WebApplicationFactory class into one job increases host contention and can make that
  job slower than today's single-suite run. Mixed shards are required.

.PARAMETER ShardIndex
  Zero-based shard index.

.PARAMETER ShardCount
  Total number of shards (must be >= 1).

.PARAMETER TestsRoot
  Path to QueenZone.Web.Tests sources. Defaults to repo-relative tests/QueenZone.Web.Tests.

.PARAMETER List
  Print class-to-shard assignment instead of a filter string.

.PARAMETER Filter
  Emit the xUnit filter string (default when -List is not set).

.EXAMPLE
  pwsh -File ./scripts/Get-WebTestShardFilter.ps1 -ShardIndex 0 -ShardCount 2

.EXAMPLE
  pwsh -File ./scripts/Get-WebTestShardFilter.ps1 -ShardCount 2 -List
#>
[CmdletBinding()]
param(
    [Parameter()]
    [ValidateRange(0, 63)]
    [int] $ShardIndex = 0,

    [Parameter()]
    [ValidateRange(1, 64)]
    [int] $ShardCount = 2,

    [Parameter()]
    [string] $TestsRoot = "",

    [Parameter()]
    [switch] $List,

    [Parameter()]
    [switch] $Filter
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($ShardIndex -ge $ShardCount) {
    throw "ShardIndex ($ShardIndex) must be less than ShardCount ($ShardCount)."
}

if ([string]::IsNullOrWhiteSpace($TestsRoot)) {
    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
    $TestsRoot = Join-Path $repoRoot "tests/QueenZone.Web.Tests"
}

$TestsRoot = (Resolve-Path -LiteralPath $TestsRoot).Path
if (-not (Test-Path -LiteralPath $TestsRoot -PathType Container)) {
    throw "Tests root not found: $TestsRoot"
}

# Higher weight for WAF hosts so they spread across shards. Keep weights modest so
# light tests still fill every shard (mixed composition is the whole point).
$script:WafWeight = 5
$script:UnitWeight = 1

function Test-IsWafSource {
    param([string] $Text)
    return $Text -match 'IClassFixture<\s*(?:WebApplicationFactory|QueenZoneWebApplicationFactory)' -or
        $Text -match 'WebApplicationFactory<\s*Program\s*>'
}

function Get-TestClasses {
    param([string] $Root)

    $classes = @()
    Get-ChildItem -LiteralPath $Root -Filter *.cs -File | ForEach-Object {
        $text = Get-Content -LiteralPath $_.FullName -Raw
        if ($text -notmatch '\[Fact|\[Theory') {
            return
        }

        $isWaf = Test-IsWafSource -Text $text
        $matches = [regex]::Matches(
            $text,
            'public\s+(?:sealed\s+)?(?:partial\s+)?class\s+(\w+)\b')

        foreach ($match in $matches) {
            $name = $match.Groups[1].Value
            # Convention: xUnit classes end in Tests. Helpers/collections/factories do not.
            if ($name -notmatch 'Tests$') {
                continue
            }

            $classes += [pscustomobject]@{
                Name   = $name
                IsWaf  = $isWaf
                Weight = $(if ($isWaf) { $script:WafWeight } else { $script:UnitWeight })
                File   = $_.Name
            }
        }
    }

    # Deduplicate partial classes; keep max weight if any part is WAF.
    $classes |
        Group-Object Name |
        ForEach-Object {
            $isWaf = [bool]($_.Group | Where-Object IsWaf | Select-Object -First 1)
            [pscustomobject]@{
                Name   = $_.Name
                IsWaf  = $isWaf
                Weight = $(if ($isWaf) { $script:WafWeight } else { $script:UnitWeight })
                File   = ($_.Group | Select-Object -First 1).File
            }
        } |
        Sort-Object Name
}

function Get-ShardAssignments {
    param(
        [object[]] $Classes,
        [int] $Count
    )

    $loads = @(for ($i = 0; $i -lt $Count; $i++) { 0 })
    $buckets = @(for ($i = 0; $i -lt $Count; $i++) { New-Object System.Collections.Generic.List[object] })

    # Heaviest first, then name — deterministic and packs WAF classes evenly.
    $ordered = $Classes | Sort-Object @{ Expression = "Weight"; Descending = $true }, Name
    foreach ($class in $ordered) {
        $best = 0
        for ($i = 1; $i -lt $Count; $i++) {
            if ($loads[$i] -lt $loads[$best]) {
                $best = $i
            }
        }

        $buckets[$best].Add($class) | Out-Null
        $loads[$best] += $class.Weight
    }

    for ($i = 0; $i -lt $Count; $i++) {
        [pscustomobject]@{
            ShardIndex = $i
            Load       = $loads[$i]
            Classes    = @($buckets[$i] | Sort-Object Name)
        }
    }
}

$allClasses = @(Get-TestClasses -Root $TestsRoot)
if ($allClasses.Count -eq 0) {
    throw "No test classes discovered under $TestsRoot."
}

$assignments = @(Get-ShardAssignments -Classes $allClasses -Count $ShardCount)
$shard = $assignments | Where-Object { $_.ShardIndex -eq $ShardIndex } | Select-Object -First 1
if ($null -eq $shard -or $shard.Classes.Count -eq 0) {
    throw "Shard $ShardIndex of $ShardCount has no classes (discovery produced $($allClasses.Count) classes)."
}

if ($List) {
    foreach ($assignment in $assignments) {
        $waf = @($assignment.Classes | Where-Object IsWaf).Count
        $unit = $assignment.Classes.Count - $waf
        Write-Host ("Shard {0}: classes={1} waf={2} unit={3} weight={4}" -f `
                $assignment.ShardIndex, $assignment.Classes.Count, $waf, $unit, $assignment.Load)
        foreach ($class in $assignment.Classes) {
            $kind = if ($class.IsWaf) { "WAF" } else { "unit" }
            Write-Host ("  [{0}] {1} (w={2})" -f $kind, $class.Name, $class.Weight)
        }
    }

    return
}

# Default: emit filter string for the requested shard.
$parts = @(
    $shard.Classes | ForEach-Object { "FullyQualifiedName~.$($_.Name)" }
)
$filterText = $parts -join "|"
Write-Output $filterText
