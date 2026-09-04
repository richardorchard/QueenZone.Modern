# Dev App Service provisioning

Phase 1 of epic #1264, issue #1265. The new root is
`infra/environments/dev`; its backend uses `dev.tfstate` in the existing
protected state container. Production retains `production.tfstate`.
No production resource or import address moves. Resource labels inside the
shared web module still say `production` to preserve existing state addresses;
the dev root's separate state and explicit names determine the actual target.

## Provisioning evidence: 2026-09-04

[PR #1300](https://github.com/richardorchard/QueenZone.Modern/pull/1300) merged
with required checks passing. After the maintainer approved the names and scoped
CI access, an operator applied the one-resource dev group bootstrap. The
[dev-only workflow](https://github.com/richardorchard/QueenZone.Modern/actions/runs/33847788695)
then planned four creates, applied that exact reviewed artifact through the
existing approval gate, and passed its HTTPS smoke check.

Azure CLI verified `queenzone-devbox` is Running on `ASP-Queenzone-Dev` in
`Queenzone-Dev-RG`, Australia East: B1, one worker, Always On, .NET 10,
TLS 1.2 and HTTPS-only. A direct HTTPS request returned HTTP 200 and the
Microsoft Azure App Service welcome page. A fresh dev plan against remote state
returned exit code 0: no creates, updates, deletes or replacements.

Production remained Running on `ASP-Queenzone` in `Queenzone-RG`, B1/one worker.
The duplicate all-roots push workflow was cancelled before apply, so the deleted
DNS record was not recreated. Custom DNS/TLS, data/storage and application
deployment remain with their later epic phases; this proves infrastructure
provisioning, not application release readiness.

## Resources and boundaries

All five resources are new, in Australia East:

| Resource | Name |
| --- | --- |
| Resource group | Queenzone-Dev-RG |
| Linux B1 plan, one worker | ASP-Queenzone-Dev |
| Always-on .NET 10 web app | queenzone-devbox |
| Log Analytics, 30-day retention, 0.1 GB daily cap | queenzone-devbox-law |
| Workspace-linked Application Insights | queenzone-devbox-ai |

The shared module preserves health checks, TLS, logging, Cloudflare ranges,
identity and lifecycle protection. B1 and one worker remain enforced for both
callers. Production's default hostnames and ingress cannot be relaxed by the
new dev options. Dev initially permits direct access to its Azure HTTPS
hostname. This exposes only Azure's empty-app placeholder until Phase 4 deploys
the application. No production settings, databases or blobs are copied.
`WEBSITE_WARMUP_PATH=/health` is seeded at creation; subsequent settings remain
operator/deployment-owned under the existing ignore_changes boundary. Application
Insights runtime wiring belongs with those settings in Phase 4.

## DNS and managed TLS handoff

`enable_custom_domain` defaults to false. Phase 1 creates no hostname binding
or certificate because DNS has not been pointed at this app. The maintainer
reported deleting the old dev DNS record on 2026-09-04; this does not prove
that the Static Web App itself has been decommissioned.

In Phase 3 (#1267), finish decommissioning the APK site, set the CNAME directly
to `queenzone-devbox.azurewebsites.net`, publish Azure's ownership TXT value
where required, and then enable the custom-domain option in committed HCL.
That creates the hostname binding, Azure-managed certificate and SNI binding,
and restores Cloudflare-only app ingress. Do not enable it before DNS is ready.

Managed certificates require no supplied PFX/private key, so they avoid the
production uploaded-certificate exception. Azure requires a direct CNAME for
subdomains; an intermediate CNAME or Cloudflare proxy can prevent issuance or
renewal. Review the permanent DNS/proxy arrangement in Phase 3 before switching
on TLS, including renewal, rather than merely passing initial issuance.
See [Azure certificate requirements](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate)
and [AzureRM managed certificates](https://registry.terraform.io/providers/hashicorp/azurerm/5.0.1/docs/resources/app_service_managed_certificate).

Production still declares the old dev DNS record. A plan can propose recreating
it after manual deletion. Do not approve that production plan; reconcile DNS
ownership in Phase 3. The apply workflow's manual `root: dev` selection allows
an independently reviewed dev plan/apply without applying production drift.

## First-use permissions and approval

Initial RBAC inspection on 2026-09-04 found both CI identities scoped to
production only. The approved bootstrap below has now been completed: the dev
group exists and both identities have the listed dev-scoped roles. Do not repeat
the bootstrap for routine applies or grant subscription-wide Contributor.

Because a resource group must exist before resource-group-scoped assignments
can be made, the first bootstrap needs an authorised operator:

1. Confirm the names above and review the full dev plan: five creates only.
2. With explicit approval, use OpenTofu to create **only** the resource group
   in the dev backend. This is a one-time targeted bootstrap, not an Azure CLI
   creation/import or a routine partial apply:

   ```powershell
   tofu '-chdir=infra/environments/dev' init -reconfigure '-backend-config=../../backend/dev.backend.hcl'
   tofu '-chdir=infra/environments/dev' plan '-target=azurerm_resource_group.dev' "-out=$env:TEMP/queenzone-dev-rg.tfplan"
   # Review before applying this exact one-create plan.
   tofu '-chdir=infra/environments/dev' apply "$env:TEMP/queenzone-dev-rg.tfplan"
   ```

3. An authorised RBAC administrator assigns the plan identity
   `859329f3-afcb-4c17-9201-ee21120cfa0d` **Reader** plus the existing
   **QueenZone OpenTofu Plan - App Service Config Reader** custom role on
   `/subscriptions/610e3b3a-028d-4f1b-ac1d-a5567a4f8b9d/resourceGroups/Queenzone-Dev-RG`.
   Assign **Contributor** to apply identity
   `7b466caa-bd0e-4bee-b77f-c9b715fbd36e` on that same group only.
   Confirm the custom role's assignable scopes include this group before use.
   Existing state-container access already covers the separate dev key.
4. Regenerate the full plan through `opentofu-apply.yml`, choosing `root: dev`
   when production drift remains. Review the four remaining creates and approve
   the protected `opentofu-apply` job. The exact checked plan artifact is applied.
   These bootstrap steps were completed on 2026-09-04 after maintainer approval;
   routine applies now use the existing resource group and assignments.

The PR plan uses `pull_request_target`, whose workflow definition comes from
main. Therefore this PR cannot automatically exercise its new dev matrix until
that definition is merged. Use the local read-only dev plan as pre-merge evidence;
verify the new PR matrix on the next infra PR. Do not claim that local operator
access proves the CI identity permissions.

## Verification and completion

Run `./scripts/Test-OpenTofu.ps1` for both roots, or pass
`-EnvironmentName dev`. It checks formatting, credentials, lifecycle protection,
initialisation, validation, and mocked shared-module contract tests. Mock tests
never contact Azure. A real plan must also pass `Test-OpenTofuPlanSafety.ps1`.

After the approved apply, verify the Azure app's HTTPS root serves the default
placeholder and inspect its plan, region and worker count in Azure. An empty
App Service does not implement QueenZone's `/health`; that endpoint is verified
after application deployment in Phase 4. Update the dev smoke URL when Phase 3
closes direct origin access. Record the apply run and live verification before
closing #1265 or starting its dependent phases. Merge alone is not provisioning.
