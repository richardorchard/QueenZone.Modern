[CmdletBinding()]
param(
    [string]$ConnectionString = $env:SQL_CONNECTION_STRING,
    [string]$EnvironmentFile = $env:GITHUB_ENV,
    [switch]$SelfTest
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($SelfTest) {
    $temporaryFile = New-TemporaryFile
    try {
        & $PSCommandPath `
            -ConnectionString "Server=test;Database=test;User ID=test;Password=test-only-password" `
            -EnvironmentFile $temporaryFile.FullName | Out-Null
        $actual = Get-Content -LiteralPath $temporaryFile.FullName -Raw
        if ($actual.Trim() -ne "TF_VAR_target_sql_admin_password=test-only-password") {
            throw "The ephemeral SQL password export did not write the expected environment entry."
        }
    }
    finally {
        Remove-Item -LiteralPath $temporaryFile.FullName -Force
    }

    Write-Output "Export-EphemeralSqlAdminPassword self-test passed."
    exit 0
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw "QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING is not mapped for this OpenTofu environment."
}
if ([string]::IsNullOrWhiteSpace($EnvironmentFile)) {
    throw "GITHUB_ENV is not available."
}

$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($ConnectionString)
$password = $builder.Password
if ([string]::IsNullOrWhiteSpace($password)) {
    throw "The migration connection string does not contain a SQL administrator password."
}
if ($password.Contains("`r") -or $password.Contains("`n")) {
    throw "The SQL administrator password cannot contain a line break."
}

Write-Output "::add-mask::$password"
Add-Content -LiteralPath $EnvironmentFile -Value "TF_VAR_target_sql_admin_password=$password"
