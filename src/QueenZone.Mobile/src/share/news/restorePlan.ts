export type ShareRestorePlan = 'wait' | 'noop' | 'openCaptured' | 'hydrateThenOpen';

/**
 * After session restore, hydrate from disk unless a share just landed in memory.
 * Hydrate clears ephemeral rejects, so a file/http share during restore must
 * open the captured view instead of reading the empty slot.
 */
export function planShareRestore(input: {
  isRestoring: boolean;
  pendingOpen: boolean;
  didHydrate: boolean;
  finishedRestore: boolean;
}): ShareRestorePlan {
  if (input.isRestoring) {
    return 'wait';
  }

  if (input.pendingOpen) {
    return 'openCaptured';
  }

  if (input.finishedRestore || !input.didHydrate) {
    return 'hydrateThenOpen';
  }

  return 'noop';
}
