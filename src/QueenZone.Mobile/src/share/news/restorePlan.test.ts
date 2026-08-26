import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { planShareRestore } from './restorePlan.ts';

describe('planShareRestore', () => {
  it('waits while the session is restoring', () => {
    assert.equal(
      planShareRestore({
        isRestoring: true,
        pendingOpen: true,
        didHydrate: false,
        finishedRestore: false,
      }),
      'wait',
    );
  });

  it('opens a share captured during restore without hydrating', () => {
    assert.equal(
      planShareRestore({
        isRestoring: false,
        pendingOpen: true,
        didHydrate: false,
        finishedRestore: true,
      }),
      'openCaptured',
    );
  });

  it('hydrates once on a cold start with no pending share', () => {
    assert.equal(
      planShareRestore({
        isRestoring: false,
        pendingOpen: false,
        didHydrate: false,
        finishedRestore: false,
      }),
      'hydrateThenOpen',
    );
  });

  it('hydrates after OAuth restore so the flushed draft returns', () => {
    assert.equal(
      planShareRestore({
        isRestoring: false,
        pendingOpen: false,
        didHydrate: true,
        finishedRestore: true,
      }),
      'hydrateThenOpen',
    );
  });

  it('does nothing on later ticks after hydrate', () => {
    assert.equal(
      planShareRestore({
        isRestoring: false,
        pendingOpen: false,
        didHydrate: true,
        finishedRestore: false,
      }),
      'noop',
    );
  });
});
