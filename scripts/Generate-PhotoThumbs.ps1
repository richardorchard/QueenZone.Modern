param(
    [string] $PicIds = "",
    [string] $PicIdsFile = "",
    [string] $ConnectionString = "",
    [string] $StorageConnectionString = "",
    [string] $SettingsFile = "",
    [int] $ThumbSize = 400,
    [switch] $DryRun
)

$ErrorActionPreference = "Stop"

$arguments = @(
    "run",
    "--project", ".\src\QueenZone.Tools\QueenZone.Tools.csproj",
    "--",
    "generate-photo-thumbs",
    "--thumb-size", $ThumbSize
)

if ($DryRun) {
    $arguments += "--dry-run"
}

if (-not [string]::IsNullOrWhiteSpace($PicIds)) {
    $arguments += @("--pic-ids", $PicIds)
}

if (-not [string]::IsNullOrWhiteSpace($PicIdsFile)) {
    $arguments += @("--pic-ids-file", $PicIdsFile)
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

dotnet @arguments
