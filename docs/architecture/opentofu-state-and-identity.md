# OpenTofu remote state and workload identities

Issue: [#616](https://github.com/richardorchard/QueenZone.Modern/issues/616), step 2 of epic [#615](https://github.com/richardorchard/QueenZone.Modern/issues/615).

**Bootstrapped and locally verified:** 2026-08-13. No application resource was imported or changed.

## Boundary

The state control plane is separate from `Queenzone-RG` and the `queenzone` media account. The production stack consumes the backend; it does not create or manage it.

| Control | Value |
| --- | --- |
| Resource group | `Queenzone-IaC-RG` |
| Storage account | `queenzonetfstate` |
| Container | `tfstate` (private) |
| State key | `production.tfstate` |
| Authentication | Entra ID only; shared-key access disabled |
| Data protection | Blob versioning; blob and container soft delete, 30 days |
| Deletion protection | `CanNotDelete` lock on the storage account |
| Locking | AzureRM backend blob lease; never use `-lock=false` for writes |

The non-secret backend settings are committed at [`infra/backend/production.backend.hcl`](../../infra/backend/production.backend.hcl). Subscription, tenant, and workload client IDs come from the authenticated Azure CLI locally or GitHub environment variables in Actions.

## Identity and privilege model

| Principal | State scope | Azure production scope | GitHub trust |
| --- | --- | --- | --- |
| Authorised local operator | `Storage Blob Data Contributor` on `tfstate` only | Existing operator access; bootstrap adds none | N/A |
| `QueenZone OpenTofu Plan` | `Storage Blob Data Contributor` on `tfstate` only | `Reader` on `Queenzone-RG` | `opentofu-plan` environment |
| `QueenZone OpenTofu Apply` | `Storage Blob Data Contributor` on `tfstate` only | `Contributor` on `Queenzone-RG` | approval-gated `opentofu-apply` environment |

Neither workload identity is Owner or User Access Administrator. Contributor is limited to the QueenZone resource group and cannot change Azure RBAC. State access is a separate container-scoped data-plane assignment, not storage-account key access.

Both GitHub environments accept protected branches only. The apply environment requires `richardorchard` approval. The federated subjects name the environments, so an untrusted fork or pull request cannot exchange its OIDC token unless it first passes the repository environment controls. The manual [`opentofu-backend-smoke.yml`](../../.github/workflows/opentofu-backend-smoke.yml) workflow verifies each identity without declaring or importing application resources. Full plan/apply automation belongs to #625.

## Bootstrap and local backend migration

Prerequisites: Azure CLI and GitHub CLI authenticated as an authorised owner. Install OpenTofu with `winget install --exact --id OpenTofu.Tofu` when it is not already available.

```powershell
az account set --subscription 610e3b3a-028d-4f1b-ac1d-a5567a4f8b9d
./infra/bootstrap/Bootstrap-OpenTofuState.ps1 -WhatIf
./infra/bootstrap/Bootstrap-OpenTofuState.ps1
./infra/bootstrap/Test-OpenTofuState.ps1
```

For the later production configuration:

```powershell
tofu -chdir=infra/environments/production init `
  -backend-config=../../backend/production.backend.hcl `
  -migrate-state
```

Before confirming migration, copy any local state outside the repository and inspect `tofu state list`. Never commit a state file or pass credentials through `-backend-config`. No production state exists until the later import issues begin.

## Cloudflare token strategy

Create two Cloudflare API tokens. Restrict both to account `f93121b2086286e79a7a9fdb8d03cb4c` and zone `079fc2f37095c82fb3a2b4da65718b2b`; do not use the Global API Key.

| Token | Account permissions | Zone permissions |
| --- | --- | --- |
| Plan | Workers Scripts Read | Zone Read, DNS Read, Zone Settings Read, SSL and Certificates Read, Workers Routes Read |
| Apply | Workers Scripts Edit | Zone Read, DNS Edit, Zone Settings Edit, SSL and Certificates Edit, Workers Routes Edit |

Add only permissions required by an observed provider denial. Do not grant account-wide zone edit, billing, member, token-management, or unrelated Workers permissions.

Store the values in the `Queenzone Development` Bitwarden Secrets Manager project as `CLOUDFLARE_API_TOKEN_TOFU_PLAN` and `CLOUDFLARE_API_TOKEN_TOFU_APPLY`. GitHub later retrieves them through its existing Bitwarden Secrets Manager action into the matching protected environment. OpenTofu reads `CLOUDFLARE_API_TOKEN` from the process environment; the value must never appear in HCL, `.tfvars`, workflow output, plans, or state.

Those two OpenTofu token names are **not created yet**. Live Worker publishes use `CLOUDFLARE_WORKER_READWRITE` (Workers Scripts Edit). Inventory/ops reads use `CLOUDFLARE_API_TOKEN_READONLY`. Do not reuse the Worker token as the future OpenTofu apply token unless its zone/DNS/settings scopes are reviewed.

The bootstrap does not create the OpenTofu tokens. #626 declares the Cloudflare
resources and import blocks; create `CLOUDFLARE_API_TOKEN_TOFU_PLAN` and
`CLOUDFLARE_API_TOKEN_TOFU_APPLY` immediately before the first remote Cloudflare
plan, then wire their Bitwarden output names during #625.

Rotation sequence:

1. Create a replacement token with the same scope and a new expiry.
2. Update the matching Bitwarden secret without printing it.
3. Run the manual backend smoke plus the relevant Cloudflare read or no-op plan.
4. Revoke the old token only after the new token succeeds.
5. Record the rotation date and owner, never the token value.

## Backup and recovery

### State backup

Before imports, moves, provider upgrades, or a high-risk apply:

```powershell
tofu -chdir=infra/environments/production state pull > $env:TEMP\queenzone-production.tfstate.backup
```

Treat the backup as a secret. Keep it outside the repository, encrypt it at rest, restrict access, and delete it after the recovery window. Azure blob versions and 30-day soft delete are the primary in-account recovery path; an operator backup covers account-level incidents.

### Restore a prior version

1. Stop all plan/apply jobs and record the current blob version ID.
2. Download the chosen earlier blob version to an encrypted local path.
3. Inspect its lineage and serial without exposing contents.
4. Use `tofu state push` only from a clean checkout at the matching commit.
5. Run a refresh-only plan, then a normal plan, before allowing applies.

Do not delete newer versions until recovery is confirmed.

### Lock recovery and force unlock

The AzureRM backend uses a blob lease for state locking. First confirm no GitHub job or operator process is active. Capture the lock ID and job evidence, then run:

```powershell
tofu -chdir=infra/environments/production force-unlock <LOCK_ID>
```

Force-unlock removes coordination only; it does not repair state. Never break the blob lease directly while an OpenTofu process may still be writing.

### Account or container deletion

The storage-account lock must be removed before account deletion. Treat lock removal as break-glass: require a reviewed change, record who removed it, restore the account/container within the soft-delete window, reapply the lock, then run the backend smoke. If the account itself is lost, restore the latest encrypted operator backup into a newly approved backend and use `tofu init -migrate-state`; do not recreate production resources.

## Credential recovery

- Local operator: reauthenticate with `az login`, then restore only the container-scoped data role.
- GitHub OIDC: add a replacement Entra application/federated credential, update the environment's non-secret `ARM_CLIENT_ID`, run the smoke, then remove the old application and assignments.
- Apply approval: restore the required reviewer and protected-branch policy before enabling the apply identity.
- Cloudflare: follow the create-test-revoke rotation sequence above.

No backend credential, token, storage key, state content, or application resource belongs in this repository.
