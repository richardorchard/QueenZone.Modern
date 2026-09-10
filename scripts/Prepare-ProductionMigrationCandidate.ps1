[CmdletBinding()]
param(
    [string]$ResourceGroup = "Queenzone-RG",
    [string]$SourceApp = "queenzone-dev",
    [string]$CandidateApp = "queenzone-prod",
    [string]$SourceSqlServer = "queenzone-sql-server",
    [string]$CandidateSqlServer = "queenzone-prod-sql",
    [string]$CandidateStorageAccount = "queenzoneprod",
    [string]$CandidateApplicationInsights = "queenzone-prod-ai",
    [string]$BitwardenProjectId = "1c16fd2d-4bfb-4eb7-8357-b49400233490",
    [switch]$PlanOnly
)

$ErrorActionPreference = "Stop"

function Invoke-JsonCommand {
    param(
        [Parameter(Mandatory)]
        [string]$Executable,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $output = & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Executable failed with exit code $LASTEXITCODE."
    }

    return ($output -join [Environment]::NewLine) | ConvertFrom-Json
}

function Invoke-BitwardenSecretUpsert {
    param(
        [Parameter(Mandatory)]
        [string]$BwsPath,

        [Parameter(Mandatory)]
        [object[]]$ExistingSecrets,

        [Parameter(Mandatory)]
        [string]$Key,

        [Parameter(Mandatory)]
        [string]$Value,

        [Parameter(Mandatory)]
        [string]$ProjectId
    )

    $existing = @($ExistingSecrets | Where-Object key -eq $Key)
    if ($existing.Count -gt 1) {
        throw "Bitwarden contains more than one secret named '$Key'."
    }

    $note = "Issue #1272 Canada East migration candidate. Do not use for the Australia East production app."
    if ($existing.Count -eq 1) {
        & $BwsPath secret edit --value $Value --note $note $existing[0].id --output none
    }
    else {
        & $BwsPath secret create $Key $Value $ProjectId --note $note --output none
    }

    if ($LASTEXITCODE -ne 0) {
        throw "Bitwarden update failed for '$Key'."
    }
}

function Get-SettingValue {
    param(
        [Parameter(Mandatory)]
        [object[]]$Settings,

        [Parameter(Mandatory)]
        [string]$Name
    )

    $matches = @($Settings | Where-Object name -eq $Name)
    if ($matches.Count -ne 1 -or [string]::IsNullOrWhiteSpace($matches[0].value)) {
        $lengths = @($matches | ForEach-Object { ([string]$_.value).Length }) -join ","
        throw "Source App Service setting '$Name' is missing or ambiguous (matches=$($matches.Count), value lengths=$lengths)."
    }

    return [string]$matches[0].value
}

$source = Invoke-JsonCommand -Executable az -Arguments @("webapp", "show", "--resource-group", $ResourceGroup, "--name", $SourceApp, "--output", "json")
$candidate = Invoke-JsonCommand -Executable az -Arguments @("webapp", "show", "--resource-group", $ResourceGroup, "--name", $CandidateApp, "--output", "json")

if ($source.location -ne "Australia East") {
    throw "Source App Service '$SourceApp' is not in Australia East."
}
if ($candidate.location -ne "Canada East") {
    throw "Candidate App Service '$CandidateApp' is not in Canada East."
}
if ($candidate.defaultHostName -ne "$CandidateApp.azurewebsites.net") {
    throw "Candidate default hostname does not match the expected direct-test hostname."
}

$sourceSettings = Invoke-JsonCommand -Executable az -Arguments @("webapp", "config", "appsettings", "list", "--resource-group", $ResourceGroup, "--name", $SourceApp, "--output", "json")
if ($sourceSettings.Count -eq 0) {
    throw "Source App Service has no application settings to copy."
}
Write-Verbose "Loaded $($sourceSettings.Count) source App Service settings: $((@($sourceSettings | ForEach-Object name) | Sort-Object) -join ', ')"

$sourceSqlConnection = Get-SettingValue -Settings $sourceSettings -Name "ConnectionStrings__QueenZoneLegacy"
$sourceSqlHost = "$SourceSqlServer.database.windows.net"
$candidateSqlHost = "$CandidateSqlServer.database.windows.net"
if ($sourceSqlConnection.IndexOf($sourceSqlHost, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw "Source SQL connection does not contain the expected source hostname."
}
$candidateSqlConnection = [regex]::Replace(
    $sourceSqlConnection,
    [regex]::Escape($sourceSqlHost),
    $candidateSqlHost,
    [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($candidateSqlConnection.IndexOf($sourceSqlHost, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw "Candidate SQL connection still contains the source hostname."
}

$candidateBlobConnection = & az storage account show-connection-string --resource-group $ResourceGroup --name $CandidateStorageAccount --query connectionString --output tsv
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($candidateBlobConnection)) {
    throw "Could not obtain the candidate Storage connection string."
}

$candidateInsightsConnection = & az monitor app-insights component show --resource-group $ResourceGroup --app $CandidateApplicationInsights --query connectionString --output tsv
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($candidateInsightsConnection)) {
    throw "Could not obtain the candidate Application Insights connection string."
}

$desired = [ordered]@{}
foreach ($setting in $sourceSettings) {
    $desired[[string]$setting.name] = [string]$setting.value
}
$desired["ConnectionStrings__QueenZoneLegacy"] = $candidateSqlConnection
$desired["ConnectionStrings__BlobStorage"] = $candidateBlobConnection
$desired["APPLICATIONINSIGHTS_CONNECTION_STRING"] = $candidateInsightsConnection
$desired["WEBSITE_WARMUP_PATH"] = "/health"
$desired.Remove("WEBSITE_RUN_FROM_PACKAGE")
$desired.Remove("WEBSITE_WARMUP_STATUSES")

if ($desired.Contains("BlobUpload__PublicBaseUrl")) {
    $desired["BlobUpload__PublicBaseUrl"] = [regex]::Replace(
        $desired["BlobUpload__PublicBaseUrl"],
        [regex]::Escape("https://queenzone.blob.core.windows.net"),
        "https://$CandidateStorageAccount.blob.core.windows.net",
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
}

$bwsPath = Join-Path $env:USERPROFILE "bin\bws.exe"
if (-not (Test-Path -LiteralPath $bwsPath)) {
    throw "Bitwarden Secrets Manager CLI was not found at the expected Windows path."
}
if ([string]::IsNullOrWhiteSpace($env:BWS_ACCESS_TOKEN)) {
    throw "BWS_ACCESS_TOKEN is not configured for this process."
}

$publishProfile = (& az webapp deployment list-publishing-profiles --resource-group $ResourceGroup --name $CandidateApp --xml) -join [Environment]::NewLine
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($publishProfile)) {
    throw "Could not obtain the candidate publish profile."
}
if ($publishProfile.IndexOf("$CandidateApp.scm.azurewebsites.net", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw "The retrieved publish profile does not belong to '$CandidateApp'."
}

# Windows PowerShell's legacy native-command binder can split an XML argument
# at spaces when it contains double-quoted attributes. Re-emit only the
# MSDeploy profile with single-quoted, XML-escaped attributes before passing it
# to bws. The resulting value is equivalent for webapps-deploy and Kudu.
[xml]$publishProfileDocument = $publishProfile
$msDeployProfile = @($publishProfileDocument.publishData.publishProfile | Where-Object publishMethod -eq "MSDeploy")
if ($msDeployProfile.Count -ne 1) {
    throw "Expected exactly one MSDeploy entry in the candidate publish profile."
}
$profileAttributes = @($msDeployProfile[0].Attributes | ForEach-Object {
        "$($_.Name)='$([System.Security.SecurityElement]::Escape($_.Value))'"
    })
$publishProfile = "<publishData><publishProfile $($profileAttributes -join ' ') /></publishData>"

$existingSecrets = Invoke-JsonCommand -Executable $bwsPath -Arguments @("secret", "list", $BitwardenProjectId, "--output", "json")
$secretValues = [ordered]@{
    "ConnectionStrings__QueenZoneLegacyCanadaEast"       = $candidateSqlConnection
    "ConnectionStrings__BlobStorageCanadaEast"          = $candidateBlobConnection
    "APPLICATIONINSIGHTS_CONNECTION_STRING_CANADA_EAST" = $candidateInsightsConnection
    "AZURE_WEBAPP_PUBLISH_PROFILE_CANADA_EAST"           = $publishProfile
}

if (-not $PlanOnly) {
    foreach ($entry in $secretValues.GetEnumerator()) {
        Invoke-BitwardenSecretUpsert `
            -BwsPath $bwsPath `
            -ExistingSecrets $existingSecrets `
            -Key $entry.Key `
            -Value $entry.Value `
            -ProjectId $BitwardenProjectId
    }
}

$sourceRoleAssignments = Invoke-JsonCommand -Executable az -Arguments @(
        "role", "assignment", "list",
        "--scope", [string]$source.id,
        "--query", "[?roleDefinitionName=='Website Contributor']",
        "--output", "json")
if ($sourceRoleAssignments.Count -ne 1) {
    throw "Expected exactly one direct Website Contributor assignment on '$SourceApp'."
}
$deployPrincipalId = [string]$sourceRoleAssignments[0].principalId

if (-not $PlanOnly) {
    & az role assignment create `
        --assignee-object-id $deployPrincipalId `
        --assignee-principal-type ServicePrincipal `
        --role "Website Contributor" `
        --scope $candidate.id `
        --output none
    if ($LASTEXITCODE -ne 0) {
        throw "Could not grant Website Contributor on the candidate App Service."
    }
}

if (-not $PlanOnly) {
    $token = & az account get-access-token --resource https://management.azure.com/ --query accessToken --output tsv
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($token)) {
        throw "Could not obtain an Azure Resource Manager token."
    }

    $uri = "https://management.azure.com$($candidate.id)/config/appsettings?api-version=2024-04-01"
    $body = @{ properties = $desired } | ConvertTo-Json -Depth 4 -Compress
    Invoke-RestMethod -Method Put -Uri $uri -Headers @{ Authorization = "Bearer $token" } -ContentType "application/json" -Body $body | Out-Null

    $verified = Invoke-JsonCommand -Executable az -Arguments @("webapp", "config", "appsettings", "list", "--resource-group", $ResourceGroup, "--name", $CandidateApp, "--output", "json")
    $verifiedMap = @{}
    foreach ($setting in $verified) {
        $verifiedMap[[string]$setting.name] = [string]$setting.value
    }
    foreach ($entry in $desired.GetEnumerator()) {
        if (-not $verifiedMap.ContainsKey($entry.Key) -or $verifiedMap[$entry.Key] -cne $entry.Value) {
            throw "Candidate setting verification failed for '$($entry.Key)'."
        }
    }
    if ($verifiedMap.ContainsKey("WEBSITE_RUN_FROM_PACKAGE") -or $verifiedMap.ContainsKey("WEBSITE_WARMUP_STATUSES")) {
        throw "A forbidden legacy deployment setting remains on the candidate."
    }
}

Write-Output "Candidate settings prepared: $($desired.Count) names; SQL, Storage, and telemetry endpoints overridden."
if ($PlanOnly) {
    Write-Output "Candidate deploy identity validated: one source Website Contributor principal found."
    Write-Output "Candidate Bitwarden secret plan: $($secretValues.Keys -join ', ')."
    Write-Output "PlanOnly: no Azure, role-assignment, or Bitwarden writes were performed."
}
else {
    Write-Output "Candidate deploy identity prepared: Website Contributor assignment present."
    Write-Output "Candidate Bitwarden secrets prepared: $($secretValues.Keys -join ', ')."
}
Write-Output "No DNS, hostname binding, TLS, source App Service, source database, or source Storage changes were requested."
