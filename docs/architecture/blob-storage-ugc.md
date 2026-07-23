# User-generated content blob storage

Shared upload infrastructure lives in `QueenZone.Storage`. Web (and future workers/tools) call `IBlobUploadService`; they do not construct `BlobServiceClient` directly.

## Configuration

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:BlobStorage` | Azure Storage connection string. Empty = app boots with `NullBlobUploadService` (uploads throw). |
| `BlobUpload:DefaultMaxBytes` | Default size limit (10 MB). |
| `BlobUpload:EditorMaxBytes` | Max size for rich-text editor uploads (default 10 MB). Effective limit is `min(EditorMaxBytes, container MaxBytes)`. |
| `BlobUpload:DefaultAllowedContentTypes` | Default MIME allowlist. |
| `BlobUpload:Containers` | Per-container max size / MIME overrides. |
| `BlobUpload:PublicBaseUrl` | Optional CDN/worker base for display URLs. |
| `UploadQuotas:Enabled` | Process-local daily quotas (default true). |
| `UploadQuotas:MaxUploadsPerDay` | Max upload operations per principal per UTC day (default 50). |
| `UploadQuotas:MaxBytesPerDay` | Max total bytes per principal per UTC day (default 100 MiB). |

Per-member quotas (`MemberUploadQuotaService`) apply to editor images, forum attachments, avatars, and photo submissions. They are **process-local** (`IMemoryCache`) and fit single-instance B1 hosting; container MIME/size allowlists remain enforced separately. Antivirus scanning of uploads is **not planned**.

Register in DI:

```csharp
services.AddQueenZoneStorage(configuration);
```

## Containers

Keep UGC separate from the legacy photo archive behind `cdn.queenzone.org`.

| Container | Use |
| --- | --- |
| `ugc-avatars` | Member profile images |
| `ugc-forum` | Forum attachments / pasted images |
| `ugc-photos` | Photo submissions |
| `ugc-articles` | News/article rich-text images |

Containers are created with **no public access** (`PublicAccessType.None`). Do not put new UGC into legacy gallery folders.

## Naming

- Member: `members/{memberId}/{guid}.{ext}`
- Editorial: `editors/{sanitizedEmail}/{guid}.{ext}`
- Fallback: `anonymous/{guid}.{ext}`

Store **container + blob name** in the database. Treat any public/display URL as optional and changeable (SAS or CDN).

## Serve strategy

- Storage is not anonymously listable.
- Do not use permanent raw `*.blob.core.windows.net` URLs as the product contract.
- Prefer short-lived SAS and/or a Cloudflare proxy with controlled origin, on separate UGC containers/routes.
- **Editor / forum pasted images** are served through the web app proxy: `/ugc/{area}/{blobName}` (e.g. `/ugc/forum/editors/…/id.webp`). Optional `?size=thumb` loads the paired `-thumb.webp` blob (thumb max 600 px longest side; full max 1200 px).
- App-proxy every byte only when auth truly requires it; private UGC containers use the proxy by default for HTML embeds.

### Legacy media CDN hostnames

Two Cloudflare hostnames proxy the legacy Azure Blob containers. They behave differently and are not interchangeable:

- **`cdn.queenzone.org`** — straight CDN proxy, no Worker. Cannot set response headers. Used by `PhotoImageUrl` for photos and images.
- **`cdn2.queenzone.org`** — Cloudflare Worker proxy. Can set `Content-Disposition` and other response headers. Used by `SongFileUrl` for fan performance audio so the browser download filename is consistent. Also used as the redirect target for **legacy forum attachments** after a member-auth check (`/forum/attachment/legacy/{postId}` → `https://cdn2.queenzone.org/attachments/{fileName}`).

Do not route audio through `cdn.queenzone.org`; it silently breaks the download filename without any test failure.

### Forum attachments

| Kind | Storage | Public HTML link | Download behaviour |
| --- | --- | --- | --- |
| Legacy import (`ModernForumPost.Attachment`) | Historical `attachments` blob container | `/forum/attachment/legacy/{legacyPostId}` | Member policy required; redirect to `cdn2.queenzone.org/attachments/…` |
| New uploads (`ForumPostAttachments`) | Private `ugc-forum` container | `/forum/attachment/{legacyPostId}/{attachmentId}` | Member policy required; stream via app with `Content-Disposition: attachment` and increment `DownloadCount` |

Do not link forum attachments straight to `cdn.queenzone.org` or raw Azure blob URLs in HTML. Inline editor images remain on `/ugc/forum/…` (see serve strategy above).

## Security baseline

- Size and MIME allowlists (per container).
- Magic-byte sniffing where possible; extension must agree when both are known.
- Collision-resistant names.
- Forum / UGC HTML (`UgcHtml`) allows `<img>` only when `src` is an app proxy path (`/ugc/…`) or a configured UGC host / known `ugc-*` Azure container path. Arbitrary external image hosts are stripped.

Follow-ups (not in the foundation service): malware scanning, orphaned-blob cleanup after soft-delete, broader CDN for UGC.

## Testing

- Unit tests cover validation without Azure.
- Opt-in round-trip: set `ConnectionStrings:BlobStorage` and `RUN_BLOB_STORAGE_TESTS=true`.
