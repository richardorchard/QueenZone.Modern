# Azure data module

Issue #628 will declare and import the existing SQL server/database, storage account, blob protection settings, and agreed containers here. Database schema and content remain under EF and operational tooling.

The SQL server, database, storage account, and durable containers must include `lifecycle { prevent_destroy = true }`. Preserve live access and public-container settings during import; do not enable new defaults.
