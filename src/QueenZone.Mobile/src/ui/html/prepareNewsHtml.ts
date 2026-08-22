/**
 * Client-side prep for news HTML before native rendering.
 * Server already sanitizes via NewsArticleContent.FormatBody; this strips a few
 * extra tags Quill/legacy markup might still emit so they degrade to text
 * instead of crashing the renderer.
 */

const UNSUPPORTED_BLOCK =
  /<\/?(?:iframe|script|object|embed|form|input|button|video|audio|svg|math)(?:\s[^>]*)?>/gi;

export function prepareNewsHtml(html: string | null | undefined): string {
  if (!html) {
    return '';
  }

  let prepared = html;
  let previous: string;
  do {
    previous = prepared;
    prepared = prepared.replace(UNSUPPORTED_BLOCK, '');
  } while (prepared !== previous);

  return prepared.trim();
}
