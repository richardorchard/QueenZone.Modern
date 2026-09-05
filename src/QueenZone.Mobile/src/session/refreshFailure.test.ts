import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { ApiError } from '../api/errors.ts';
import { isDefiniteAuthRefreshFailure, isTransientRefreshFailure } from './refreshFailure.ts';

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
