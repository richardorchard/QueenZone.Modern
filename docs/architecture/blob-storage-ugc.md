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
- Prefer short-lived SAS and/or a Cloudflare CDN with controlled origin—same idea as `cdn.queenzone.org` for legacy media (straight Cloudflare proxy to Azure Blob), but on separate UGC containers/routes.
- **Editor / forum pasted images** are served through the web app proxy: `/ugc/{area}/{blobName}` (e.g. `/ugc/forum/editors/…/id.webp`). Optional `?size=thumb` loads the paired `-thumb.webp` blob (thumb max 600 px longest side; full max 1200 px).
- App-proxy every byte only when auth truly requires it; private UGC containers use the proxy by default for HTML embeds.

## Security baseline

- Size and MIME allowlists (per container).
- Magic-byte sniffing where possible; extension must agree when both are known.
- Collision-resistant names.
- Forum / UGC HTML (`UgcHtml`) allows `<img>` only when `src` is an app proxy path (`/ugc/…`) or a configured UGC host / known `ugc-*` Azure container path. Arbitrary external image hosts are stripped.

Follow-ups (not in the foundation service): malware scanning, orphaned-blob cleanup after soft-delete, broader CDN for UGC.

## Testing

- Unit tests cover validation without Azure.
- Opt-in round-trip: set `ConnectionStrings:BlobStorage` and `RUN_BLOB_STORAGE_TESTS=true`.
