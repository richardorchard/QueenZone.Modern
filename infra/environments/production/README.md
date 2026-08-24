# QueenZone production OpenTofu root

This root module is the only production entry point. The resource group,
Azure web/telemetry estate, Azure data estate, and Cloudflare edge estate all
use declarative import blocks from #622, #628, and #626.

The first remote plan must show imports and no unexplained change, replacement,
or deletion. Do not apply from a local operator session. The protected
`opentofu-apply` environment remains the only apply path.

Use [`scripts/Test-OpenTofu.ps1`](../../../scripts/Test-OpenTofu.ps1) for local validation. See [`docs/architecture/opentofu-contributor-runbook.md`](../../../docs/architecture/opentofu-contributor-runbook.md) before planning, importing, moving state, or applying.
