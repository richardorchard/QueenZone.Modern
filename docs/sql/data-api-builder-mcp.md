# SQL MCP With Azure Data API Builder

This guide explains how agents can expose a narrow, read-oriented SQL MCP server for local legacy database investigation.

Use this for schema review, performance diagnostics, and public-content migration checks. Do not use it to expose private tables wholesale or to make unreviewed database changes.

## What This Uses

Microsoft Data API Builder (DAB) can run as an MCP server over stdio:

```powershell
dab start --mcp-stdio --config .tools\dab-queenzone-mcp.json
```

The DAB config is local and ignored because it depends on local tool installation and secret environment variables. Keep real connection strings in ignored local settings only.

Reference: https://learn.microsoft.com/en-us/azure/data-api-builder/mcp/overview

## Install DAB Locally

Install the tool into the ignored `.tools` folder from the repository root:

```powershell
cd C:\workspace\QueenZone.Modern
dotnet tool install Microsoft.DataApiBuilder --tool-path .tools\data-api-builder
```

Verify:

```powershell
.\.tools\data-api-builder\dab.exe --version
```

PowerShell note: use `.\.tools\...` from the repo root. Without the leading `.\`, PowerShell can treat `.tools` as a module name and fail with `The module '.tools' could not be loaded`.

## Configure The Connection String

The DAB config should read the connection string from:

```text
ConnectionStrings__QueenZoneLegacy
```

For a local shell session, load it from the ignored app settings file:

```powershell
$json = Get-Content -Raw .\src\QueenZone.Web\appsettings.Local.json | ConvertFrom-Json
$env:ConnectionStrings__QueenZoneLegacy = [string]$json.ConnectionStrings.QueenZoneLegacy
```

Do not paste or commit the real connection string into documentation, scripts, or committed config.

## Getting The Live App Service Connection

The live preview App Service stores the runtime SQL connection string as an App Setting named:

```text
ConnectionStrings__QueenZoneLegacy
```

It may not appear in the App Service "connection strings" collection. Check App Settings instead:

```powershell
az login --tenant c9f094fd-23bf-4a35-a406-bcaacd7e1a8e

$settings = az webapp config appsettings list `
  --name queenzone-dev `
  --resource-group Queenzone-RG `
  -o json | ConvertFrom-Json

$env:ConnectionStrings__QueenZoneLegacy = [string]((
  $settings | Where-Object { $_.name -eq 'ConnectionStrings__QueenZoneLegacy' }
).value)
```

The current live target is `queenzone-db` on `queenzone-sql-server.database.windows.net`. Do not print the full value; parse and report only non-secret metadata such as server and database names.

Minimal redacted connectivity check:

```powershell
$b = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($env:ConnectionStrings__QueenZoneLegacy)
$env:SQLCMDPASSWORD = $b.Password
try {
  sqlcmd -S $b.DataSource -d $b.InitialCatalog -U $b.UserID `
    -Q "SET NOCOUNT ON; SELECT DB_NAME() AS database_name, @@SERVERNAME AS server_name;" `
    -b -l 30 -h -1 -W
}
finally {
  Remove-Item Env:\SQLCMDPASSWORD -ErrorAction SilentlyContinue
}
```

## Create A Local MCP Config

Create the base config:

```powershell
.\.tools\data-api-builder\dab.exe init `
  --database-type mssql `
  --connection-string "@env('ConnectionStrings__QueenZoneLegacy')" `
  --config .tools\dab-queenzone-mcp.json `
  --host-mode development `
  --rest.enabled true `
  --graphql.enabled false `
  --mcp.enabled true
```

DAB currently requires either REST or GraphQL to be enabled at init time, even when MCP is enabled. Keep the process local and expose only reviewed entities.

Add a minimal read-only `News` entity:

```powershell
.\.tools\data-api-builder\dab.exe add News `
  --source dbo.NEWS_T `
  --source.type table `
  --source.key-fields NEWS_ID `
  --permissions "anonymous:read" `
  --description "Published QueenZone news articles. Use only read operations for public archive and editorial checks." `
  --fields.exclude "ARTICLE,USER_ID,EDITOR_EMAIL" `
  --mcp.dml-tools true `
  --config .tools\dab-queenzone-mcp.json
```

This intentionally exposes only a small public-news surface. Add more entities only after checking `docs/legacy/table-map.md` and excluding private, moderated, deleted, credential, mail, IP, or user-account data.

## Run The MCP Server

From the repository root:

```powershell
$json = Get-Content -Raw .\src\QueenZone.Web\appsettings.Local.json | ConvertFrom-Json
$env:ConnectionStrings__QueenZoneLegacy = [string]$json.ConnectionStrings.QueenZoneLegacy

.\.tools\data-api-builder\dab.exe start --mcp-stdio --config .tools\dab-queenzone-mcp.json
```

From another working directory, use absolute paths:

```powershell
C:\workspace\QueenZone.Modern\.tools\data-api-builder\dab.exe start --mcp-stdio --config C:\workspace\QueenZone.Modern\.tools\dab-queenzone-mcp.json
```

## Smoke Test

If using an MCP-capable agent UI, connect the server command above and call:

- `initialize`
- `tools/list`
- `describe_entities`
- `read_records` with `entity = "News"` and a small `first` value

Expected server info:

```text
SQL MCP Server 2.0.x
```

Expected tool names include:

```text
aggregate_records
create_record
delete_record
describe_entities
execute_entity
read_records
update_record
```

Even though the DAB server advertises write-capable tool names, the `News` entity permissions above allow only `READ`.

## Agent Safety Rules

- Prefer read-only MCP calls and direct read-only catalog queries for diagnostics.
- Do not expose `USERS_T`, mail tables, private messages, IP/moderation tables, tracker/download tables, or error logs through DAB without explicit review.
- Do not enable write permissions in DAB unless the user has explicitly approved a scoped write task.
- Do not commit `.tools\dab-queenzone-mcp.json` if it contains local-only assumptions; `.tools` is ignored by design.
- Report whether real legacy database checks were run in PRs and handoffs.
- Use `DISPLAY = 1` as the public visibility gate for legacy public content unless the relevant repository code says otherwise.

## When To Use Direct SQL Instead

DAB MCP is good for a controlled entity surface. Use direct read-only SQL through `sqlcmd`, Dapper, or EF tooling when you need:

- index metadata
- execution stats
- missing-index DMV output
- stored procedure definitions
- query plan or `SET STATISTICS IO/TIME` analysis

Keep those scripts local or commit only sanitized, reusable read-only diagnostics.
