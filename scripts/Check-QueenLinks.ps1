param(
    [string] $ConnectionString = "",

    [string] $SettingsFile = "",

    [int] $Concurrency = 8,

    [int] $ConfirmAfter = 2,

    [int] $TimeoutSeconds = 10,

    [int] $Limit = 0,

    [switch] $DryRun
)

$ErrorActionPreference = "Stop"

$arguments = @(
    "run",
    "--project", ".\src\QueenZone.Tools\QueenZone.Tools.csproj",
    "--",
    "check-links",
    "--concurrency", $Concurrency,
    "--confirm-after", $ConfirmAfter,
    "--timeout-seconds", $TimeoutSeconds
)

if ($DryRun) {
    $arguments += "--dry-run"
}

if (-not [string]::IsNullOrWhiteSpace($ConnectionString)) {
    $arguments += @("--connection-string", $ConnectionString)
}

if (-not [string]::IsNullOrWhiteSpace($SettingsFile)) {
    $arguments += @("--settings-file", $SettingsFile)
}

if ($Limit -gt 0) {
    $arguments += @("--limit", $Limit)
}

dotnet @arguments
