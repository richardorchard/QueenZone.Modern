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
    QueenZone.Import/
  tests/
    QueenZone.Data.Tests/
    QueenZone.Web.Tests/
  docs/
    architecture/
    legacy/
    decisions/
    backlog/
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

The hosted App Service currently connects to Azure SQL with SQL authentication. For local development, use a local-only SQL auth connection string or another explicitly granted development principal:

```json
{
  "ConnectionStrings": {
    "QueenZoneLegacy": "Server=tcp:queenzone-sql-server.database.windows.net,1433;Database=queenzone-db;User ID=...;Password=...;Encrypt=True;TrustServerCertificate=False;"
  }
}
```

Do not commit real usernames, passwords, publish profiles, or copied Azure setting values. If a SQL password contains semicolons or other connection-string delimiters, wrap it using the standard connection-string escaping rules before saving it in Azure or local settings.

## Testing And Workflow

Follow the layered testing policy in `docs/architecture/testing-policy.md`.

Default verification:

```powershell
dotnet restore QueenZone.sln
dotnet build QueenZone.sln --configuration Release --no-restore
dotnet test QueenZone.sln --configuration Release --no-build
```

CI also publishes a code coverage artifact for pull requests and pushes. Use it to guide test review around risky changes, especially canonical routing, publication rules, legacy data mapping, and HTML sanitisation.

A Playwright browser smoke suite (`tests/QueenZone.Web.E2E`) runs in CI on a self-hosted Windows runner so it does not consume GitHub Actions minutes. See `docs/architecture/self-hosted-e2e-runner.md` for runner setup. To run it locally:

```powershell
.\tests\QueenZone.Web.E2E\bin\Release\net10.0\playwright.ps1 install chromium
dotnet publish src/QueenZone.Web/QueenZone.Web.csproj --configuration Release --output ./e2e-app
# start the published app on http://127.0.0.1:5099, then:
dotnet test tests/QueenZone.Web.E2E/QueenZone.Web.E2E.csproj --configuration Release
```

Normal CI and pull request checks should not require the restored legacy database. Real legacy database checks are opt-in until a controlled test database exists.

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

Public news reads still use Dapper. Admin writes and audit logging use EF Core (`QueenZoneDbContext`).

### Admin authentication (Microsoft Entra ID)

Admin routes require Microsoft Entra ID sign-in and an allowed admin email address.

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

For automated tests, send `X-Test-User-Email` with an allowed admin email address.

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
