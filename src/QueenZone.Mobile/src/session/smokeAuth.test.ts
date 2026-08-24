import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  buildSmokeAuthUrl,
  isSmokeAuthEnabled,
  parseSmokeAuthAccessToken,
  smokeAuthHost,
  smokeAuthScheme,
} from './smokeAuth.ts';

describe('smokeAuth', () => {
  it('enables only when the Debug __DEV__ flag is true', () => {
    assert.equal(isSmokeAuthEnabled({ dev: true }), true);
    assert.equal(isSmokeAuthEnabled({ dev: false }), false);
    assert.equal(isSmokeAuthEnabled({}), false);
  });

  it('parses a smoke-auth deep link and ignores OAuth callbacks', () => {
    const token = 'header.payload.signature';
    const url = buildSmokeAuthUrl(token);
    assert.equal(url.startsWith(`${smokeAuthScheme}://${smokeAuthHost}?`), true);
    assert.equal(parseSmokeAuthAccessToken(url), token);
    assert.equal(
      parseSmokeAuthAccessToken('queenzone://auth/callback?code=abc&state=xyz'),
      null,
    );
    assert.equal(parseSmokeAuthAccessToken('queenzone://smoke-auth'), null);
    assert.equal(parseSmokeAuthAccessToken('not-a-url'), null);
  });

  it('rejects an empty token when building a smoke-auth URL', () => {
    assert.throws(() => buildSmokeAuthUrl('   '), /non-empty access token/);
  });
});
