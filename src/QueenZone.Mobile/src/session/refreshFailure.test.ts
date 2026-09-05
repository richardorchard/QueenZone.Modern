import assert from 'node:assert/strict';
import { register } from 'node:module';
import { describe, it } from 'node:test';
import { pathToFileURL } from 'node:url';

register(
  `data:text/javascript,${encodeURIComponent(`
    export async function resolve(specifier, context, nextResolve) {
      if (specifier.startsWith('.') && !/\\\\.(?:[cm]?[jt]s|json)$/.test(specifier)) {
        try {
          return await nextResolve(specifier + '.ts', context);
        } catch {
          return nextResolve(specifier, context);
        }
      }
      return nextResolve(specifier, context);
    }
  `)}`,
  pathToFileURL('./'),
);

const { ApiError } = await import('../api/errors.ts');
const { isDefiniteAuthRefreshFailure, isTransientRefreshFailure } = await import('./refreshFailure.ts');

describe('refresh failure classification', () => {
  it('treats 401 and invalid_grant as definite auth failure', () => {
    assert.equal(isDefiniteAuthRefreshFailure(ApiError.http(401, 'Unauthorized')), true);
    assert.equal(isDefiniteAuthRefreshFailure(new Error('invalid_grant')), true);
    assert.equal(isTransientRefreshFailure(new Error('invalid_grant')), false);
  });

  it('retains identity for offline and timeout refresh failures', () => {
    assert.equal(isTransientRefreshFailure(ApiError.offline()), true);
    assert.equal(isTransientRefreshFailure(ApiError.timeout()), true);
    assert.equal(isTransientRefreshFailure(new TypeError('Network request failed')), true);
    assert.equal(isDefiniteAuthRefreshFailure(new TypeError('Network request failed')), false);
  });
});
