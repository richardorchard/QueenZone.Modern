# Production region migration

Issue: [#1272](https://github.com/richardorchard/QueenZone.Modern/issues/1272),
Phase 7 of [epic #1264](https://github.com/richardorchard/QueenZone.Modern/issues/1264).

## Target and safety boundary

ADR 0017 approves `eastus` for production. The App Service, SQL server and
database, and Blob Storage move together. The existing `Queenzone-RG`
resource group remains; its own `australiaeast` location is metadata and does
not constrain child-resource locations.

The replacement names are:

| Resource | Target |
| --- | --- |
| App Service plan | `ASP-Queenzone-Prod` |
| App Service | `queenzone-prod` |
| Log Analytics | `queenzone-prod-law` |
| Application Insights | `queenzone-prod-ai` |
| SQL server | `queenzone-prod-sql` |
| SQL database | `queenzone-db` |
| Storage account | `queenzoneprod` |

Never combine the build, data-copy, cutover, and retirement stages into one
apply. Each gate must leave the old production path intact until the new path
has passed its checks. Never run `tofu destroy` against either estate.

## Stage 1: build the parallel target

The production apply plan runs `Test-AzureMigrationTargetCapacity.ps1` before
the approval gate. If the target App Service plan or SQL logical server does
not yet exist, the check fails closed unless the requested App Service SKU has
enough regional capacity and Azure SQL reports logical-server provisioning as
available for the subscription.

On **7 September 2026**, the first eastus apply stopped with B1 capacity at
`0/0` and Azure SQL status `Visible` with `ProvisioningDisabled`. It created
only the target Storage account, Log Analytics workspace, and Application
Insights component. Request B1 quota of at least one instance and an Azure SQL
regional provisioning exception before retrying.

The initial configuration keeps the imported `module.azure_web` and
`module.azure_data` unchanged. `module.azure_web_target` and
`module.azure_data_target` create the parallel resources. The candidate app
allows direct access at `https://queenzone-prod.azurewebsites.net` and has no
production hostname binding. The target Storage account has no custom domain.

The target SQL database is deliberately absent. Azure's cross-server copy
operation must create the destination database, so creating an empty database
first would block the supported copy path.

Required evidence before approval:

- local `scripts/Test-OpenTofu.ps1` passes;
- the production remote plan reports creates only;
- the plan contains no update, replacement, or delete;
- the SQL password is shown only as `(write-only attribute)`;
- target resources use `eastus` and the names above.

Merge does not apply immediately. Review the `opentofu-apply.yml` plan summary,
then approve the protected `opentofu-apply` environment only when it still
contains creates and state-address moves only. Reject any run that changes or
deletes an existing resource.

## Stage 2: copy and verify data

Take a fresh, retained database export or backup before the final cutover. Do
not delete or overwrite the source database or source blobs.

Create the database copy through Azure's native cross-server operation:

```powershell
az sql db copy `
  --resource-group Queenzone-RG `
  --server queenzone-sql-server `
  --name queenzone-db `
  --dest-resource-group Queenzone-RG `
  --dest-server queenzone-prod-sql `
  --dest-name queenzone-db `
  --service-objective S0 `
  --backup-storage-redundancy Local
```

Copy Blob Storage container-by-container using a short-lived, read/list-only
source user-delegation SAS and authenticated destination writes. Never print a
SAS, account key, connection string, or blob content. Poll every server-side
copy to success before comparison.

Compare exact row counts for every user table. Compare blob count and total
content length for every container. Any mismatch blocks deployment and
cutover. Record only table/container names, counts, byte totals, and mismatch
details; do not record row values, blob names, or credentials.

After the database copy is verified, add it to the target module with
`manage_sql_database = true` and a declarative import at its target address.
The follow-up plan may show that import but must still show no replacement or
delete.

## Stage 3: configure and test the candidate

Create candidate connection strings by changing only the SQL server and
Storage account endpoints. Store them in Bitwarden; never commit or print
them. Copy the remaining production application settings by name and value
through a secret-safe operator script, then update only the candidate
connection strings and Application Insights connection.

Grant the existing production deploy identity Website Contributor on
`queenzone-prod`, add a candidate publish profile in Bitwarden, and deploy the
same verified build that production runs. Do not change `deploy.yml`'s default
target yet.

Before DNS changes, test the candidate hostname directly:

- `/health`, `/health/ready`, `/`, `/news`, and representative archive pages;
- forum reads and an authenticated forum journey;
- admin and member authentication callbacks;
- representative public and private blobs;
- mobile API health, public lists/details, authentication, and member routes.

Source and candidate may both receive writes during this stage. Treat the
candidate as disposable until the final copy.

## Stage 4: final copy and cutover

Schedule a write freeze. Stop the old app or enable a maintenance response so
no writes occur after the final copy starts. Recreate the candidate database
from a fresh Azure copy and run a final incremental blob copy. Repeat every
row and blob inventory check.

Bind `queenzone.org` and `www.queenzone.org` to `queenzone-prod` with valid SNI
certificates before changing traffic. Repoint the Cloudflare apex and `www`
records to `queenzone-prod.azurewebsites.net`, then disable direct candidate
ingress by restoring the Cloudflare-only default deny rule. Update
`deploy.yml` and its deploy identity/publish-profile mappings to the new app.

Verify the live hostname over HTTPS, including build stamp, full route smoke,
auth, forum, uploads, Blob media, and mobile API journeys. If a blocking check
fails, point Cloudflare back to `queenzone-dev`, restore the old app to service,
and end the write freeze. Do not attempt an OpenTofu state rollback during the
traffic incident.

## Stage 5: observation and retirement

Keep the old App Service, plan, SQL database/server, Storage account, and the
immediate pre-cutover backup throughout the agreed observation window. Do not
remove their state or delete them in the cutover change.

After the observation window passes with no unresolved mismatch:

1. Back up remote OpenTofu state and stop all applies.
2. Use reviewed `tofu state rm` commands for the old resource addresses.
3. Remove the old import blocks and configuration in a follow-up pull request.
4. Confirm the normal plan has no delete or replacement.
5. Manually delete only the exact old Azure resources after one final target
   inventory and backup check.
6. Record the retained backup location and expiry without recording secrets.

Update current-state documentation only after live cutover is proven. The
final repository search must find no operational reference that still targets
the old `queenzone-dev` App Service; historical incident notes may retain the
old name when clearly dated.
