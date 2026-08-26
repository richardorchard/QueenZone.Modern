import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { ApiError, isLocalFileFailure, isOfflineFailure, isTimeoutFailure } from './errors.ts';

describe('ApiError', () => {
  it('infers offline from status 0 and never invents timeout', () => {
    const error = new ApiError(0, 'Unable to reach QueenZone. Check your connection and try again.');
    assert.equal(error.kind, 'offline');
    assert.equal(error.name, 'ApiError');
    assert.equal(isOfflineFailure(error), true);
    assert.equal(isTimeoutFailure(error), false);
  });

  it('mints offline through the factory', () => {
    const error = ApiError.offline();
    assert.equal(error.kind, 'offline');
    assert.equal(error.status, 0);
    assert.equal(isOfflineFailure(error), true);
  });

  it('mints timeout only through the factory', () => {
    const error = ApiError.timeout();
    assert.equal(error.kind, 'timeout');
    assert.equal(error.status, 0);
    assert.equal(error.name, 'ApiError');
    assert.equal(
      error.message,
      'QueenZone is taking too long to respond. Check your connection and try again.',
    );
    assert.equal(isTimeoutFailure(error), true);
    assert.equal(isOfflineFailure(error), false);
  });

  it('marks malformed 2xx separately from offline', () => {
    const error = ApiError.malformed(200);
    assert.equal(error.kind, 'malformed');
    assert.equal(error.status, 200);
    assert.equal(isOfflineFailure(error), false);
  });

  it('builds http failures from the factory', () => {
    const error = ApiError.http(404, 'Not found.');
    assert.equal(error.kind, 'http');
    assert.equal(error.status, 404);
    assert.equal(error.name, 'ApiError');
  });

  it('mints local-file through the factory and keeps the cause', () => {
    const cause = new TypeError('Network request failed');
    const error = ApiError.localFile(cause);
    assert.equal(error.kind, 'local-file');
    assert.equal(error.status, 0);
    assert.equal(error.message, 'Could not read the selected photo. Try choosing it again.');
    assert.equal(error.cause, cause);
    assert.equal(isLocalFileFailure(error), true);
    assert.equal(isOfflineFailure(error), false);
  });

  it('keeps the fetch TypeError on offline and timeout factories', () => {
    const cause = new TypeError('Failed to fetch');
    assert.equal(ApiError.offline(cause).cause, cause);
    assert.equal(ApiError.timeout(cause).cause, cause);
  });
});
