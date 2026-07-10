# User-generated content blob storage

Shared upload infrastructure lives in `QueenZone.Storage`. Web (and future workers/tools) call `IBlobUploadService`; they do not construct `BlobServiceClient` directly.

## Configuration

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:BlobStorage` | Azure Storage connection string. Empty = app boots with `NullBlobUploadService` (uploads throw). |
| `BlobUpload:DefaultMaxBytes` | Default size limit (10 MB). |
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
- Prefer short-lived SAS and/or a Cloudflare Worker/CDN with controlled origin—same idea as `cdn.queenzone.org` for legacy media, but on separate UGC containers/routes.
- App-proxy every byte only when auth truly requires it.

## Security baseline

- Size and MIME allowlists (per container).
- Magic-byte sniffing where possible; extension must agree when both are known.
- Collision-resistant names.

Follow-ups (not in the foundation service): image re-encoding/EXIF strip, malware scanning, rate limits, authenticated upload UI endpoints.

## Testing

- Unit tests cover validation without Azure.
- Opt-in round-trip: set `ConnectionStrings:BlobStorage` and `RUN_BLOB_STORAGE_TESTS=true`.
