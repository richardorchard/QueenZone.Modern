param(
    [Parameter(Mandatory = $true)]
    [string] $CsvPath,

    [string] $ConnectionString = $env:ConnectionStrings__QueenZoneLegacy,

    [switch] $DryRun
)

$ErrorActionPreference = "Stop"

if (-not $DryRun -and [string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw "ConnectionString was not provided. Pass -ConnectionString or set ConnectionStrings__QueenZoneLegacy."
}

$arguments = @("run", "--project", ".\src\QueenZone.Tools\QueenZone.Tools.csproj", "--", "import-history", "--csv", $CsvPath)
if ($DryRun) {
    $arguments += "--dry-run"
} else {
    $arguments += @("--connection-string", $ConnectionString)
}

dotnet @arguments
