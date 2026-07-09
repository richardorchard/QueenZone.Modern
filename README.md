# QueenZone Modern

This repository contains the new QueenZone modernization project.

The goal is to restart QueenZone as a clean, modern public site that fully exposes the valuable legacy archive while also making news the first live editorial slice. The public archive should be read-only for visitors, but approved editors will be able to add new news articles once the news workflow is in place.

## Repository Shape

`QueenZone.Web` uses ASP.NET Core Razor Pages for server-rendered public and admin UI. Keep page rendering in `Pages/` with `.cshtml` and page-model classes; use endpoint routes only for small non-page endpoints such as health checks or future APIs.

```text
/
  QueenZone.sln
  src/
    QueenZone.Web/
      Pages/
    QueenZone.Data/
    QueenZone.NewsAgent/
    QueenZone.NewsAgent.Worker/
    QueenZone.Import/
  tests/
    QueenZone.Data.Tests/
    QueenZone.Web.Tests/
  docs/
    architecture/
    legacy/
    decisions/
    backlog/
  scripts/
    Smoke-NewsAgent.bat
```

## Local Development

The app targets .NET 10.

```powershell
dotnet restore QueenZone.sln
dotnet build QueenZone.sln
dotnet test QueenZone.sln
dotnet run --project src/QueenZone.Web/QueenZone.Web.csproj
```

To generate a local code coverage report:

```powershell
dotnet tool restore
dotnet test QueenZone.sln --configuration Release --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory ./TestResults
dotnet tool run reportgenerator -reports:".\TestResults\**\coverage.cobertura.xml" -targetdir:".\coverage-report" -reporttypes:"HtmlInline;Cobertura;MarkdownSummary"
```

Open `coverage-report/index.html` to inspect the report. Coverage reports and raw test result folders are local artifacts and should not be committed.

Local secrets belong in `src/QueenZone.Web/appsettings.Local.json`, which is ignored by git. You can also set `ConnectionStrings__QueenZoneLegacy` in your shell or a local `.env` file for tooling that loads dotenv values. If no `ConnectionStrings:QueenZoneLegacy` value is present, the site uses sample news data so the first slice can still run locally.

### Using live legacy data locally

Live-data development is opt-in. Use it when you need to reproduce a production-only data mapping, SQL, or admin editorial issue. Prefer a local SQL Server copy for repeat debugging so normal local work does not depend on Azure SQL latency, firewall rules, or live database load.

#### Option A: point at a local SQL Server copy

Install or use a local SQL Server instance such as SQL Server Express. On Richard's workstation the instance is:

```text
glory11\sqlexpress
```

Export the Azure SQL database to a BACPAC. Read the source connection string from an ignored local settings file, or paste it into a temporary shell variable; do not print it to the console or commit it.

```powershell
$settings = Get-Content -Raw .\src\QueenZone.Web\appsettings.Local.json | ConvertFrom-Json
$sourceConnectionString = [string]$settings.ConnectionStrings.QueenZoneLegacy

New-Item -ItemType Directory -Force C:\Backups | Out-Null
SqlPackage /Action:Export `
  /SourceConnectionString:$sourceConnectionString `
  /TargetFile:C:\Backups\queenzone-live.bacpac `
  /p:CommandTimeout=1200
```

Import the BACPAC into SQL Express:

```powershell
SqlPackage /Action:Import `
  /SourceFile:C:\Backups\queenzone-live.bacpac `
  /TargetConnectionString:"Server=glory11\sqlexpress;Initial Catalog=QueenZoneLocal;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;" `
  /p:CommandTimeout=1200
```

Verify the import:

```powershell
sqlcmd -S "glory11\sqlexpress" -E -d QueenZoneLocal -Q "SELECT DB_NAME() AS DatabaseName; SELECT COUNT_BIG(*) AS NewsRows FROM dbo.NEWS_T;"
```

Point the web app at the local copy in `src/QueenZone.Web/appsettings.Local.json`:

```json
{
  "ConnectionStrings": {
    "QueenZoneLegacy": "Server=glory11\\sqlexpress;Database=QueenZoneLocal;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;"
  },
  "AzureAd": {
    "ClientId": ""
  },
  "Admin": {
    "AllowedEmails": [
      "richard@thinkingwebsites.com.au",
      "me@richardorchard.com"
    ]
  }
}
```

SQL Server Express has a 10 GB database-size limit. The imported QueenZone copy was about 3 GB on 2026-07-01, so Express was sufficient then. Use SQL Server Developer edition if the local copy grows beyond the Express limit.

#### Option B: point directly at Azure SQL

Create `src/QueenZone.Web/appsettings.Local.json` and add the live Azure SQL connection string under `ConnectionStrings:QueenZoneLegacy`. This file is ignored by git; do not commit it, paste it into a pull request, or include it in logs.

For local admin testing without real Entra sign-in, also set `AzureAd:ClientId` to an empty string and include your admin email in `Admin:AllowedEmails`:

```json
{
  "ConnectionStrings": {
    "QueenZoneLegacy": "Server=tcp:...;Database=...;User ID=...;Password=...;Encrypt=True;TrustServerCertificate=False;"
  },
  "AzureAd": {
    "ClientId": ""
  },
  "Admin": {
    "AllowedEmails": [
      "richard@thinkingwebsites.com.au",
      "me@richardorchard.com"
    ]
  }
}
```

Run the web app normally:

```powershell
dotnet run --project src/QueenZone.Web/QueenZone.Web.csproj
```

To exercise admin pages locally with the test-header fallback, send the allowlisted email header:

```powershell
Invoke-WebRequest `
  -Uri http://localhost:5146/admin/news `
  -Headers @{ "X-Test-User-Email" = "richard@thinkingwebsites.com.au" } `
  -UseBasicParsing
```

If you already have `src/QueenZone.NewsAgent.Worker/appsettings.Local.json` configured with `ConnectionStrings:QueenZoneLegacy`, you can copy that value into the web app's local settings. Keep both files local-only.

The local SQL copy may contain production user, mail, IP, moderation, and private-message data. Treat local BACPAC and MDF/LDF files as sensitive data: store them outside the repository, do not attach them to issues or pull requests, and delete or refresh them deliberately when they are no longer needed.

### News agent (discovery worker)

The news agent fetches configured public sources, triages items with OpenRouter, generates editor-reviewable drafts, and stores candidates for the admin review queue. It does not publish to public pages automatically.

Full setup, worker flags, and admin review UI are documented in `docs/architecture/news-agent.md`.

Quick start:

```powershell
copy src\QueenZone.NewsAgent.Worker\appsettings.Local.json.example src\QueenZone.NewsAgent.Worker\appsettings.Local.json
# Edit appsettings.Local.json: set OpenRouter:ApiKey and optionally ConnectionStrings:QueenZoneLegacy

dotnet run --project src/QueenZone.NewsAgent.Worker -- discover-news --seed-sources --triage --draft
```

OpenRouter smoke test (Windows): double-click `scripts/Smoke-NewsAgent.bat`.

Admin review queue (after signing in as an allowed admin): `/admin/news-discovery`. Promoted drafts are edited and published through the existing `/admin/news` workflow.

The hosted App Service currently connects to Azure SQL with SQL authentication. For local development, use a local-only SQL auth connection string or another explicitly granted development principal:

```json
{
  "ConnectionStrings": {
    "QueenZoneLegacy": "Server=tcp:queenzone-sql-server.database.windows.net,1433;Database=queenzone-db;User ID=...;Password=...;Encrypt=True;TrustServerCertificate=False;"
  }
}
```

Do not commit real usernames, passwords, publish profiles, or copied Azure setting values. If a SQL password contains semicolons or other connection-string delimiters, wrap it using the standard connection-string escaping rules before saving it in Azure or local settings.

Agents can use Azure Data API Builder as a local SQL MCP server for controlled read-only legacy database investigation. Setup lives in `docs/sql/data-api-builder-mcp.md`; the DAB tool, config, and smoke-test artifacts should stay under ignored local paths such as `.tools`.

## Testing And Workflow

Follow the layered testing policy in `docs/architecture/testing-policy.md`.

Default verification:

```powershell
dotnet restore QueenZone.sln
dotnet build QueenZone.sln --configuration Release --no-restore
dotnet test QueenZone.sln --configuration Release --no-build
```

CI also publishes a code coverage artifact for pull requests and pushes. Use it to guide test review around risky changes, especially canonical routing, publication rules, legacy data mapping, and HTML sanitisation.

A Playwright browser smoke suite (`tests/QueenZone.Web.E2E`) runs in CI on a self-hosted Windows runner so it does not consume GitHub Actions minutes. It covers public homepage/news/forum journeys, mobile nav, axe-core critical a11y checks, and admin/editorial smoke. See `docs/architecture/self-hosted-e2e-runner.md` for runner setup and local commands. On failure, screenshots and traces land in `test-results/e2e/` (uploaded as CI artifacts).

Normal CI and pull request checks should not require the restored legacy database. Real legacy database checks are opt-in until a controlled test database exists.

If you change admin news write behavior or discovery promotion behavior, you can run the opt-in legacy write probe against the configured `ConnectionStrings__QueenZoneLegacy` database:

```powershell
$env:RUN_LEGACY_WRITE_PROBE = "true"
.\scripts\Probe-AdminNewsLegacyWrites.ps1
```

The probe is intentionally destructive-but-self-cleaning: it creates, publishes, unpublishes, and deletes a uniquely named admin draft article to verify the real SQL-backed workflow. Only run it when the configured database is one you are comfortable mutating.

Feature work should happen on an agent-prefixed branch such as `grok/news-pagination` and be reviewed through a pull request before it reaches `main`. See `AGENTS.md` for the branch and PR policy.

## Deployment

The `Deploy App Service` GitHub Actions workflow deploys `main` to the `queenzone-dev` Azure App Service at `https://queenzone-dev.azurewebsites.net`.

The planned public canonical domain for the site is `https://www.queenzone.org`. SEO features that emit absolute public URLs, such as sitemaps and robots.txt, should use that host in production configuration.

Set `Site:PublicBaseUrl` in App Service configuration (or `appsettings.Local.json` for local overrides) when the public host differs from the default in `appsettings.json`.

Repository secrets required:

- `AZURE_WEBAPP_PUBLISH_PROFILE`: the App Service publish profile XML.
- `QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING`: Azure SQL connection string used by the deploy workflow to apply EF Core migrations before each release. This is separate from the App Service runtime connection. Use a dedicated SQL login or other deliberately granted principal with permission to create or alter tables.

App Service configuration required:

- `ConnectionStrings__QueenZoneLegacy`: the Azure SQL connection string for the copied legacy tables. The current runtime path uses SQL authentication against `queenzone-db` on `queenzone-sql-server.database.windows.net`.
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: the Application Insights connection string for production/preview telemetry. Leave unset in local development unless local telemetry is intentional.

Application Insights is wired through Azure Monitor OpenTelemetry and is opt-in at runtime: no telemetry is exported unless `APPLICATIONINSIGHTS_CONNECTION_STRING` is present. The default `ApplicationInsights` settings in `src/QueenZone.Web/appsettings.json` are deliberately low-volume for a hobby project: at most 0.2 traces per second, warning-or-higher exported logs, trace-based log sampling enabled, and Live Metrics disabled. In Azure, also set a small daily cap on both the Application Insights resource and its Log Analytics workspace, for example 0.05-0.10 GB/day, so unexpected traffic or noisy logging cannot run up a large bill.

The runtime App Service setting and the GitHub Actions migration secret are intentionally separate. Updating `QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING` in GitHub does not update the live App Service setting. If the database name, login, or password changes, update both places as needed and restart the App Service so the running container reloads the connection string.

The runtime SQL login should be created in the target database and granted only the permissions the app needs. Public archive reads need read access to the legacy news tables. Admin news publishing writes to `NEWS_T` and `NewsAuditLog`, so the runtime login also needs scoped write permissions once the admin workflow is enabled.

Example permission shape:

```sql
CREATE USER [app_login_name] FOR LOGIN [app_login_name];
ALTER ROLE db_datareader ADD MEMBER [app_login_name];
ALTER ROLE db_datawriter ADD MEMBER [app_login_name];
```

Keep read-only environments on `db_datareader` only.

The `Deploy App Service` workflow applies pending EF Core migrations automatically after tests and before deploy. Configure `QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING` in the GitHub `dev` environment secrets.

For manual bootstrap or recovery, you can still run:

```text
docs/sql/001-news-admin-columns.sql
```

or:

```powershell
dotnet tool restore
dotnet ef database update --project src/QueenZone.Data/QueenZone.Data.csproj --startup-project src/QueenZone.Web/QueenZone.Web.csproj
```

Public and admin SQL access uses EF Core (`QueenZoneDbContext`). Hot paths keep stored procedures, invoked via EF (`SqlQuery` / `SqlQueryRaw` / EF-managed proc calls). See ADR 0006.

### Admin authentication

Admin routes require a signed-in user whose email address is listed in `Admin:AllowedEmails`.
The signed-in identity may come from the site OAuth member login (Google, Microsoft, or Facebook)
or from the dedicated Microsoft Entra ID admin sign-in when Entra is configured.

1. Create an Entra app registration for `QueenZone.Web`.
2. Add a web redirect URI such as `https://queenzone-dev.azurewebsites.net/signin-oidc`.
3. Create a client secret for the app registration.
4. Configure these App Service settings:

```text
AzureAd__TenantId
AzureAd__ClientId
AzureAd__ClientSecret
Admin__AllowedEmails__0
Admin__AllowedEmails__1
```

Use `src/QueenZone.Web/appsettings.Local.json` for local Entra values. If `AzureAd:ClientId` is empty locally, the app falls back to test-header authentication for development only.

For automated tests, send `X-Test-User-Email` with an allowed admin email address, or sign in through the OAuth callback test double with an allowlisted email.

Member sign-in at `/account/login` (Google, Microsoft, Facebook OAuth) is separate from admin access. Stripping member OAuth does not remove the admin requirement: admins still need Entra sign-in in production, or the test-header fallback locally as described above.

Admin editorial surfaces:

- `/admin/news` — create, edit, preview, publish news articles
- `/admin/news-discovery` — review discovered candidates and AI-generated drafts (see `docs/architecture/news-agent.md`)

Do not commit publish profiles, `.pubxml` files, local app settings, or connection strings. Rotate the App Service publish profile if it has ever been saved outside GitHub Secrets.

## First Milestone

Build the first vertical slice around news. This slice should prove both archive rendering and the path for new approved news articles:

1. Connect to the restored legacy SQL Server database.
2. Read archived published rows from `NEWS_T`, either directly or through `Q_NEWS_*` stored procedures.
3. Add separate modern tables for newly approved live news articles and draft/review workflow.
4. Render latest news on the homepage as one seamless list across archived and new articles.
5. Render `/news` archive across both archived and new articles.
6. Render `/news/{id}/{slug}` detail pages without exposing the storage split to visitors.
7. Use stable, search-friendly canonical URLs.
8. Keep automated discovery and AI-assisted drafts behind explicit editorial approval.
9. Deploy a preview to Azure App Service.

## Legacy Reference Policy

Keep the old repository available locally as an archaeological reference, but do not copy it wholesale into the new repo.

Useful reference files belong in `docs/legacy`:

- `db-schema.sql`
- table map
- stored procedure index
- legacy URL notes, only when they help understand content relationships
- content inventory

The old source is only copied or ported when a specific page or behavior needs to be understood.
