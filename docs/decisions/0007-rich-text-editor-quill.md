# ADR 0007: Quill Rich Text Editor For Shared Authoring

## Status

Accepted.

## Context

Forum posts, user-submitted articles, and news/editorial authoring will need rich text with clipboard image paste. The app is ASP.NET Core Razor Pages (server-rendered forms + antiforgery), not a SPA. Issue #226 added `IBlobUploadService` for durable image storage.

Candidates considered: Trix, Quill, ProseMirror/TipTap.

## Decision

Use **Quill** (vendored under `wwwroot`) as the shared rich text editor for authoring surfaces.

- Integrate via a Razor partial that syncs HTML into a hidden `<textarea>` for normal form POST.
- Clipboard/drag images upload through a minimal authenticated endpoint that uses `IBlobUploadService`.
- Always sanitize HTML on the server with HtmlSanitizer before persistence; never trust editor output.
- Constrain Quill formats to the server allowlist.

## Consequences

Benefits:

- No React/Vue requirement; works with Razor and a small init script.
- Clear path for paste-to-blob via a custom image handler.
- Version pinning via vendored assets (no mandatory npm app for the site).

Tradeoffs:

- Quill HTML is not perfectly semantic; sanitizer + format limits are required.
- Quill evolves more slowly than TipTap; acceptable for this host model.
- Admin news remains plain text per ADR 0005 until a separate feature opts that surface into rich HTML.
