# QueenZone production OpenTofu root

This root module is the only production entry point. It has no managed resources yet. Issues #622, #628, and #626 will instantiate the modules and import existing resources; they must not create replacements.

Use [`scripts/Test-OpenTofu.ps1`](../../../scripts/Test-OpenTofu.ps1) for local validation. See [`docs/architecture/opentofu-contributor-runbook.md`](../../../docs/architecture/opentofu-contributor-runbook.md) before planning, importing, moving state, or applying.
