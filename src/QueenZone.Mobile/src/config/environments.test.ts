import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { describe, it } from 'node:test';

const require = createRequire(import.meta.url);
const {
  defaultApiBaseUrls,
  marketingVersionPrefix,
  normalizeApiBaseUrl,
  resolveApiBaseUrl,
  resolveAppEnvironment,
  resolveIosBuildNumber,
  resolveMarketingVersion,
  rewriteLoopbackForAndroid,
} = require('../../apiEnvironments.cjs') as typeof import('../../apiEnvironments.cjs');

function compareDotted(left: string, right: string): number {
  const a = left.split('.').map(Number);
  const b = right.split('.').map(Number);
  const length = Math.max(a.length, b.length);
  for (let index = 0; index < length; index += 1) {
    const av = a[index] ?? 0;
    const bv = b[index] ?? 0;
    if (av !== bv) {
      return av - bv;
    }
  }
  return 0;
}

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

  it('routes staging and production through the public site origin', () => {
    assert.equal(defaultApiBaseUrls.staging, 'https://www.queenzone.org');
    assert.equal(defaultApiBaseUrls.production, 'https://www.queenzone.org');
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

describe('resolveMarketingVersion', () => {
  it('uses the committed 0.1 prefix and local unsigned default', () => {
    assert.equal(marketingVersionPrefix, '0.1');
    assert.equal(resolveMarketingVersion(), '0.1.0');
    assert.equal(resolveMarketingVersion({}), '0.1.0');
    assert.equal(resolveMarketingVersion({ prefix: '0.1' }), '0.1.0');
    assert.equal(resolveMarketingVersion({ prefix: '0.1', runNumber: '' }), '0.1.0');
    assert.equal(resolveMarketingVersion({ prefix: '0.1', runNumber: '   ' }), '0.1.0');
  });

  it('appends the publish run number', () => {
    assert.equal(resolveMarketingVersion({ prefix: '0.1', runNumber: '847' }), '0.1.847');
    assert.equal(resolveMarketingVersion({ prefix: '0.1', runNumber: 214 }), '0.1.214');
    assert.equal(resolveMarketingVersion({ prefix: '0.1', runNumber: '007' }), '0.1.7');
  });

  it('rejects prefixes that are not N.N and non-positive run numbers', () => {
    assert.throws(() => resolveMarketingVersion({ prefix: '1' }), /N\.N/);
    assert.throws(() => resolveMarketingVersion({ prefix: '1.0.0' }), /N\.N/);
    assert.throws(() => resolveMarketingVersion({ prefix: 'v1.0' }), /N\.N/);
    assert.throws(() => resolveMarketingVersion({ prefix: '1.x' }), /N\.N/);
    assert.throws(() => resolveMarketingVersion({ prefix: '' }), /N\.N/);
    assert.throws(
      () => resolveMarketingVersion({ prefix: '0.1', runNumber: '0' }),
      /positive integer/,
    );
    assert.throws(
      () => resolveMarketingVersion({ prefix: '0.1', runNumber: '1.5' }),
      /positive integer/,
    );
    assert.throws(
      () => resolveMarketingVersion({ prefix: '0.1', runNumber: 'abc' }),
      /positive integer/,
    );
  });

  it('keeps a 1.x flip greater than 0.1.{n} as dotted integers', () => {
    assert.equal(resolveMarketingVersion({ prefix: '1.0', runNumber: '847' }), '1.0.847');
    assert.equal(resolveMarketingVersion({ prefix: '1.1', runNumber: '847' }), '1.1.847');
    assert.ok(compareDotted('1.0.847', '0.1.847') > 0);
    assert.ok(compareDotted('1.0.1', '0.1.999') > 0);
    assert.ok(compareDotted('0.1.848', '0.1.847') > 0);
  });
});

describe('resolveIosBuildNumber', () => {
  it('defaults to 1 when nothing is provided', () => {
    assert.equal(resolveIosBuildNumber(), '1');
    assert.equal(resolveIosBuildNumber({}), '1');
  });

  it('prefers IOS_BUILD_NUMBER override, then GITHUB_RUN_NUMBER, then fallback', () => {
    assert.equal(
      resolveIosBuildNumber({
        override: '42',
        githubRunNumber: '3',
        fallback: '9',
      }),
      '42',
    );
    assert.equal(
      resolveIosBuildNumber({
        githubRunNumber: '3',
        fallback: '9',
      }),
      '3',
    );
    assert.equal(resolveIosBuildNumber({ fallback: '9' }), '9');
  });

  it('normalizes leading zeros and rejects non-positive values', () => {
    assert.equal(resolveIosBuildNumber({ override: '007' }), '7');
    assert.throws(() => resolveIosBuildNumber({ override: '0' }), /positive integer/);
    assert.throws(() => resolveIosBuildNumber({ override: '1.5' }), /positive integer/);
    assert.throws(() => resolveIosBuildNumber({ override: 'abc' }), /positive integer/);
  });
});
