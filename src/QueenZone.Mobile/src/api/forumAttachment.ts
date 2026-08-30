import { Linking, Share } from 'react-native';
import { getAppConfig } from '../config';
import { resolveContentUrl } from '../ui/html/resolveContentUrl';
import { ApiError } from './client';

export type ForumAttachmentBytes = {
  finalUrl: string;
  contentType: string;
  dataUri: string;
};

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

/**
 * Bearer GET of `downloadUrl`. Refuses the cookie-gated `/forum/attachment/...` path.
 */
export async function fetchForumAttachment(
  downloadUrl: string,
  accessToken: string,
  signal?: AbortSignal,
): Promise<ForumAttachmentBytes> {
  if (isCookieGatedForumAttachmentPath(downloadUrl)) {
    throw ApiError.http(400, 'This attachment cannot be opened from the app.');
  }

  const url = resolveContentUrl(downloadUrl, getAppConfig().apiBaseUrl);
  if (!url) {
    throw ApiError.http(400, 'This attachment cannot be opened from the app.');
  }

  const response = await fetch(url, {
    method: 'GET',
    redirect: 'follow',
    headers: {
      Accept: '*/*',
      Authorization: `Bearer ${accessToken}`,
    },
    signal,
  });

  if (!response.ok) {
    throw ApiError.http(
      response.status,
      response.status === 401 ? 'Sign in to continue.' : `Request failed (${response.status}).`,
    );
  }

  const contentType = response.headers.get('content-type')?.split(';')[0]?.trim() || 'application/octet-stream';
  const bytes = new Uint8Array(await response.arrayBuffer());
  return {
    finalUrl: response.url,
    contentType,
    dataUri: `data:${contentType};base64,${bytesToBase64(bytes)}`,
  };
}

/**
 * Bearer GET an image (follows the legacy CDN redirect) and return a local
 * cached data URI for the in-app viewer. RN Image must not load `url`.
 */
export async function openForumAttachmentImage(
  downloadUrl: string,
  accessToken: string,
  signal?: AbortSignal,
): Promise<string> {
  const cached = imageCache.get(downloadUrl);
  if (cached) {
    return cached;
  }

  const fetched = await fetchForumAttachment(downloadUrl, accessToken, signal);
  if (isCookieGatedForumAttachmentPath(fetched.finalUrl)) {
    throw ApiError.http(400, 'This attachment cannot be opened from the app.');
  }

  imageCache.set(downloadUrl, fetched.dataUri);
  return fetched.dataUri;
}

export type OpenForumAttachmentFileOptions = {
  signal?: AbortSignal;
  /** When false, skip the OEM share / Linking sheet after a successful Bearer fetch. */
  present?: boolean;
};

/** Open a non-image after a Bearer fetch. Follows a public CDN redirect; otherwise shares the bytes. */
export async function openForumAttachmentFile(
  downloadUrl: string,
  accessToken: string,
  fileName: string,
  options?: OpenForumAttachmentFileOptions,
): Promise<void> {
  const present = options?.present ?? true;
  const fetched = await fetchForumAttachment(downloadUrl, accessToken, options?.signal);
  if (
    fetched.finalUrl &&
    !isCookieGatedForumAttachmentPath(fetched.finalUrl) &&
    /^https?:\/\//i.test(fetched.finalUrl) &&
    !/\/api\/v1\/forum\/attachments(?:\/|$)/i.test(fetched.finalUrl)
  ) {
    if (present) {
      await Linking.openURL(fetched.finalUrl);
    }
    return;
  }

  if (!present) {
    return;
  }

  await Share.share({
    title: fileName,
    message: fileName,
    url: fetched.dataUri,
  });
}

const imageCache = new Map<string, string>();

function bytesToBase64(bytes: Uint8Array): string {
  let binary = '';
  for (let i = 0; i < bytes.length; i += 1) {
    binary += String.fromCharCode(bytes[i]!);
  }
  return globalThis.btoa(binary);
}
