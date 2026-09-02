<#
.SYNOPSIS
  Builds a deterministic xUnit --filter for one mixed shard of QueenZone.Web.Tests.

.DESCRIPTION
  Discovers public test classes under tests/QueenZone.Web.Tests and assigns each to a
  shard with a greedy weight balance. Weight is case-count times a host-kind
  multiplier so large in-memory WAF suites and EF-backed hosts spread, while
  every shard still mixes light unit tests with heavier HTTP tests.

  IMPORTANT (see issue #442): do NOT partition as "all unit vs all WAF". Isolating every
  WebApplicationFactory class into one job increases host contention and can make that
  job slower than today's single-suite run. Mixed shards are required.

.PARAMETER ShardIndex
  Zero-based shard index.

.PARAMETER ShardCount
  Total number of shards (must be >= 1).

.PARAMETER TestsRoot
  Path to QueenZone.Web.Tests sources. Defaults to repo-relative tests/QueenZone.Web.Tests.

.PARAMETER DurationsPath
  Optional JSON map of test class names to observed milliseconds. When present,
  measured durations replace heuristic weights for known classes.

.PARAMETER List
  Print class-to-shard assignment instead of a filter string.

.PARAMETER Filter
  Emit the xUnit filter string (default when -List is not set).

.PARAMETER SelfTest
  Run fixture-based assertions against the weight and assignment helpers, then exit.

.EXAMPLE
  pwsh -File ./scripts/Get-WebTestShardFilter.ps1 -ShardIndex 0 -ShardCount 4

.EXAMPLE
  pwsh -File ./scripts/Get-WebTestShardFilter.ps1 -ShardCount 4 -List
#>
[CmdletBinding()]
param(
    [Parameter()]
    [ValidateRange(0, 63)]
    [int] $ShardIndex = 0,

    [Parameter()]
    [ValidateRange(1, 64)]
    [int] $ShardCount = 4,

    [Parameter()]
    [string] $TestsRoot = "",

    [Parameter()]
    [string] $DurationsPath = "",

    [Parameter()]
    [switch] $List,

    [Parameter()]
    [switch] $Filter,

    [Parameter()]
    [switch] $SelfTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$script:ObservedDurations = @{}

# Per-case multipliers. A flat WAF=5 / unit=1 (one weight per class) kept
# class counts even but parked every Admin*EfRoutes host on shard 1 via
# alphabetical pairing — that shard's test step was ~2x the other.
$script:UnitCaseWeight = 1
$script:SqliteUnitCaseWeight = 2
$script:WafCaseWeight = 5
$script:ProductionWafCaseWeight = 10
$script:EfWafCaseWeight = 20

function Test-IsWafSource {
    param([string] $Text)
    return $Text -match 'IClassFixture<\s*(?:WebApplicationFactory|QueenZoneWebApplicationFactory)' -or
        $Text -match 'WebApplicationFactory<\s*Program\s*>'
}

function Test-IsEfWafSource {
    param(
        [bool] $IsWaf,
        [string] $Text
    )
    if (-not $IsWaf) {
        return $false
    }

    return $Text -match 'IAsyncLifetime' -or
        $Text -match 'AdminEfWebTestHarness' -or
        $Text -match '\.UseSqlite\('
}

function Test-IsProductionWafSource {
    param(
        [bool] $IsWaf,
        [string] $Text
    )
    return $IsWaf -and $Text -match 'UseEnvironment\(\s*"Production"\s*\)'
}

function Get-CaseCount {
    param([string] $Text)

    $facts = [regex]::Matches($Text, '\[Fact\b').Count
    $theories = [regex]::Matches($Text, '\[Theory\b').Count
    $inline = [regex]::Matches($Text, '\[InlineData\b').Count
    $member = [regex]::Matches($Text, '\[(?:MemberData|ClassData)\b').Count
    $cases = $facts + [Math]::Max($inline + $member, $theories)
    if ($cases -lt 1) {
        return 1
    }

    return $cases
}

function Get-ClassKind {
    param(
        [bool] $IsWaf,
        [bool] $IsEfWaf,
        [bool] $IsProductionWaf,
        [bool] $IsSqliteUnit
    )

    if ($IsEfWaf) {
        return "EF-WAF"
    }

    if ($IsProductionWaf) {
        return "PROD-WAF"
    }

    if ($IsWaf) {
        return "WAF"
    }

    if ($IsSqliteUnit) {
        return "sqlite"
    }

    return "unit"
}

function Get-ClassWeight {
    param(
        [int] $Cases,
        [bool] $IsWaf,
        [bool] $IsEfWaf,
        [bool] $IsProductionWaf,
        [bool] $IsSqliteUnit
    )

    $multiplier = $script:UnitCaseWeight
    if ($IsEfWaf) {
        $multiplier = $script:EfWafCaseWeight
    }
    elseif ($IsProductionWaf) {
        $multiplier = $script:ProductionWafCaseWeight
    }
    elseif ($IsWaf) {
        $multiplier = $script:WafCaseWeight
    }
    elseif ($IsSqliteUnit) {
        $multiplier = $script:SqliteUnitCaseWeight
    }

    return $Cases * $multiplier
}

function Get-TestClasses {
    param([string] $Root)

    $classes = @()
    Get-ChildItem -LiteralPath $Root -Filter *.cs -File | ForEach-Object {
        $text = Get-Content -LiteralPath $_.FullName -Raw
        if ($text -notmatch '\[Fact|\[Theory') {
            return
        }

        $classMatches = [regex]::Matches(
            $text,
            'public\s+(?:sealed\s+)?(?:partial\s+)?class\s+(\w+)\b')

        $testMatches = @($classMatches | Where-Object { $_.Groups[1].Value -match 'Tests$' })
        for ($i = 0; $i -lt $testMatches.Count; $i++) {
            $match = $testMatches[$i]
            $endIndex = if ($i -lt $testMatches.Count - 1) {
                $testMatches[$i + 1].Index
            }
            else {
                $text.Length
            }

            $body = $text.Substring($match.Index, $endIndex - $match.Index)
            $isWaf = Test-IsWafSource -Text $body
            $isEfWaf = Test-IsEfWafSource -IsWaf $isWaf -Text $body
            $isProductionWaf = Test-IsProductionWafSource -IsWaf $isWaf -Text $body
            $isSqliteUnit = (-not $isWaf) -and ($body -match '\.UseSqlite\(')
            $cases = Get-CaseCount -Text $body

            $classes += [pscustomobject]@{
                Name            = $match.Groups[1].Value
                IsWaf           = $isWaf
                IsEfWaf         = $isEfWaf
                IsProductionWaf = $isProductionWaf
                IsSqliteUnit    = $isSqliteUnit
                Kind            = Get-ClassKind -IsWaf $isWaf -IsEfWaf $isEfWaf -IsProductionWaf $isProductionWaf -IsSqliteUnit $isSqliteUnit
                Cases           = $cases
                Weight          = Get-ClassWeight -Cases $cases -IsWaf $isWaf -IsEfWaf $isEfWaf -IsProductionWaf $isProductionWaf -IsSqliteUnit $isSqliteUnit
                File            = $_.Name
            }
        }
    }

    # Deduplicate partial classes: OR flags, sum cases, recompute weight.
    $classes |
        Group-Object Name |
        ForEach-Object {
            $isWaf = [bool]($_.Group | Where-Object IsWaf | Select-Object -First 1)
            $isEfWaf = [bool]($_.Group | Where-Object IsEfWaf | Select-Object -First 1)
            $isProductionWaf = [bool]($_.Group | Where-Object IsProductionWaf | Select-Object -First 1)
            $isSqliteUnit = [bool]($_.Group | Where-Object IsSqliteUnit | Select-Object -First 1)
            $cases = ($_.Group | Measure-Object Cases -Sum).Sum
            [pscustomobject]@{
                Name            = $_.Name
                IsWaf           = $isWaf
                IsEfWaf         = $isEfWaf
                IsProductionWaf = $isProductionWaf
                IsSqliteUnit    = $isSqliteUnit
                Kind            = Get-ClassKind -IsWaf $isWaf -IsEfWaf $isEfWaf -IsProductionWaf $isProductionWaf -IsSqliteUnit $isSqliteUnit
                Cases           = $cases
                Weight          = Get-ClassWeight -Cases $cases -IsWaf $isWaf -IsEfWaf $isEfWaf -IsProductionWaf $isProductionWaf -IsSqliteUnit $isSqliteUnit
                File            = ($_.Group | Select-Object -First 1).File
            }
        } |
        Sort-Object Name
}

function Get-ShardAssignments {
    param(
        [object[]] $Classes,
        [int] $Count
    )

    $loads = @(for ($i = 0; $i -lt $Count; $i++) { [long]0 })
    $buckets = @(for ($i = 0; $i -lt $Count; $i++) { New-Object System.Collections.Generic.List[object] })

    # Heaviest first, then name — deterministic and packs expensive hosts first.
    $weightedClasses = @(
        foreach ($class in $Classes) {
            $effectiveWeight = [long]$class.Weight * 1000
            if ($script:ObservedDurations.ContainsKey($class.Name)) {
                $effectiveWeight = [Math]::Max(1, [long]$script:ObservedDurations[$class.Name])
            }
            $class | Add-Member -NotePropertyName EffectiveWeight -NotePropertyValue $effectiveWeight -Force -PassThru
        }
    )

    $ordered = $weightedClasses | Sort-Object @{ Expression = "EffectiveWeight"; Descending = $true }, Name
    foreach ($class in $ordered) {
        $best = 0
        for ($i = 1; $i -lt $Count; $i++) {
            if ($loads[$i] -lt $loads[$best]) {
                $best = $i
            }
        }

        $buckets[$best].Add($class) | Out-Null
        $loads[$best] += $class.EffectiveWeight
    }

    for ($i = 0; $i -lt $Count; $i++) {
        [pscustomobject]@{
            ShardIndex = $i
            Load       = $loads[$i]
            Classes    = @($buckets[$i] | Sort-Object Name)
        }
    }
}

function Assert-SelfTestEqual {
    param(
        $Actual,
        $Expected,
        [string] $Message
    )

    if ($Actual -ne $Expected) {
        throw "Self-test failed: $Message (expected '$Expected', got '$Actual')."
    }
}

function Invoke-ShardFilterSelfTest {
    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("qz-shard-filter-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $tempRoot | Out-Null
    try {
        Set-Content -LiteralPath (Join-Path $tempRoot "TinyUnitTests.cs") -Value @"
public sealed class TinyUnitTests
{
    [Fact]
    public void One() {}
}
"@

        Set-Content -LiteralPath (Join-Path $tempRoot "HugeUnitTests.cs") -Value @"
public sealed class HugeUnitTests
{
    [Fact] public void A() {}
    [Fact] public void B() {}
    [Fact] public void C() {}
    [Fact] public void D() {}
    [Fact] public void E() {}
    [Fact] public void F() {}
    [Fact] public void G() {}
    [Fact] public void H() {}
    [Fact] public void I() {}
    [Fact] public void J() {}
}
"@

        Set-Content -LiteralPath (Join-Path $tempRoot "SmallWafTests.cs") -Value @"
public sealed class SmallWafTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    [Fact]
    public void One() {}
}
"@

        Set-Content -LiteralPath (Join-Path $tempRoot "EfWafATests.cs") -Value @"
public sealed class EfWafATests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    [Fact] public void One() {}
    [Fact] public void Two() {}
}
"@

        Set-Content -LiteralPath (Join-Path $tempRoot "EfWafBTests.cs") -Value @"
public sealed class EfWafBTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    [Fact] public void One() {}
    [Fact] public void Two() {}
}
"@

        Set-Content -LiteralPath (Join-Path $tempRoot "ProdWafTests.cs") -Value @"
public sealed class ProdWafTests : IClassFixture<WebApplicationFactory<Program>>
{
    public ProdWafTests()
    {
        builder.UseEnvironment("Production");
    }

    [Fact]
    public void One() {}

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Two(int value) {}
}
"@

        Set-Content -LiteralPath (Join-Path $tempRoot "SqliteUnitTests.cs") -Value @"
public sealed class SqliteUnitTests
{
    public SqliteUnitTests()
    {
        builder.UseSqlite(connection);
    }

    [Fact]
    public void One() {}
    [Fact]
    public void Two() {}
}
"@

        $discovered = @(Get-TestClasses -Root $tempRoot)
        Assert-SelfTestEqual $discovered.Count 7 "fixture class count"

        $byName = @{}
        foreach ($class in $discovered) {
            $byName[$class.Name] = $class
        }

        Assert-SelfTestEqual $byName["TinyUnitTests"].Weight 1 "tiny unit weight"
        Assert-SelfTestEqual $byName["TinyUnitTests"].Kind "unit" "tiny unit kind"
        Assert-SelfTestEqual $byName["HugeUnitTests"].Weight 10 "huge unit 10 facts"
        Assert-SelfTestEqual $byName["SmallWafTests"].Weight 5 "small WAF 1 fact * 5"
        Assert-SelfTestEqual $byName["EfWafATests"].Weight 40 "EF WAF 2 facts * 20"
        Assert-SelfTestEqual $byName["EfWafATests"].Kind "EF-WAF" "EF WAF kind"
        Assert-SelfTestEqual $byName["EfWafBTests"].Weight 40 "second EF WAF weight"
        Assert-SelfTestEqual $byName["ProdWafTests"].Cases 3 "prod WAF fact + 2 InlineData"
        Assert-SelfTestEqual $byName["ProdWafTests"].Weight 30 "prod WAF 3 cases * 10"
        Assert-SelfTestEqual $byName["ProdWafTests"].Kind "PROD-WAF" "prod WAF kind"
        Assert-SelfTestEqual $byName["SqliteUnitTests"].Weight 4 "sqlite unit 2 facts * 2"
        Assert-SelfTestEqual $byName["SqliteUnitTests"].Kind "sqlite" "sqlite kind"

        $assignments = @(Get-ShardAssignments -Classes $discovered -Count 2)
        $efShards = @(
            foreach ($assignment in $assignments) {
                foreach ($class in $assignment.Classes) {
                    if ($class.Name -in @("EfWafATests", "EfWafBTests")) {
                        $assignment.ShardIndex
                    }
                }
            }
        ) | Sort-Object -Unique
        Assert-SelfTestEqual $efShards.Count 2 "equal-weight EF WAF hosts must split across shards"

        $loads = @($assignments | ForEach-Object { $_.Load })
        $spread = [Math]::Abs($loads[0] - $loads[1])
        if ($spread -gt 20000) {
            throw "Self-test failed: shard loads $($loads[0]) vs $($loads[1]) differ by more than 20000."
        }
    }
    finally {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    Write-Output "Get-WebTestShardFilter self-test passed."
}

if ($SelfTest) {
    Invoke-ShardFilterSelfTest
    return
}

if ($ShardIndex -ge $ShardCount) {
    throw "ShardIndex ($ShardIndex) must be less than ShardCount ($ShardCount)."
}

if ([string]::IsNullOrWhiteSpace($TestsRoot)) {
    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
    $TestsRoot = Join-Path $repoRoot "tests/QueenZone.Web.Tests"
}

if ([string]::IsNullOrWhiteSpace($DurationsPath)) {
    $DurationsPath = Join-Path $PSScriptRoot "web-test-class-durations.json"
}
if (Test-Path -LiteralPath $DurationsPath -PathType Leaf) {
    $durationJson = Get-Content -LiteralPath $DurationsPath -Raw | ConvertFrom-Json -AsHashtable
    foreach ($entry in $durationJson.GetEnumerator()) {
        if ($entry.Value -isnot [int] -and $entry.Value -isnot [long]) {
            throw "Observed duration for '$($entry.Key)' must be an integer number of milliseconds."
        }
        $script:ObservedDurations[$entry.Key] = [long]$entry.Value
    }
}

$TestsRoot = (Resolve-Path -LiteralPath $TestsRoot).Path
if (-not (Test-Path -LiteralPath $TestsRoot -PathType Container)) {
    throw "Tests root not found: $TestsRoot"
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
        $cases = ($assignment.Classes | Measure-Object Cases -Sum).Sum
        Write-Host ("Shard {0}: classes={1} waf={2} unit={3} cases={4} weight={5}" -f `
                $assignment.ShardIndex, $assignment.Classes.Count, $waf, $unit, $cases, $assignment.Load)
        foreach ($class in $assignment.Classes) {
            Write-Host ("  [{0}] {1} (cases={2} heuristic={3} effective-ms={4})" -f $class.Kind, $class.Name, $class.Cases, $class.Weight, $class.EffectiveWeight)
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
