import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  isSessionRestoreTimeoutError,
  sessionRestoreTimeoutLabel,
  withTimeout,
} from './restoreTimeout.ts';

describe('withTimeout', () => {
  it('resolves when the inner promise wins', async () => {
    await assert.doesNotReject(() => withTimeout(Promise.resolve('ok'), 50, sessionRestoreTimeoutLabel));
    assert.equal(await withTimeout(Promise.resolve('ok'), 50, sessionRestoreTimeoutLabel), 'ok');
  });

  it('rejects with the timeout label when the inner promise never settles', async () => {
    const hung = new Promise<never>(() => {});
    await assert.rejects(
      () => withTimeout(hung, 10, sessionRestoreTimeoutLabel),
      (error: unknown) => {
        assert.equal(isSessionRestoreTimeoutError(error), true);
        return true;
      },
    );
  });

  it('does not treat a generic error as a restore timeout', () => {
    assert.equal(isSessionRestoreTimeoutError(new Error('nope')), false);
    assert.equal(isSessionRestoreTimeoutError(sessionRestoreTimeoutLabel), false);
  });
});
