# OpenTofu Azure data import

Issue: [#628](https://github.com/richardorchard/QueenZone.Modern/issues/628),
step 5 of epic [#615](https://github.com/richardorchard/QueenZone.Modern/issues/615).

## Managed boundary

The production root declares imports for the existing Azure SQL logical server,
Basic database, Azure-services firewall rule, disabled server/database auditing
settings, Storage account, Blob service, and 26 product containers.

OpenTofu records the existing SQL server administrator name because ARM requires
it, but does not manage its password. Database principals, schema, EF migrations,
tables, procedures, rows, blob objects, and the operator workstation firewall
rule remain outside the stack. No SQL/Storage diagnostic settings or stack-owned
RBAC assignments exist, so none are created.

## Secret-free Storage state

The AzureRM Storage account resource exports account keys and connection
strings even when configuration does not reference them. This violates the
stack's no-secrets-in-state boundary. Storage is therefore managed through
AzAPI ARM resources with empty response exports. No `listKeys` action exists in
the configuration; module outputs expose the Storage resource ID only.

## Recovery and retention

Azure SQL remains Basic 5 DTU, 2 GB, with locally redundant backup storage and
seven days of point-in-time restore retention. Differential backups run every
24 hours. Long-term retention is disabled. These are the current low-cost
recovery controls; #596 can assess stronger recovery separately.

Blob and container soft delete remain enabled for seven days. Blob versioning,
change feed, point-in-time restore, and lifecycle management remain disabled or
absent. The first import deliberately avoids new storage cost or retention.

## Container ACL decision

The imported ACLs match live product behaviour:

- `databasebackup`, `ugc-avatars`, and `ugc-forum` are private;
- archive/media containers retain public blob access;
- `css` retains public container access;
- `songfiles` and `attachments` remain public blob access pending #177;
- scratch `test` remains outside the stack;
- missing `ugc-photos` and `ugc-articles` are not created.

Changing the two legacy member-facing containers to private requires the
application/CDN work in #177. It is not safe as import cleanup.

## Verification contract

Keep plan files outside the repository and report resource actions only. A
valid first plan must show imports with no Azure property create, update,
replacement, deletion, or ACL change. AzAPI may report state-only updates to its
computed `output` after imports because response exports are deliberately empty.
Live checks must cover public CDN media, raw public ACL behaviour,
private-container denial, and read-only SQL connectivity.
