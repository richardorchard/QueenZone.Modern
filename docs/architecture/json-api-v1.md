# Versioned JSON API (`/api/v1`)

Contract for the mobile app and any future JSON clients. Razor Pages remain the website UI. Unversioned narrow endpoints in `src/QueenZone.Web/Endpoints/` (RSS, editor image upload, audio/file streaming) stay as-is and are **not** part of this surface.

Decision record: [`docs/decisions/0010-versioned-json-api-conventions.md`](../decisions/0010-versioned-json-api-conventions.md).

## Area

| Path | Role |
| --- | --- |
| `/api/v1` | Discovery document (version, OpenAPI URL, conventions) |
| `/openapi/v1.json` | Generated OpenAPI 3.1 document (runtime, kept in sync with mapped `/api/v1` endpoints) |
| `/api/v1/auth/*` | Mobile OAuth2 PKCE + tokens (see issues #720 / #721) |
| `/api/v1/admin` | Admin status probe; future admin JSON must use the same `Admin` policy (#723) |
| `/api/v1/content/*` | Public, read-only archive content for the mobile app: news, biography, discography, timeline, and Freddie Tribute (#726). No authentication required. |
| `/api/v1/forum/*` | Public forum browse for the mobile app: category list, category detail, paged topic lists (#731), and topic headers plus paged posts (#732). Same `IForumRepository` visibility as `/forum` Razor Pages. Reads require no authentication. Topic posts default and clamp `pageSize` to `ForumRoutes.PostsPageSize` (15) so pages match the website. Topic headers include `isLocked` (same source as write 403 TopicLocked). Authenticated writes (#733): `POST /api/v1/forum/categories/{id}/topics` and `POST /api/v1/forum/topics/{id}/posts` require `MobileMemberPolicy` and reuse `ForumPostWriteService` (the same sanitization, attachment rules, and `ForumPostRateLimiter` as the website). Reply 201 Location matches `detailPath` (website topic URL + `#post-{id}`), not the posts collection. Attachment metadata includes `/forum/attachment/*` paths; those remain cookie-gated and are not opened from the app. Polls are #734. |
| `/api/v1/{resource}` | Later epics (messages, galleries, …) |

Later endpoints should be mapped on a `MapGroup("/api/v1")` (or a sub-group) with `.WithGroupName("v1")` so they appear in the OpenAPI document. Do not add mobile/app JSON routes under `src/QueenZone.Web/Endpoints/`.

## Versioning

- URL path versioning: `/api/v1`, then `/api/v2` if a breaking change is required.
- Within v1, changes must be additive: new optional fields, new endpoints, new optional query parameters.
- Do not rename, remove, or change the meaning of existing JSON fields in v1.
- A new OpenAPI document name (`v2`) is required alongside `/api/v2`.

## JSON

Configured in `AddQueenZoneJsonApi()`:

- Property names: **camelCase**
- Timestamps: **ISO-8601 UTC**
- Enums: **strings** (camelCase)

OAuth2 token success payloads keep RFC 6749 names (`access_token`, `refresh_token`, `token_type`, `expires_in`).

## Errors

Unhandled `/api/v1` failures and empty error statuses use RFC 7807 Problem Details (`application/problem+json`):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "No API resource matches '/api/v1/missing'."
}
```

HTML `/error` pages are not used for `/api/v1`. Unknown `/api/v1/...` paths return Problem Details `404`, not the public Not Found Razor page.

**Auth exception:** `/api/v1/auth/token`, `/authorize`, and `/callback` error objects stay RFC 6749 `{ "error", "error_description" }` (and redirects for the browser hop). Do not convert those to Problem Details. Rate-limit rejections on those paths use `429` with `error: temporarily_unavailable` (never Problem Details, and never echo tokens).

Sign-in and token routes are process-local rate limited: per client IP (same `RateLimiting:Auth` policy as website `/account/login`) plus a per-member cap on callback completion and refresh grants. See [`hosting-scale-and-cache.md`](hosting-scale-and-cache.md).

## Pagination

Use `ApiPagination.Normalize` and return `ApiPagedResponse<T>` from list endpoints.

| Query | Default | Rules |
| --- | --- | --- |
| `page` | `1` | Values below 1 clamp to 1 |
| `pageSize` | `20` | Values below 1 clamp to 20; values above 100 clamp to 100. **Exception:** `GET /api/v1/forum/topics/{id}/posts` defaults and clamps to `ForumRoutes.PostsPageSize` (15) so pages match `/forum/topic/...`. |

Response:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0
}
```

## Authentication

Member routes use the existing mobile JWT bearer scheme (`MemberAuthenticationSchemes.MobileMemberPolicy`). Admin-capable API routes live under `/api/v1/admin` and must be mapped with `MapAdminApiGroup()` so they use the same `Admin` authorization policy as `/admin` Razor pages: the Entra/test admin composite scheme plus `Admin:AllowedEmails`. A member mobile access token is never treated as admin, even when its email is on the allowlist. Failed admin-API authorization returns Problem Details `401`/`403` rather than an Entra redirect.

`/api/v1` is cookie-antiforgery-free (`DisableAntiforgery` on the API groups). Do not require `__RequestVerificationToken` on JSON API calls.

## OpenAPI

`GET /openapi/v1.json` is generated from endpoint metadata at runtime. Only endpoints with group name `v1` are included, so Razor Pages, `/health`, and `/api/uploads/editor-image` stay out of the spec.

The discovery document (`GET /api/v1`) points at that URL so the React Native client and backend share one contract.

## Production and nightly checks

Public, unauthenticated `/api/v1` routes are included in the live-site read-only sweep (`LiveSiteContentApiTests`): discovery, OpenAPI, content and forum list/detail *shape*, and Problem Details 404. That fixture is `RealData` + `ReadOnly`, so it also runs against the SQL Express mirror in the nightly RealData suite. `/api/v1/auth` and `/api/v1/admin` are not part of the sweep (token grants / rate limits, and Entra). Post-deploy smoke hits `GET /api/v1` and `GET /api/v1/content/news?pageSize=1`. In-memory contract tests live in `QueenZone.Web.Tests` (`ApiV1RoutesTests`, `ContentApi*Tests`, `ForumApiTests`).
