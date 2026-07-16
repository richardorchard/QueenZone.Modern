#Requires -Version 5.1
<#
.SYNOPSIS
  Post-deploy HTTP smoke against the live custom domain (or another base URL).

.DESCRIPTION
  Waits for /health to return 200 with status=ok, then checks key public routes.
  Matches the post-deploy smoke in .github/workflows/deploy-app-service.yml so you
  can re-run the same checks locally after a deploy or when debugging a failure.

.EXAMPLE
  .\scripts\Smoke-LiveSite.ps1

.EXAMPLE
  .\scripts\Smoke-LiveSite.ps1 -BaseUrl https://www.queenzone.org -TimeoutMinutes 6
#>
[CmdletBinding()]
param(
    [string]$BaseUrl = "https://www.queenzone.org",
    [int]$TimeoutMinutes = 6,
    [int]$PollSeconds = 10
)

$ErrorActionPreference = "Stop"

if ($BaseUrl -notmatch '^https?://') {
    throw "BaseUrl must be an absolute http(s) URL. Received: '$BaseUrl'"
}

$BaseUrl = $BaseUrl.TrimEnd("/")
$healthUrl = "$BaseUrl/health"
$maxAttempts = [Math]::Max(1, [int][Math]::Ceiling(($TimeoutMinutes * 60) / $PollSeconds))

Write-Host "Waiting for healthy $healthUrl (up to ~$TimeoutMinutes min)..."

$healthy = $false
for ($i = 1; $i -le $maxAttempts; $i++) {
    $elapsed = $i * $PollSeconds
    try {
        $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq 200 -and $response.Content -match '"status"\s*:\s*"ok"') {
            Write-Host "Healthy after ~${elapsed}s (HTTP $($response.StatusCode), status=ok)"
            $healthy = $true
            break
        }
        Write-Host "  [${elapsed}s] HTTP $($response.StatusCode) but health body incomplete - waiting ${PollSeconds}s..."
    }
    catch {
        Write-Host "  [${elapsed}s] unreachable - waiting ${PollSeconds}s..."
    }
    Start-Sleep -Seconds $PollSeconds
}

if (-not $healthy) {
    throw "Custom domain did not return healthy /health within $TimeoutMinutes minutes: $healthUrl"
}

$paths = @(
    "/health",
    "/",
    "/news",
    "/forum",
    "/articles",
    "/biography",
    "/photography",
    "/search"
)

$failed = 0
foreach ($path in $paths) {
    $url = "$BaseUrl$path"
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 30
        if ($response.StatusCode -ne 200) {
            Write-Host "FAIL  $url -> HTTP $($response.StatusCode)"
            $failed++
            continue
        }
        if ($path -eq "/health" -and $response.Content -notmatch '"status"\s*:\s*"ok"') {
            Write-Host "FAIL  $url -> 200 but body missing status=ok"
            $failed++
            continue
        }
        Write-Host "OK    $url -> HTTP $($response.StatusCode)"
    }
    catch {
        Write-Host "FAIL  $url -> $($_.Exception.Message)"
        $failed++
    }
}

if ($failed -gt 0) {
    throw "Post-deploy smoke failed: $failed route(s) against $BaseUrl"
}

Write-Host "All post-deploy smoke checks passed against $BaseUrl."
