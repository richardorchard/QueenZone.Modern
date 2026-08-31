/** Cookie-gated website path. Never load this in Image / WebView / Linking. */
export function isCookieGatedForumAttachmentPath(path: string | null | undefined): boolean {
  const trimmed = path?.trim() ?? '';
  if (!trimmed) {
    return false;
  }
  return (
    /\/forum\/attachment(?:\/|$)/i.test(trimmed) &&
    !/\/api\/v1\/forum\/attachments(?:\/|$)/i.test(trimmed)
  );
}
