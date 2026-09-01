# OpenTofu CI, drift detection, and operational runbooks

Issue: [#625](https://github.com/richardorchard/QueenZone.Modern/issues/625), step 8 of epic [#615](https://github.com/richardorchard/QueenZone.Modern/issues/615).

This is the CI-facing companion to
[`opentofu-contributor-runbook.md`](opentofu-contributor-runbook.md) (local
validation, provider upgrades, first-import, state moves) and
[`opentofu-state-and-identity.md`](opentofu-state-and-identity.md) (backend,
identities, lock recovery, backup/restore, credential rotation). It does not
repeat that content — it explains how the automated workflows fit together,
gives rollback guidance by resource class, and links disaster recovery.

## Prerequisite: Cloudflare plan/apply tokens

Every plan or apply touches `module.cloudflare_edge` alongside the Azure
modules, since the production root plans them together. Before the workflows
below can succeed:

1. Create the two Cloudflare API tokens described in
   `opentofu-state-and-identity.md`'s "Cloudflare token strategy" (scoped to
   read-only for plan, edit for apply — never the Global API Key).
2. Store them in the `Queenzone Development` Bitwarden project as
   `CLOUDFLARE_API_TOKEN_TOFU_PLAN` and `CLOUDFLARE_API_TOKEN_TOFU_APPLY`.
3. Add two GitHub repository variables, `BITWARDEN_OPENTOFU_PLAN_SECRETS` and
   `BITWARDEN_OPENTOFU_APPLY_SECRETS`, each mapping the matching Bitwarden
   secret ID to `CLOUDFLARE_API_TOKEN`, following the exact format documented
   in [`bitwarden-secrets.md`](../bitwarden-secrets.md).

Until this is done, every workflow below fails fast at an explicit
"`CLOUDFLARE_API_TOKEN` is not configured" step rather than hanging or
failing unpredictably later.

## How the workflows fit together

| Workflow | Trigger | Identity | Can it change production? |
| --- | --- | --- | --- |
| [`opentofu-plan.yml`](../../.github/workflows/opentofu-plan.yml) | Pull request touching `infra/**` | `opentofu-plan` (Reader) | No — read-only plan, posts a PR comment |
| [`opentofu-apply.yml`](../../.github/workflows/opentofu-apply.yml) | Push to `main` touching `infra/**`, or manual dispatch | `opentofu-plan` for its own `plan` job, then `opentofu-apply` (Contributor) for `apply` | Yes, only after the required reviewer approves the `opentofu-apply` environment |
| [`opentofu-drift.yml`](../../.github/workflows/opentofu-drift.yml) | Daily schedule, or manual dispatch | `opentofu-plan` (Reader) only | No — structurally incapable of applying |

`opentofu-plan.yml`'s `fmt-validate` job runs for every PR, including forks,
with no cloud credentials at all (`scripts/Test-OpenTofu.ps1`). Its `plan`
job only runs for same-repository PRs — a fork PR cannot reach it, and even
if it did, the `opentofu-plan` GitHub environment only trusts OIDC exchanges
from this repository's protected branches.

`opentofu-apply.yml`'s `apply` job applies the **exact binary plan artifact**
produced by its own `plan` job in the same run — never a freshly generated
plan — so "apply only a plan produced from the reviewed commit" holds by
construction, not by convention. Nothing else can run between the two jobs:
the workflow's `concurrency` group serializes runs, and the AzureRM backend's
blob lease would block a second writer regardless.

This is entirely separate from
[`deploy.yml`](../../.github/workflows/deploy.yml) (application releases,
EF migrations, App Service zip deploys). Neither workflow triggers the
other, and they use different GitHub environments and different OIDC
identities.

## Provider/OpenTofu upgrades in CI

The upgrade *procedure* (one provider per PR, refresh checksums, refresh-only
plan first) is unchanged and lives in `opentofu-contributor-runbook.md`. What
CI adds: a version-bump PR runs through the same `opentofu-plan.yml` as any
other infra change, so the resulting plan is reviewed and posted as a PR
comment before merge, and the resulting apply goes through the same
approval-gated `opentofu-apply.yml` as any other change — there is no
separate "upgrade" pipeline.

## Rollback guidance by resource class

OpenTofu has no built-in "rollback" — a bad apply is undone by applying a
corrected configuration, not by reverting state. What "corrected
configuration" means differs by resource class:

- **Azure web/telemetry (`azure-web` module)** — revert the PR on `main` and
  let the next `opentofu-apply` run plan the revert. `prevent_destroy` means
  a revert that would delete the web app, service plan, or hostname bindings
  fails closed instead of applying; if that happens, hand-edit the reverted
  configuration to match the resource's actual current state instead of
  deleting-and-recreating.
- **Azure SQL/Storage (`azure-data` module)** — same revert-and-replan
  approach for configuration (SKU, auditing, container ACLs). OpenTofu never
  owns schema or row/blob data, so a bad apply here cannot corrupt content —
  only settings. `prevent_destroy` blocks the database and storage account
  from ever being destroyed or replaced through this path.
- **Cloudflare edge (`cloudflare-edge` module)** — DNS, zone settings,
  Worker scripts/routes: revert-and-replan, same as above. A DNS or proxy
  misconfiguration is the highest-blast-radius category (can take the public
  site offline or expose the Azure origin directly) — if a revert-and-replan
  is not fast enough, the state-and-identity doc's break-glass dashboard
  process is the immediate mitigation, reconciled back into OpenTofu
  afterward as its own PR.
- **App Service application settings** — outside OpenTofu entirely per
  [ADR 0008](../decisions/0008-app-service-settings-ownership.md). Rotate/fix
  directly via `az webapp config appsettings set` or the Bitwarden/App
  Service flow in `bitwarden-secrets.md` — there is no Tofu apply/plan step
  to run or wait on for these.

## Cutover / post-apply verification

`opentofu-apply.yml`'s `post-apply-smoke` job runs
[`scripts/Test-OpenTofuPostApplySmoke.ps1`](../../scripts/Test-OpenTofuPostApplySmoke.ps1)
after every apply: the general route suite (`Smoke-LiveSite.ps1`), a direct
Azure origin check (`GET /health` on `queenzone-dev.azurewebsites.net` must
return 403), `/health/ready` reachability, the `cdn2.queenzone.org/songfiles/*`
→ 404 contract, and a Cloudflare-proxy reachability check on
`cdn.queenzone.org`. Application Insights freshness is checked best-effort
and never blocks the workflow.

A failure in this job does not undo the apply (see Rollback guidance above);
it fails the workflow loudly so it is investigated immediately rather than
discovered later.

## Drift detection

`opentofu-drift.yml` runs `tofu plan -detailed-exitcode` daily using the
Reader-only `opentofu-plan` identity — it can report drift but cannot
apply. On detected drift it opens (or comments on) a GitHub issue labeled
`opentofu-drift` with the redacted summary and a link to the run, so drift is
an actionable, visible item rather than a workflow log nobody reads. Not
every drift is a problem: a deliberate break-glass dashboard change (state-
and-identity doc) will show up here too — the response is to either
reconcile it into a PR or document why it's intentional, not to
auto-suppress the alert.

## Disaster recovery

Issue [#596](https://github.com/richardorchard/QueenZone.Modern/issues/596)
(production disaster-recovery / restore runbook, RPO/RTO) is **open and
undecided** as of this writing — there is no finished DR document to link to
yet. What exists today: OpenTofu's own state backup/restore and lock
recovery procedures in `opentofu-state-and-identity.md`, which cover the
*infrastructure control plane* (recreating/reconciling Azure and Cloudflare
resource configuration) but explicitly not SQL data, blob contents, or
application-level recovery. When #596 is resolved, this section should link
directly to the resulting `docs/architecture/disaster-recovery.md` and note
which parts of a real incident OpenTofu's runbooks cover versus which parts
that document covers.

## First real apply after this issue lands

PR #892 (Cloudflare edge import) added
`ip_restriction_default_action = "Deny"` to the production web app's
`site_config` but never ran it through a real apply — there was no apply
pipeline yet. The first `opentofu-apply.yml` run after this issue merges will
very likely propose that change. It is a real, deliberate, network-facing
change (tightening the App Service's default access-restriction behavior)
and — like every apply — still requires manual approval on the
`opentofu-apply` environment before anything happens. Review that diff
carefully rather than approving on reflex just because it's the "first" run.
