# QueenZone production OpenTofu root

This root module is the only production entry point. The resource group,
Azure web/telemetry estate, Azure data estate, and Cloudflare edge estate all
use declarative import blocks from #622, #628, and #626.

The first remote plan must show imports and no unexplained change, replacement,
or deletion. Do not apply from a local operator session. The protected
`opentofu-apply` environment remains the only apply path.

Use [`scripts/Test-OpenTofu.ps1`](../../../scripts/Test-OpenTofu.ps1) for local validation. See [`docs/architecture/opentofu-contributor-runbook.md`](../../../docs/architecture/opentofu-contributor-runbook.md) before planning, importing, moving state, or applying.

## Phase 7 staged migration

Issue #1272 adds a `canadaeast` candidate alongside the imported
`australiaeast` estate. The first stage creates only the replacement web,
telemetry, SQL-server, Storage, and container resources. It does not create
the destination database, bind production hostnames, change Cloudflare DNS,
or alter an existing resource.

The SQL administrator password comes from the existing Bitwarden migration
connection-string secret. OpenTofu passes it through the AzureRM provider's
write-only field; it is ephemeral and cannot enter the plan or state.

Follow
[`production-region-migration.md`](../../../docs/architecture/production-region-migration.md)
for the phased apply, copy, verification, cutover, and retirement gates.
