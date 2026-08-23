[CmdletBinding()]
param(
    [string]$SiteName = "queenzone-dev",
    [string]$ResourceGroup = "Queenzone-RG",
    [string]$PublicBaseUrl = "https://www.queenzone.org",
    [switch]$WhatIf
)

# Ensures App Service has MobileAuth__SigningKey so /api/v1/auth can issue JWTs.
# Production mobile login fails closed with "Mobile auth is not configured." when
# the setting is blank (see MobileAuthService / MobileAuthTokenIssuer).
#
# - Never prints the key value (name presence + public authorize probe only).
# - Idempotent: if the setting name already exists, exits 0 after a public probe.
# - Does not write Bitwarden; mirror the value there separately after a create.

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$settingName = "MobileAuth__SigningKey"
$az = Get-Command az -ErrorAction Stop

function Test-SettingNamePresent {
    $liveNamesJson = & $az.Source webapp config appsettings list `
        --name $SiteName `
        --resource-group $ResourceGroup `
        --query "[].name" `
        -o json
    if ($LASTEXITCODE -ne 0) {
        throw "az webapp config appsettings list failed for $SiteName/$ResourceGroup."
    }

    $liveNames = @(($liveNamesJson | ConvertFrom-Json))
    return ($settingName -in $liveNames)
}

function Assert-MobileAuthConfigured {
    param([string]$BaseUrl)

    $challenge = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
    $authorizeUrl = (
        "$BaseUrl/api/v1/auth/authorize?response_type=code" +
        "&client_id=queenzone-mobile" +
        "&redirect_uri=queenzone%3A%2F%2Fauth%2Fcallback" +
        "&code_challenge=$challenge" +
        "&code_challenge_method=S256" +
        "&state=ensure-mobile-auth" +
        "&provider=google"
    )

    # Prefer curl: consistent Location capture without following the custom-scheme redirect.
    $curl = Get-Command curl -ErrorAction SilentlyContinue
    if ($curl) {
        $headerDump = & $curl.Source -sS -D - -o /dev/null $authorizeUrl 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) {
            throw "Authorize probe curl failed: $headerDump"
        }

        $locationLine = ($headerDump -split "`n" | Where-Object { $_ -match '^(?i)location:' } | Select-Object -First 1)
        if (-not $locationLine) {
            throw "Authorize probe returned no Location header."
        }

        $location = ($locationLine -replace '^(?i)location:\s*', '').Trim()
    }
    else {
        try {
            $null = Invoke-WebRequest -Uri $authorizeUrl -MaximumRedirection 0
            throw "Authorize probe expected a redirect."
        }
        catch {
            $response = $_.Exception.Response
            if (-not $response) {
                throw
            }

            $location = [string]$response.Headers.Location
        }
    }

    if ([string]::IsNullOrWhiteSpace($location)) {
        throw "Authorize probe returned an empty Location header."
    }

    if ($location -match "Mobile%20auth%20is%20not%20configured" -or
        $location -match "Mobile auth is not configured") {
        throw "Authorize probe still reports mobile auth is not configured."
    }

    Write-Output "Authorize probe ok (Location no longer reports missing MobileAuth signing key)."
}

$present = Test-SettingNamePresent
if ($present) {
    Write-Output "$settingName name already present on ${SiteName}."
    Assert-MobileAuthConfigured -BaseUrl $PublicBaseUrl.TrimEnd('/')
    exit 0
}

# 48 bytes → 64 url-safe chars; meets MobileAuthOptionsValidator minimum (32).
$keyBytes = [byte[]]::new(48)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($keyBytes)
$key = [Convert]::ToBase64String($keyBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$keyLength = $key.Length

if ($WhatIf) {
    Write-Output "WhatIf: would set $settingName on ${SiteName} (len=$keyLength) and restart the site."
    exit 0
}

Write-Output "Setting $settingName on ${SiteName} (len=$keyLength)…"
& $az.Source webapp config appsettings set `
    --name $SiteName `
    --resource-group $ResourceGroup `
    --settings "${settingName}=$key" `
    --output none
if ($LASTEXITCODE -ne 0) {
    throw "az webapp config appsettings set failed for $settingName."
}

# Drop the plaintext key from this process as soon as App Service has it.
$key = $null
$keyBytes = $null

Write-Output "Restarting ${SiteName}…"
& $az.Source webapp restart --name $SiteName --resource-group $ResourceGroup --output none
if ($LASTEXITCODE -ne 0) {
    throw "az webapp restart failed for $SiteName."
}

# App Service setting propagation + process start can lag a few seconds.
$deadline = [DateTime]::UtcNow.AddMinutes(3)
$lastError = $null
while ([DateTime]::UtcNow -lt $deadline) {
    try {
        Assert-MobileAuthConfigured -BaseUrl $PublicBaseUrl.TrimEnd('/')
        $lastError = $null
        break
    }
    catch {
        $lastError = $_
        Start-Sleep -Seconds 5
    }
}

if ($null -ne $lastError) {
    throw $lastError
}

if (-not (Test-SettingNamePresent)) {
    throw "Post-write verify failed: $settingName name missing on ${SiteName}."
}

Write-Output "$settingName is configured on ${SiteName} (len=$keyLength)."
Write-Output "Mirror the same value into Bitwarden secret '$settingName' (Queenzone Development). Do not paste the value into chat or git."
