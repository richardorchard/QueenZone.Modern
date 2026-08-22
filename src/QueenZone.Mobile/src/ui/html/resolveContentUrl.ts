/**
 * Resolve relative content URLs (e.g. `/ugc/news/...`) against the API origin
 * so native Image / Linking can load them.
 */
export function resolveContentUrl(
  href: string | null | undefined,
  baseOrigin: string,
): string | null {
  if (!href) {
    return null;
  }

  const trimmed = href.trim();
  if (!trimmed) {
    return null;
  }

  if (/^https?:\/\//i.test(trimmed)) {
    return trimmed;
  }

  if (trimmed.startsWith('//')) {
    return `https:${trimmed}`;
  }

  try {
    const base = new URL(baseOrigin);
    return new URL(trimmed, base.origin).toString();
  } catch {
    return null;
  }
}

/** True when the URL is safe to open in an external browser. */
export function isHttpUrl(href: string | null | undefined): boolean {
  if (!href) {
    return false;
  }
  try {
    const uri = new URL(href);
    return uri.protocol === 'http:' || uri.protocol === 'https:';
  } catch {
    return false;
  }
}
