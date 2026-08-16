# Azure data module

This module owns the imported Azure SQL server/database, the Azure-services
firewall rule, disabled auditing settings, Storage account, Blob protection
settings, and approved containers. ARM requires the existing SQL administrator
name in the server resource, but its password remains external. Database schema,
SQL principals, connection strings, and blob objects remain outside OpenTofu.

Storage uses AzAPI so the provider never calls `listKeys` or exports generated
account keys and connection strings into state. The resources export IDs only.

The SQL database remains Basic 5 DTU with a 2 GB limit, 7-day LRS short-term
retention, and no long-term retention. The personal workstation firewall rule
is outside this stack. No diagnostic settings or stack-owned RBAC assignments
exist, so none are invented.

Blob and container soft delete remain seven days. Versioning, change feed,
point-in-time restore, and lifecycle rules remain disabled or absent. This is a
cost-neutral first import, not a protection-policy expansion.

Container ACLs match the live estate. `databasebackup`, `ugc-avatars`, and
`ugc-forum` stay private. Public `songfiles` and `attachments` remain public
until #177 changes the product path. The scratch `test` container and missing
future `ugc-photos`/`ugc-articles` containers are not managed.
