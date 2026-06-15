# ADR 0005: Admin News Publishing With Microsoft Entra ID

## Status

Accepted.

## Context

ADR 0001 limited the first release to a read-only public site. Issue #5 adds a protected admin workflow so authorized editors can manage news without touching the database directly.

The legacy `NEWS_T` table remains the content store. Public routes already read published rows from `NEWS_T` using `DISPLAY = 1` and latest-row deduplication per `NEWS_ID`.

## Decision

Add an admin news publishing workflow that:

- Runs inside the existing `QueenZone.Web` App Service host.
- Authenticates admins with Microsoft Entra ID (Azure AD OpenID Connect).
- Authorizes only configured admin email addresses.
- Writes directly to legacy `NEWS_T` for create, edit, publish, unpublish, and hard delete.
- Stores optional custom slugs and editor metadata in new nullable columns on `NEWS_T`.
- Records publish, unpublish, edit, create, and delete actions in a separate `NewsAuditLog` table.
- Uses plain `<textarea>` input for article bodies. New admin content is stored as plain text.
- Scaffolds Entra configuration in appsettings with deployment documentation for Azure setup.

Admin article lifecycle:

| Action | `DISPLAY` | Public visibility |
| --- | --- | --- |
| Create draft | `0` | Hidden |
| Publish | `1` | Visible on `/news` and `/news/{id}/{slug}` |
| Unpublish | `0` | Hidden; record retained |
| Delete | row removed | Hidden |

Slug behavior:

- Default slug is generated from the title.
- Admins may override the slug before save.
- Stored slug wins on public canonical URLs when present.

## Consequences

Benefits:

- Reuses the existing public read pipeline and `NEWS_ID` URL namespace.
- Keeps admin and public traffic on one App Service.
- Entra ID provides managed authentication without local password storage.

Tradeoffs:

- `NEWS_T` gains nullable extension columns and a companion audit table.
- The deployed App Service identity needs scoped `db_datawriter` permission.
- Entra app registration and App Service settings are required before admin works in Azure.
- ADR 0001's "no admin editing" constraint is lifted for news only.

## Database Changes

Run `docs/sql/001-news-admin-columns.sql` or apply the EF Core migration in `src/QueenZone.Data/Migrations` before enabling admin writes. See ADR 0006 for the hybrid Dapper/EF Core data-access split.

## Security

- All admin routes and write endpoints require the `Admin` authorization policy.
- Anonymous users receive 401/403 responses from protected endpoints.
- Audit log entries include actor email and action metadata.
- Source URLs and plain-text bodies are validated before save.