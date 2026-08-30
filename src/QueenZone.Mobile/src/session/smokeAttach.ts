/**
 * Debug-only forum attach inject (#1072). Same gate as `queenzone://smoke-auth`:
 * `__DEV__` plus `appEnv === 'development'`. Release / staging / production
 * fail closed. The OEM Files sheet is not the journey gate.
 */

/** Same shape as `SmokeAuthGate` in smokeAuth.ts — no runtime import (node:test). */
type SmokeAttachGate = {
  dev?: boolean;
  appEnv?: string;
};

export const smokeAttachHost = 'smoke-attach';
export const smokeAttachFileName = 'attach.txt';
export const smokeAttachMimeType = 'text/plain';
export const smokeAttachDefaultAndroidUri =
  'file:///sdcard/Android/data/org.queenzone.mobile/files/attach.txt';

export type SmokeAttachAsset = {
  uri: string;
  name: string;
  mimeType: string;
};

let pending: SmokeAttachAsset | null = null;

export function isSmokeAttachEnabled(env: SmokeAttachGate = {}): boolean {
  const dev = env.dev ?? (typeof __DEV__ !== 'undefined' ? __DEV__ : false);
  return dev === true && env.appEnv === 'development';
}

export function parseSmokeAttachAsset(url: string): SmokeAttachAsset | null {
  const parsed = tryParseUrl(url);
  if (!parsed) {
    return null;
  }

  const hostOrPath = parsed.hostname || parsed.host;
  const path = parsed.pathname.replace(/^\/+/, '');
  const isAttach =
    hostOrPath === smokeAttachHost || path === smokeAttachHost || parsed.pathname === `/${smokeAttachHost}`;
  if (parsed.protocol !== 'queenzone:' || !isAttach) {
    return null;
  }

  const uri = parsed.searchParams.get('uri')?.trim() ?? '';
  if (!uri) {
    return null;
  }

  const name = parsed.searchParams.get('name')?.trim() || fileNameFromUri(uri) || smokeAttachFileName;
  const mimeType = parsed.searchParams.get('type')?.trim() || smokeAttachMimeType;
  return { uri, name, mimeType };
}

export function buildSmokeAttachUrl(
  uri: string,
  extras: { name?: string; type?: string } = {},
): string {
  const trimmed = uri.trim();
  if (!trimmed) {
    throw new Error('Smoke attach URL requires a non-empty file URI.');
  }

  const params = new URLSearchParams({ uri: trimmed });
  const name = extras.name?.trim();
  const type = extras.type?.trim();
  if (name) {
    params.set('name', name);
  }
  if (type) {
    params.set('type', type);
  }
  return `queenzone://${smokeAttachHost}?${params.toString()}`;
}

export function defaultSmokeAttachAsset(platform: string): SmokeAttachAsset {
  if (platform === 'android') {
    return {
      uri: smokeAttachDefaultAndroidUri,
      name: smokeAttachFileName,
      mimeType: smokeAttachMimeType,
    };
  }

  return {
    uri: `file:///Documents/${smokeAttachFileName}`,
    name: smokeAttachFileName,
    mimeType: smokeAttachMimeType,
  };
}

export function stashSmokeAttachAsset(asset: SmokeAttachAsset): void {
  pending = asset;
}

export function takePendingSmokeAttachAsset(): SmokeAttachAsset | null {
  const next = pending;
  pending = null;
  return next;
}

export function peekPendingSmokeAttachAsset(): SmokeAttachAsset | null {
  return pending;
}

export function resetSmokeAttachPending(): void {
  pending = null;
}

function fileNameFromUri(uri: string): string {
  const trimmed = uri.trim();
  const query = trimmed.indexOf('?');
  const path = query >= 0 ? trimmed.slice(0, query) : trimmed;
  const slash = Math.max(path.lastIndexOf('/'), path.lastIndexOf('\\'));
  return slash >= 0 ? path.slice(slash + 1) : path;
}

function tryParseUrl(url: string): URL | null {
  try {
    return new URL(url);
  } catch {
    return null;
  }
}
