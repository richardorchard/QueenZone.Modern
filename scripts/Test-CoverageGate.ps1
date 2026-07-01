param(
    [Parameter(Mandatory = $true)]
    [string]$Reports,

    [double]$GlobalLineThreshold = 51,

    [double]$ChangedLineThreshold = 80,

    [string]$BaseRef = $env:GITHUB_BASE_REF,

    [string]$HeadRef = "HEAD"
)

$ErrorActionPreference = "Stop"

function Get-RepoRelativePath {
    param([string]$Path)

    $root = (Get-Location).Path
    if (-not $root.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $root += [System.IO.Path]::DirectorySeparatorChar
    }

    $rootUri = [Uri]::new($root)
    $pathUri = [Uri]::new($Path)
    $relativeUri = $rootUri.MakeRelativeUri($pathUri)

    return [Uri]::UnescapeDataString($relativeUri.ToString())
}

function Convert-ToRepoPath {
    param(
        [string]$Path,
        [string[]]$Sources
    )

    $normalizedPath = $Path.Replace('\', [System.IO.Path]::DirectorySeparatorChar).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    $candidatePaths = @()

    if ([System.IO.Path]::IsPathRooted($normalizedPath)) {
        $candidatePaths += $normalizedPath
    }
    else {
        foreach ($source in $Sources) {
            $candidatePaths += [System.IO.Path]::GetFullPath((Join-Path $source $normalizedPath))
        }
    }

    foreach ($candidatePath in $candidatePaths) {
        if (Test-Path -LiteralPath $candidatePath) {
            return (Get-RepoRelativePath -Path $candidatePath)
        }
    }

    return ($Path -replace '\\', '/')
}

function Get-ChangedLines {
    param(
        [string]$BaseRef,
        [string]$HeadRef
    )

    if ([string]::IsNullOrWhiteSpace($BaseRef)) {
        Write-Host "No base ref supplied; skipping changed-line coverage gate."
        return @{}
    }

    $resolvedBaseRef = $BaseRef
    if ($BaseRef -notmatch '^origin/') {
        $remoteRef = "origin/$BaseRef"
        git rev-parse --verify --quiet $remoteRef *> $null
        if ($LASTEXITCODE -eq 0) {
            $resolvedBaseRef = $remoteRef
        }
    }

    git rev-parse --verify --quiet $resolvedBaseRef *> $null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Base ref '$BaseRef' is not available locally; skipping changed-line coverage gate."
        return @{}
    }

    $diffLines = git diff --unified=0 --no-color "$resolvedBaseRef...$HeadRef" -- '*.cs'
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to calculate changed lines against '$resolvedBaseRef'."
    }

    $changedLines = @{}
    $currentFile = $null

    foreach ($line in $diffLines) {
        if ($line -match '^\+\+\+ b/(.+)$') {
            $currentFile = $Matches[1]
            if (-not $changedLines.ContainsKey($currentFile)) {
                $changedLines[$currentFile] = [System.Collections.Generic.HashSet[int]]::new()
            }
            continue
        }

        if ($null -eq $currentFile) {
            continue
        }

        if ($line -match '^@@ -\d+(?:,\d+)? \+(\d+)(?:,(\d+))? @@') {
            $startLine = [int]$Matches[1]
            $lineCount = if ($Matches[2]) { [int]$Matches[2] } else { 1 }

            for ($offset = 0; $offset -lt $lineCount; $offset++) {
                [void]$changedLines[$currentFile].Add($startLine + $offset)
            }
        }
    }

    return $changedLines
}

$reportFiles = Get-ChildItem -Path $Reports -Recurse -Filter "coverage.cobertura.xml" -File
if ($reportFiles.Count -eq 0) {
    throw "No Cobertura coverage reports found under '$Reports'."
}

$totalLinesValid = 0
$totalLinesCovered = 0
$coveredLinesByFile = @{}

foreach ($reportFile in $reportFiles) {
    [xml]$coverage = Get-Content -LiteralPath $reportFile.FullName
    $totalLinesValid += [int]$coverage.coverage.'lines-valid'
    $totalLinesCovered += [int]$coverage.coverage.'lines-covered'

    $sources = @($coverage.coverage.sources.source | ForEach-Object {
        if ($_ -is [string]) {
            $_
        }
        elseif ($_.InnerText) {
            $_.InnerText
        }
        else {
            $_.'#text'
        }
    } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

    if ($sources.Count -eq 0) {
        $sources = @((Get-Location).Path)
    }

    foreach ($class in $coverage.coverage.packages.package.classes.class) {
        $repoPath = Convert-ToRepoPath -Path $class.filename -Sources $sources

        if (-not $coveredLinesByFile.ContainsKey($repoPath)) {
            $coveredLinesByFile[$repoPath] = @{}
        }

        foreach ($line in $class.lines.line) {
            $lineNumber = [int]$line.number
            $hits = [int]$line.hits

            if (-not $coveredLinesByFile[$repoPath].ContainsKey($lineNumber)) {
                $coveredLinesByFile[$repoPath][$lineNumber] = 0
            }

            $coveredLinesByFile[$repoPath][$lineNumber] += $hits
        }
    }
}

if ($totalLinesValid -eq 0) {
    throw "Coverage report contains no valid lines."
}

$globalLineCoverage = [math]::Round(($totalLinesCovered / $totalLinesValid) * 100, 2)
Write-Host "Global line coverage: $globalLineCoverage% ($totalLinesCovered/$totalLinesValid)"

if ($globalLineCoverage -lt $GlobalLineThreshold) {
    throw "Global line coverage $globalLineCoverage% is below the required $GlobalLineThreshold%."
}

$changedLines = Get-ChangedLines -BaseRef $BaseRef -HeadRef $HeadRef
if ($changedLines.Count -eq 0) {
    Write-Host "No changed C# lines found for patch coverage."
    exit 0
}

$changedCoverableLines = 0
$changedCoveredLines = 0
$uncoveredChangedLines = @()

foreach ($file in $changedLines.Keys) {
    if (-not $coveredLinesByFile.ContainsKey($file)) {
        continue
    }

    foreach ($lineNumber in $changedLines[$file]) {
        if (-not $coveredLinesByFile[$file].ContainsKey($lineNumber)) {
            continue
        }

        $changedCoverableLines++

        if ($coveredLinesByFile[$file][$lineNumber] -gt 0) {
            $changedCoveredLines++
        }
        else {
            $uncoveredChangedLines += "${file}:${lineNumber}"
        }
    }
}

if ($changedCoverableLines -eq 0) {
    Write-Host "Changed C# lines do not overlap coverable lines in the Cobertura report."
    exit 0
}

$changedLineCoverage = [math]::Round(($changedCoveredLines / $changedCoverableLines) * 100, 2)
Write-Host "Changed-line coverage: $changedLineCoverage% ($changedCoveredLines/$changedCoverableLines)"

if ($changedLineCoverage -lt $ChangedLineThreshold) {
    $sample = $uncoveredChangedLines | Select-Object -First 20
    Write-Host "Uncovered changed lines:"
    $sample | ForEach-Object { Write-Host "  $_" }

    if ($uncoveredChangedLines.Count -gt $sample.Count) {
        Write-Host "  ...and $($uncoveredChangedLines.Count - $sample.Count) more."
    }

    throw "Changed-line coverage $changedLineCoverage% is below the required $ChangedLineThreshold%."
}
