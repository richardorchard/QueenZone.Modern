# QueenZone Modern

This repository contains the new QueenZone modernization project.

The goal is to restart QueenZone as a clean, modern public site that fully exposes the valuable legacy archive while also making news the first live editorial slice. The public archive should be read-only for visitors, but approved editors will be able to add new news articles once the news workflow is in place.

## Repository Shape

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

To point a local app instance at the Azure SQL development database, use your signed-in Entra identity rather than a SQL password:

```json
{
  "ConnectionStrings": {
    "QueenZoneLegacy": "Server=tcp:queenzone-sql-server.database.windows.net,1433;Database=queenzone-dev-db;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;"
  }
}
```

The local Entra database user is `richard@thinkingwebsites.com.au`. It should be granted only the permissions needed for local testing, usually `db_datareader` and only `db_datawriter` when write-path testing is intentional.

## Testing And Workflow

Follow the layered testing policy in `docs/architecture/testing-policy.md`.

Default verification:

```powershell
dotnet restore QueenZone.sln
dotnet build QueenZone.sln --configuration Release --no-restore
dotnet test QueenZone.sln --configuration Release --no-build
```

CI also publishes a code coverage artifact for pull requests and pushes. Use it to guide test review around risky changes, especially canonical routing, publication rules, legacy data mapping, and HTML sanitisation.

Normal CI and pull request checks should not require the restored legacy database. Real legacy database checks are opt-in until a controlled test database exists.

Feature work should happen on an agent-prefixed branch such as `grok/news-pagination` and be reviewed through a pull request before it reaches `main`. See `AGENTS.md` for the branch and PR policy.

## Deployment

The `Deploy App Service` GitHub Actions workflow deploys `main` to the `queenzone-dev` Azure App Service at `https://queenzone-dev.azurewebsites.net`.

The planned public canonical domain for the site is `https://www.queenzone.org`. SEO features that emit absolute public URLs, such as sitemaps and robots.txt, should use that host in production configuration.

Repository secrets required:

- `AZURE_WEBAPP_PUBLISH_PROFILE`: the App Service publish profile XML.

App Service configuration required:

- `ConnectionStrings__QueenZoneLegacy`: the Azure SQL connection string for the copied legacy tables, using Managed Identity authentication.

The `queenzone-dev` App Service uses its system-assigned Managed Identity to connect to Azure SQL. The matching database user is created in the target database, not `master`, and should have only the permissions the app needs:

```sql
CREATE USER [queenzone-dev] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [queenzone-dev];
```

Only add write permissions if the deployed app has an intentional write path:

```sql
ALTER ROLE db_datawriter ADD MEMBER [queenzone-dev];
```

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
