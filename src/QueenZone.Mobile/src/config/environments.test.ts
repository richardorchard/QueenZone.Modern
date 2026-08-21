import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { describe, it } from 'node:test';

const require = createRequire(import.meta.url);
const {
  defaultApiBaseUrls,
  normalizeApiBaseUrl,
  resolveApiBaseUrl,
  resolveAppEnvironment,
  rewriteLoopbackForAndroid,
} = require('../../apiEnvironments.cjs') as typeof import('../../apiEnvironments.cjs');

describe('resolveAppEnvironment', () => {
  it('defaults to development when unset', () => {
    assert.equal(resolveAppEnvironment(undefined), 'development');
    assert.equal(resolveAppEnvironment(''), 'development');
  });

  it('accepts full names and short aliases', () => {
    assert.equal(resolveAppEnvironment('development'), 'development');
    assert.equal(resolveAppEnvironment('Staging'), 'staging');
    assert.equal(resolveAppEnvironment('PRODUCTION'), 'production');
    assert.equal(resolveAppEnvironment('dev'), 'development');
    assert.equal(resolveAppEnvironment('stage'), 'staging');
    assert.equal(resolveAppEnvironment('prod'), 'production');
  });

  it('rejects unknown values', () => {
    assert.throws(() => resolveAppEnvironment('qa'), /Unknown app environment/);
  });
});

describe('resolveApiBaseUrl', () => {
  it('uses committed defaults per environment', () => {
    assert.equal(resolveApiBaseUrl({ appEnv: 'development' }), defaultApiBaseUrls.development);
    assert.equal(resolveApiBaseUrl({ appEnv: 'staging' }), defaultApiBaseUrls.staging);
    assert.equal(resolveApiBaseUrl({ appEnv: 'production' }), defaultApiBaseUrls.production);
  });

  it('lets EXPO_PUBLIC_API_BASE_URL override without code changes', () => {
    assert.equal(
      resolveApiBaseUrl({
        appEnv: 'production',
        override: 'https://localhost:7162/',
      }),
      'https://localhost:7162',
    );
    assert.equal(
      resolveApiBaseUrl({
        appEnv: 'staging',
        override: 'http://192.168.1.20:5146',
      }),
      'http://192.168.1.20:5146',
    );
  });

  it('strips trailing slashes and accidental /api/v1 paths', () => {
    assert.equal(normalizeApiBaseUrl('https://www.queenzone.org/api/v1/'), 'https://www.queenzone.org');
  });

  it('rejects empty or non-http(s) URLs', () => {
    assert.throws(() => normalizeApiBaseUrl(''), /must not be empty/);
    assert.throws(() => normalizeApiBaseUrl('ftp://example.com'), /must be http/);
    assert.throws(() => normalizeApiBaseUrl('not-a-url'), /not a valid absolute URL/);
  });
});

describe('rewriteLoopbackForAndroid', () => {
  it('rewrites localhost and 127.0.0.1 on Android only', () => {
    assert.equal(
      rewriteLoopbackForAndroid('http://localhost:5146', 'android'),
      'http://10.0.2.2:5146',
    );
    assert.equal(
      rewriteLoopbackForAndroid('http://127.0.0.1:5146', 'android'),
      'http://10.0.2.2:5146',
    );
    assert.equal(
      rewriteLoopbackForAndroid('http://localhost:5146', 'ios'),
      'http://localhost:5146',
    );
    assert.equal(
      rewriteLoopbackForAndroid('https://www.queenzone.org', 'android'),
      'https://www.queenzone.org',
    );
  });
});
