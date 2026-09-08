import type { PersistedNewsShare } from './draftStore';
import { normalizeShareUrl, type ShareIntake } from './parseShare';

/** Hydrate discards a share slot when `now - savedAt` is greater than this. */
export const newsShareStaleWindowMs = 30 * 60 * 1000;

/**
 * Missing or unparseable `savedAt` is stale (one-time migration).
 * Exactly 30 minutes is still fresh; only greater than 30 minutes is discarded.
 */
export function isPersistedShareFresh(savedAt: string | undefined, nowMs: number): boolean {
  if (typeof savedAt !== 'string' || savedAt.length === 0) {
    return false;
  }

  const savedMs = Date.parse(savedAt);
  if (Number.isNaN(savedMs)) {
    return false;
  }

  return nowMs - savedMs <= newsShareStaleWindowMs;
}

export function shareSlotFingerprint(slot: PersistedNewsShare): string | null {
  if (slot.kind === 'form') {
    const url = slot.draft.url.trim();
    if (!url) {
      return null;
    }
    return normalizeShareUrl(url);
  }

  return slot.candidates.map((url) => normalizeShareUrl(url)).join('\n');
}

export function shareIntakeFingerprint(intake: ShareIntake): string | null {
  if (intake.kind === 'accepted') {
    return normalizeShareUrl(intake.url);
  }

  if (intake.kind === 'choose') {
    return intake.candidates.map((url) => normalizeShareUrl(url)).join('\n');
  }

  return null;
}
