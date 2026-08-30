import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { readJwtSubject, resolvePushMemberId } from './pushMemberId.ts';

function accessJwt(payload: object): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode(payload)}.sig`;
}

describe('resolvePushMemberId', () => {
  it('prefers an explicit member id over the access token', () => {
    assert.equal(resolvePushMemberId(accessJwt({ sub: 'from-jwt' }), ' from-profile '), 'from-profile');
  });

  it('reads sub from a JWT when no member id is passed', () => {
    assert.equal(resolvePushMemberId(accessJwt({ sub: 'jwt-member' })), 'jwt-member');
  });

  it('returns null for an opaque token with no member id', () => {
    assert.equal(resolvePushMemberId('access-token'), null);
  });
});

describe('readJwtSubject', () => {
  it('returns sub from a URL-safe payload', () => {
    assert.equal(readJwtSubject(accessJwt({ sub: '11111111-1111-1111-1111-111111111111' })), '11111111-1111-1111-1111-111111111111');
  });

  it('ignores empty or missing sub', () => {
    assert.equal(readJwtSubject(accessJwt({ sub: '  ' })), null);
    assert.equal(readJwtSubject(accessJwt({ nameid: 'not-sub' })), null);
  });

  it('returns null for malformed tokens', () => {
    assert.equal(readJwtSubject('not-a-jwt'), null);
    assert.equal(readJwtSubject('a.not-json.sig'), null);
    assert.equal(readJwtSubject('a.b'), null);
  });

  it('returns null when the payload is not an object', () => {
    const encode = (value: string) => Buffer.from(value).toString('base64url');
    const header = encode(JSON.stringify({ alg: 'none', typ: 'JWT' }));
    assert.equal(readJwtSubject(`${header}.${encode('true')}.sig`), null);
    assert.equal(readJwtSubject(`${header}.${encode('null')}.sig`), null);
  });
});
