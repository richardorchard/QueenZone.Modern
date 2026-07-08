param(
    [string] $ConnectionString = "",

    [string] $StorageConnectionString = "",

    [string] $SettingsFile = "",

    [ValidateSet("http", "blob")]
    [string] $Method = "http",

    [int] $CategoryId = 0,

    [string] $CategorySlug = "",

    [int] $Limit = 0,

    [int] $Concurrency = 8,

    [string] $Output = "",

    [string] $HideIdsOutput = "",

    [switch] $DryRun
)

$ErrorActionPreference = "Stop"

$arguments = @(
    "run",
    "--project", ".\src\QueenZone.Tools\QueenZone.Tools.csproj",
    "--",
    "check-photos",
    "--method", $Method,
    "--concurrency", $Concurrency
)

if ($DryRun) {
    $arguments += "--dry-run"
}

if (-not [string]::IsNullOrWhiteSpace($ConnectionString)) {
    $arguments += @("--connection-string", $ConnectionString)
}

if (-not [string]::IsNullOrWhiteSpace($StorageConnectionString)) {
    $arguments += @("--storage-connection-string", $StorageConnectionString)
}

if (-not [string]::IsNullOrWhiteSpace($SettingsFile)) {
    $arguments += @("--settings-file", $SettingsFile)
}

if ($CategoryId -gt 0) {
    $arguments += @("--category-id", $CategoryId)
}

if (-not [string]::IsNullOrWhiteSpace($CategorySlug)) {
    $arguments += @("--category-slug", $CategorySlug)
}

if ($Limit -gt 0) {
    $arguments += @("--limit", $Limit)
}

if (-not [string]::IsNullOrWhiteSpace($Output)) {
    $arguments += @("--output", $Output)
}

if (-not [string]::IsNullOrWhiteSpace($HideIdsOutput)) {
    $arguments += @("--hide-ids-output", $HideIdsOutput)
}

dotnet @arguments
