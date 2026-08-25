# Versioned JSON API (`/api/v1`)

Contract for the mobile app and any future JSON clients. Razor Pages remain the website UI. Unversioned narrow endpoints in `src/QueenZone.Web/Endpoints/` (RSS, editor image upload, audio/file streaming) stay as-is and are **not** part of this surface. Fan-performance audio has a JWT alias under `/api/v1/content/fan-performances/{id}/audio` that reuses the same `ServeAudioAsync` implementation as the cookie-gated website path.

Decision record: [`docs/decisions/0010-versioned-json-api-conventions.md`](../decisions/0010-versioned-json-api-conventions.md).

## Area

| Path | Role |
| --- | --- |
| `/api/v1` | Discovery document (version, OpenAPI URL, conventions) |
| `/openapi/v1.json` | Generated OpenAPI 3.1 document (runtime, kept in sync with mapped `/api/v1` endpoints) |
| `/api/v1/auth/*` | Mobile OAuth2 PKCE + tokens (see issues #720 / #721). Public `GET /api/v1/auth/providers` lists configured Google/Microsoft/Discord/GitHub/Apple buttons matching `/account/login`. |
| `/api/v1/admin` | Admin status probe; future admin JSON must use the same `Admin` policy (#723) |
| `/api/v1/content/*` | Public, read-only archive content for the mobile app: news, biography, discography, timeline, Freddie Tribute (#726), photo galleries (#743), and fan-performance listings (#747 / #748 / #750). No authentication required for those reads. `GET /api/v1/content/news` accepts optional `decade` (e.g. `2010`) to filter server-side to that 10-year span (floored to the decade start; out-of-range years are ignored). Photo image URLs are `cdn.queenzone.org` via `PhotoImageUrl` (not App Service). Category list/detail/items reuse `PublicQueryCacheService` photo helpers (same as Razor). Category items default and clamp `pageSize` to `PhotoRoutes.CategoryPageSize` (24) so pages match `/photography/{slug}`. Photo items include `detailPath` and `categoryPath`. Fan-performance items include `durationSeconds` (MPEG metadata when the songfile is readable) and `audioPath`. Streaming `GET /api/v1/content/fan-performances/{id}/audio` requires `MobileMemberPolicy` and reuses `FanPerformanceEndpoints.ServeAudioAsync` (private `songfiles` blob, HTTP range processing). The website cookie path `/fan-performances/{id}/audio` is unchanged. |
| `/api/v1/search` | Public whole-site search over the shared `SearchDocument` index (`ISiteSearchService` / `dbo.SearchDocument_Search`). Same visibility as website `/search`. Query `q` plus optional `type` (`SiteSearchContentType.Normalize`; unknown values search all types). Empty or whitespace `q` returns `200` with an empty page (not `400`). Default `pageSize` is 20 (`SearchModel.PageSize`); max 100. Items include `sourceKey` and an optional numeric `id` parsed from that key. Summaries are plain text (no `<mark>` HTML). Rate limit: `QueenZoneRateLimitPolicies.Search`. |
| `/api/v1/forum/*` | Public forum browse for the mobile app: category list, category detail, paged topic lists (#731), topic headers plus paged posts (#732), and topic polls (#734). Same `IForumRepository` visibility as `/forum` Razor Pages. Reads require no authentication. Topic posts default and clamp `pageSize` to `ForumRoutes.PostsPageSize` (15) so pages match the website. Topic headers include `isLocked` (same source as write 403 TopicLocked). Authenticated writes (#733): `POST /api/v1/forum/categories/{id}/topics` and `POST /api/v1/forum/topics/{id}/posts` require `MobileMemberPolicy` and reuse `ForumPostWriteService` (the same sanitization, attachment rules, and `ForumPostRateLimiter` as the website). Reply 201 Location matches `detailPath` (website topic URL + `#post-{id}`), not the posts collection. `GET /api/v1/forum/topics/{id}/poll` is public; an optional Bearer token fills viewer flags (`canViewerVote`, `viewerHasVoted`, `selectedByViewer`). Authenticated vote/close (`POST .../poll/vote`, `POST .../poll/close`) require `MobileMemberPolicy` and reuse `IForumPollRepository` plus `ForumPollVoteMapper` (same one-vote-per-member and closed rules as `/forum/poll/{id}/vote`). Attachment metadata includes `/forum/attachment/*` paths; those remain cookie-gated and are not opened from the app. |
| `/api/v1/contact` | Public contact form for the mobile app (#755). Same admin inbox as website `/contact`. Optional mobile JWT; guests send name and email. |
| `/api/v1/me` | Signed-in member account for the mobile app (#752 / #753 / #754). `GET`/`PATCH` profile (display name, messaging privacy, legacy claim, field limits). `POST`/`DELETE /me/avatar` uses `MemberAccountService` (same 2 MB JPEG/PNG/WebP crop as `/account/settings`). `POST /me/deletion-request` matches `/account/delete` (type `DELETE`, 30-day cooling-off, refresh-token revocation). Requires `MobileMemberPolicy`. Avatar bytes remain at `/account/avatar/{id}` (anonymous). |
| `/api/v1/me/messages` | Signed-in member inbox for the mobile app (#737 / #738 / #739). `GET` is a paged conversation list with the same unread counts as website `/messages`. Default and clamp `pageSize` to `PrivateMessageLimits.InboxPageSize` (50). `GET /me/messages/unread-count` is the masthead badge (`CountUnreadConversationsAsync`). `GET /me/messages/recipients?q=` is display-name recipient search matching website compose (`SearchRecipientsAsync`; cap 20; privacy/block filters apply on send, not search). `POST /me/messages` starts or continues a conversation through `PrivateMessageService.ComposeAsync` (same privacy, block, and SortKey rules as website `POST /messages/compose`); success is `201` with the latest page and Location `/messages/{id}`. `GET /me/messages/{conversationId}` opens a thread and marks it read the same way as `GET /messages/{id}` (omit `page` for the latest page). Message items include `reportedByViewer`. Message bodies are plain text (no HTML, markdown, or auto-linkification); clients must render them as text. `POST /me/messages/{conversationId}` sends a reply through `PrivateMessageService.ReplyAsync` (same SortKey / conversation-lock path as website `POST /messages/{id}`); success is `201` with the latest page and Location `/messages/{id}`. `POST /me/messages/{conversationId}/messages/{messageId}/report` reports an abusive message (#469) through `PrivateMessageService.ReportMessageAsync` (participant-only, optional reason up to 1000 characters, snapshot of the message plus up to two preceding messages for moderator review). Success is `201` with `{ reportId, alreadyReported }`; repeating the same report is idempotent `200`. The reported member is not notified. Empty/oversized bodies are `400`; blocked or privacy-disallowed sends are `403` (`Unable to send message.`); non-participants are `404`; the shared private-message rate limiter is `429`. Requires `MobileMemberPolicy`. Archive stays on later story (#741). |
| `/api/v1/me/submissions/*` | Signed-in member photo, news-suggestion, and article submission status (#745). Same status model as website `/account/my-submissions`. Requires `MobileMemberPolicy`. Admin review changes appear on the next refresh. |
| `/api/v1/me/notification-preferences` | Per-member notification category toggles for the mobile app (#758). `GET` and `PATCH`. Defaults are `forumReply` true, `privateMessage` true, and `news` false. `PATCH` is partial. Omitted fields stay unchanged. Persistence is per member, not per device. A stored row is an explicit choice, including a choice that matches today's default. `forumReply` is a master mute. Forum reply pushes also require Watching the topic (#735). Requires `MobileMemberPolicy`. Dispatch is #759. Settings UI is #852. |
| `/api/v1/member/*` | Authenticated member writes. `POST /api/v1/member/photo-submissions` (#746, mobile client #744) requires `MobileMemberPolicy` and delegates to `PhotoSubmissionService.SubmitAsync` — the same `ugc-photos` blob path, admin review queue, and `MemberUploadQuotaService` bucket as `/submit/photo`. Multipart form (`title` / `Title` plus `photo` / `PhotoFile`). Success is `201` with submission id, `Pending` status, and Location `/api/v1/member/photo-submissions/{id}`. Quota and disabled-upload messages from the service are Problem Details `429`; validation stays `400`. Not under public `/api/v1/content/photos*`. |
| `/api/v1/notifications/devices` | Push device token register/unregister for the mobile app (#757). `POST` upserts by client `deviceId` (idempotent; re-registering rotates the token and, if the device changed member, reassigns ownership). `DELETE /devices/{deviceId}` unregisters (sign-out, permission revoked, or settings toggle); `404` if the id doesn't belong to the caller. Tagged with provider (`apns`/`fcm`). Requires `MobileMemberPolicy`. Dispatch (#759) and delivery-failure cleanup (#760) are separate stories; see [ADR 0014](../decisions/0014-push-notification-transport-and-dispatch.md). |
| `/api/v1/{resource}` | Later epics |

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
| `pageSize` | `20` | Values below 1 clamp to 20; values above 100 clamp to 100. **Exceptions:** `GET /api/v1/forum/topics/{id}/posts` defaults and clamps to `ForumRoutes.PostsPageSize` (15) so pages match `/forum/topic/...`. `GET /api/v1/content/photos/categories/{slug}/items` defaults and clamps to `PhotoRoutes.CategoryPageSize` (24) so pages match `/photography/{slug}`. `GET /api/v1/me/messages` defaults and clamps to `PrivateMessageLimits.InboxPageSize` (50) so pages match `/messages`. `GET /api/v1/me/messages/{conversationId}` defaults and clamps to `PrivateMessageLimits.ConversationPageSize` (50); omit `page` to match the website latest-page default. |

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

Public, unauthenticated `/api/v1` routes are included in the live-site read-only sweep (`LiveSiteContentApiTests`): discovery, OpenAPI, content and forum list/detail *shape*, photo category / items / detail plus CDN image hosts, optional poll GET when `hasPoll` is true, and Problem Details 404. That fixture is `RealData` + `ReadOnly`, so it also runs against the SQL Express mirror in the nightly RealData suite. `/api/v1/auth` and `/api/v1/admin` are not part of the sweep (token grants / rate limits, and Entra). Post-deploy smoke hits `GET /api/v1` and `GET /api/v1/content/news?pageSize=1`. In-memory contract tests live in `QueenZone.Web.Tests` (`ApiV1RoutesTests`, `ContentApi*Tests`, `SearchApiTests`, `ForumApiTests`, `ForumApiPollTests`, `MemberPhotoSubmissionApiTests`, `MeApiTests`, `MessagesApiTests`, `SubmissionsApiTests`, `DevicesApiTests`, `NotificationPreferencesApiTests`).
