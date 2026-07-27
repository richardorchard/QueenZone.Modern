<#
.SYNOPSIS
  Run the issue #334 legacy public-read evaluation benchmark against SQL Server.

.DESCRIPTION
  Executes the same query shapes as docs/sql/007-legacy-public-read-benchmark.sql
  via System.Data.SqlClient, prints inventory + timings, and optionally writes CSV.

  Connection string (first match wins):
    1. -ConnectionString argument
    2. env ConnectionStrings__QueenZoneLegacy
    3. Azure App Service app setting (az CLI): queenzone-dev / Queenzone-RG

  Read-only. Does not print the connection string.

.EXAMPLE
  $env:ConnectionStrings__QueenZoneLegacy = '<from Bitwarden or App Service>'
  powershell -File .\scripts\Run-LegacyPublicReadBenchmark.ps1

.EXAMPLE
  powershell -File .\scripts\Run-LegacyPublicReadBenchmark.ps1 -FromAppService -OutCsv .\docs\performance\results\legacy-modern-eval-live-timings.csv
#>
[CmdletBinding()]
param(
    [string] $ConnectionString,
    [switch] $FromAppService,
    [string] $AppName = "queenzone-dev",
    [string] $ResourceGroup = "Queenzone-RG",
    [int] $Runs = 3,
    [string] $OutCsv
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-ConnectionString {
    if ($ConnectionString) { return $ConnectionString }
    if ($env:ConnectionStrings__QueenZoneLegacy) { return $env:ConnectionStrings__QueenZoneLegacy }
    if ($FromAppService) {
        $az = Get-Command az -ErrorAction SilentlyContinue
        if (-not $az) {
            throw "az CLI required for -FromAppService. Or set ConnectionStrings__QueenZoneLegacy."
        }
        $value = az webapp config appsettings list `
            -g $ResourceGroup -n $AppName `
            --query "[?name=='ConnectionStrings__QueenZoneLegacy'].value | [0]" -o tsv
        if (-not $value) {
            throw "App Service setting ConnectionStrings__QueenZoneLegacy was empty for $AppName."
        }
        return [string]$value
    }
    throw "No connection string. Pass -ConnectionString, set ConnectionStrings__QueenZoneLegacy, or use -FromAppService."
}

function Invoke-ScalarInt {
    param([System.Data.SqlClient.SqlConnection] $Conn, [string] $Sql)
    $cmd = $Conn.CreateCommand()
    $cmd.CommandTimeout = 180
    $cmd.CommandText = $Sql
    $result = $cmd.ExecuteScalar()
    if ($null -eq $result -or $result -is [DBNull]) { return 0 }
    return [int64]$result
}

function Invoke-TimedQuery {
    param(
        [string] $Cs,
        [string] $Name,
        [string] $Sql,
        [int] $RunCount
    )
    $times = New-Object System.Collections.Generic.List[int]
    $lastRows = 0
    for ($i = 1; $i -le $RunCount; $i++) {
        $conn = New-Object System.Data.SqlClient.SqlConnection $Cs
        $conn.Open()
        try {
            $cmd = $conn.CreateCommand()
            $cmd.CommandTimeout = 180
            $cmd.CommandText = $Sql
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            $reader = $cmd.ExecuteReader()
            $rows = 0
            while ($reader.Read()) { $rows++ }
            $reader.Close()
            $sw.Stop()
            $times.Add([int]$sw.ElapsedMilliseconds)
            $lastRows = $rows
        }
        finally {
            $conn.Close()
        }
    }
    $avg = [math]::Round(($times | Measure-Object -Average).Average, 1)
    $min = ($times | Measure-Object -Minimum).Minimum
    $max = ($times | Measure-Object -Maximum).Maximum
    [pscustomobject]@{
        Area   = $Name
        Runs   = $RunCount
        AvgMs  = $avg
        MinMs  = $min
        MaxMs  = $max
        Rows   = $lastRows
        Times  = ($times -join ",")
        Source = "client-wall"
    }
}

function Invoke-ServerMs {
    param([string] $Cs, [string] $Label, [string] $InnerSql)
    $sql = @"
DECLARE @t datetime2(7) = SYSUTCDATETIME();
$InnerSql
SELECT DATEDIFF(millisecond, @t, SYSUTCDATETIME()) AS ElapsedMs;
"@
    $conn = New-Object System.Data.SqlClient.SqlConnection $Cs
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandTimeout = 180
        $cmd.CommandText = $sql
        $ms = [int]$cmd.ExecuteScalar()
        [pscustomobject]@{
            Area   = $Label
            Runs   = 1
            AvgMs  = $ms
            MinMs  = $ms
            MaxMs  = $ms
            Rows   = 0
            Times  = "$ms"
            Source = "server-datediff"
        }
    }
    finally {
        $conn.Close()
    }
}

Add-Type -AssemblyName System.Data
$cs = Get-ConnectionString
Write-Host "Connected (connection string length $($cs.Length); value not printed)."

# Inventory
$conn = New-Object System.Data.SqlClient.SqlConnection $cs
$conn.Open()
try {
    $inventory = [ordered]@{
        "NEWS_T rows"              = Invoke-ScalarInt $conn "SELECT COUNT_BIG(*) FROM dbo.NEWS_T"
        "NEWS_T DISPLAY=1"         = Invoke-ScalarInt $conn "SELECT COUNT_BIG(*) FROM dbo.NEWS_T WHERE DISPLAY = 1"
        "NEWS_T distinct NEWS_ID"  = Invoke-ScalarInt $conn "SELECT COUNT_BIG(DISTINCT NEWS_ID) FROM dbo.NEWS_T"
        "NEWS_T dup NEWS_ID groups"= Invoke-ScalarInt $conn @"
SELECT COUNT_BIG(*) FROM (
  SELECT NEWS_ID FROM dbo.NEWS_T GROUP BY NEWS_ID HAVING COUNT(*) > 1
) d
"@
        "IX_NEWS_T_Display_Date"   = Invoke-ScalarInt $conn @"
SELECT CASE WHEN EXISTS (
  SELECT 1 FROM sys.indexes
  WHERE object_id = OBJECT_ID(N'dbo.NEWS_T') AND name = N'IX_NEWS_T_Display_Date'
) THEN 1 ELSE 0 END
"@
        "Q_ARTICLE_T DISPLAY=1"    = Invoke-ScalarInt $conn "SELECT COUNT_BIG(*) FROM dbo.Q_ARTICLE_T WHERE DISPLAY = 1"
        "PIC_FILES_T DISPLAY=1"    = Invoke-ScalarInt $conn "SELECT COUNT_BIG(*) FROM dbo.PIC_FILES_T WHERE DISPLAY = 1"
        "Q_STAGE_T DISPLAY=1"      = Invoke-ScalarInt $conn "SELECT COUNT_BIG(*) FROM dbo.Q_STAGE_T WHERE DISPLAY = 1"
        "ModernForum tables"       = Invoke-ScalarInt $conn "SELECT COUNT_BIG(*) FROM sys.tables WHERE name LIKE N'ModernForum%'"
    }
}
finally {
    $conn.Close()
}

Write-Host ""
Write-Host "Inventory"
Write-Host "---------"
foreach ($key in $inventory.Keys) {
    Write-Host ("{0,-28} {1}" -f $key, $inventory[$key])
}

$queries = [ordered]@{
    "news-published-count-cte" = @"
WITH PublishedNews AS (
  SELECT NEWS_ID AS Id,
    ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
  FROM NEWS_T WHERE DISPLAY = 1
)
SELECT COUNT(*) FROM PublishedNews WHERE RowNumber = 1
"@
    "news-published-count-simple" = "SELECT COUNT(*) FROM NEWS_T WHERE DISPLAY = 1"
    "news-archive-page1-cte" = @"
WITH PublishedNews AS (
  SELECT NEWS_ID AS Id, TITLE AS Title, ISNULL(EXCERPT,'') AS Excerpt,
    CAST(N'' AS nvarchar(max)) AS Body, [DATE] AS PublishedAt, SOURCE_URL AS SourceUrl,
    CAST(1 AS bit) AS IsPublished, SLUG AS Slug,
    ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
  FROM NEWS_T WHERE DISPLAY = 1
)
SELECT Id, Title, Excerpt, Body, PublishedAt, SourceUrl, IsPublished, Slug
FROM PublishedNews WHERE RowNumber = 1
ORDER BY PublishedAt DESC, Id DESC
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY
"@
    "news-archive-page100-cte" = @"
WITH PublishedNews AS (
  SELECT NEWS_ID AS Id, TITLE AS Title, ISNULL(EXCERPT,'') AS Excerpt,
    CAST(N'' AS nvarchar(max)) AS Body, [DATE] AS PublishedAt, SOURCE_URL AS SourceUrl,
    CAST(1 AS bit) AS IsPublished, SLUG AS Slug,
    ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
  FROM NEWS_T WHERE DISPLAY = 1
)
SELECT Id, Title, Excerpt, Body, PublishedAt, SourceUrl, IsPublished, Slug
FROM PublishedNews WHERE RowNumber = 1
ORDER BY PublishedAt DESC, Id DESC
OFFSET 1980 ROWS FETCH NEXT 20 ROWS ONLY
"@
    "news-sitemap-all-cte" = @"
WITH PublishedNews AS (
  SELECT NEWS_ID AS Id, TITLE AS Title, [DATE] AS PublishedAt, SLUG AS Slug,
    ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
  FROM NEWS_T WHERE DISPLAY = 1
)
SELECT Id, Title, PublishedAt, Slug
FROM PublishedNews WHERE RowNumber = 1
ORDER BY PublishedAt DESC, Id DESC
"@
    "news-admin-count-cte" = @"
WITH LatestNews AS (
  SELECT NEWS_ID,
    ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
  FROM NEWS_T
)
SELECT COUNT(*) FROM LatestNews WHERE RowNumber = 1
"@
    "articles-archive-page1-preview" = @"
SELECT CAST(a.Q_ARTICLE_ID AS int) AS Id, a.ARTICLE_NAME AS Title,
  LEFT(ISNULL(CAST(a.ARTICLE_TEXT AS nvarchar(max)), N''), 2000) AS Body,
  a.DATE_CREATED AS PublishedAt
FROM Q_ARTICLE_T a WHERE a.DISPLAY = 1
ORDER BY a.DATE_CREATED DESC, a.Q_ARTICLE_ID DESC
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY
"@
    "photos-categories-with-counts" = @"
SELECT c.cat_id, c.name, COUNT(p.PIC_ID) AS ImageCount
FROM dbo.PIC_CAT_T c
INNER JOIN dbo.PIC_FILES_T p ON p.Cat_ID = c.cat_id AND p.DISPLAY = 1
GROUP BY c.cat_id, c.name
HAVING COUNT(p.PIC_ID) > 0
ORDER BY c.name
"@
    "photos-largest-cat-page1" = @"
DECLARE @cat int = (
  SELECT TOP 1 Cat_ID FROM dbo.PIC_FILES_T WHERE DISPLAY = 1
  GROUP BY Cat_ID ORDER BY COUNT(*) DESC
);
SELECT p.PIC_ID, p.Name, p.Date_time, p.Thumb_URL
FROM dbo.PIC_FILES_T p
WHERE p.Cat_ID = @cat AND p.DISPLAY = 1
ORDER BY p.Date_time DESC, p.PIC_ID DESC
OFFSET 0 ROWS FETCH NEXT 24 ROWS ONLY
"@
    "fan-perf-page20" = @"
SELECT TOP (20) Q_STAGE_ID, TITLE, DATE_ADDED
FROM dbo.Q_STAGE_T WHERE DISPLAY = 1 ORDER BY DATE_ADDED DESC
"@
    "bio-list-sp" = "EXEC Q_BIO_LIST_SP"
    "album-list-sp" = "EXEC Q_ALBUM_LIST_SP"
}

$results = New-Object System.Collections.Generic.List[object]
Write-Host ""
Write-Host "Client wall-clock ($Runs runs each; includes network RTT)"
Write-Host "-------------------------------------------------------"
foreach ($name in $queries.Keys) {
    try {
        $row = Invoke-TimedQuery -Cs $cs -Name $name -Sql $queries[$name] -RunCount $Runs
        $results.Add($row)
        Write-Host ("{0,-36} avg={1,6} ms  min={2,4} max={3,4} rows={4}" -f $name, $row.AvgMs, $row.MinMs, $row.MaxMs, $row.Rows)
    }
    catch {
        Write-Warning "$name failed: $($_.Exception.Message)"
        $results.Add([pscustomobject]@{
            Area = $name; Runs = 0; AvgMs = -1; MinMs = -1; MaxMs = -1; Rows = -1
            Times = "err"; Source = "client-wall"
        })
    }
}

Write-Host ""
Write-Host "Server-side engine ms (DATEDIFF inside one batch; no network per row)"
Write-Host "--------------------------------------------------------------------"
$serverJobs = [ordered]@{
    "server-news-count-cte" = @"
;WITH PublishedNews AS (
  SELECT NEWS_ID, ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS rn
  FROM NEWS_T WHERE DISPLAY = 1
)
SELECT COUNT(*) AS c INTO #s1 FROM PublishedNews WHERE rn = 1;
DROP TABLE #s1;
"@
    "server-news-count-simple" = @"
SELECT COUNT(*) AS c INTO #s2 FROM NEWS_T WHERE DISPLAY = 1;
DROP TABLE #s2;
"@
    "server-news-page1-cte" = @"
;WITH PublishedNews AS (
  SELECT NEWS_ID AS Id, TITLE, ISNULL(EXCERPT,'') AS Excerpt, [DATE] AS PublishedAt, SLUG,
    ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS rn
  FROM NEWS_T WHERE DISPLAY = 1
)
SELECT * INTO #s3 FROM PublishedNews WHERE rn = 1
ORDER BY PublishedAt DESC, Id DESC OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;
DROP TABLE #s3;
"@
    "server-news-sitemap-cte" = @"
;WITH PublishedNews AS (
  SELECT NEWS_ID AS Id, TITLE, [DATE] AS PublishedAt, SLUG,
    ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS rn
  FROM NEWS_T WHERE DISPLAY = 1
)
SELECT * INTO #s4 FROM PublishedNews WHERE rn = 1 ORDER BY PublishedAt DESC, Id DESC;
DROP TABLE #s4;
"@
}
foreach ($name in $serverJobs.Keys) {
    $row = Invoke-ServerMs -Cs $cs -Label $name -InnerSql $serverJobs[$name]
    $results.Add($row)
    Write-Host ("{0,-36} {1,6} ms" -f $name, $row.AvgMs)
}

if ($OutCsv) {
    $dir = Split-Path -Parent $OutCsv
    if ($dir -and -not (Test-Path $dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
    $results | Export-Csv -NoTypeInformation -Path $OutCsv
    Write-Host ""
    Write-Host "Wrote $OutCsv"
}

Write-Host ""
Write-Host "Done. Interpret with docs/performance/legacy-to-modern-eval-2026-07-27.md"
