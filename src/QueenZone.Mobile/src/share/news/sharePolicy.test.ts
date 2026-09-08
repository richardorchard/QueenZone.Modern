import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { isPersistedShareFresh, newsShareStaleWindowMs, shareIntakeFingerprint, shareSlotFingerprint } from './sharePolicy.ts';

const nowMs = Date.parse('2026-09-08T12:00:00.000Z');

describe('isPersistedShareFresh', () => {
  it('treats a missing savedAt as stale', () => {
    assert.equal(isPersistedShareFresh(undefined, nowMs), false);
  });

  it('treats an invalid savedAt as stale', () => {
    assert.equal(isPersistedShareFresh('not-a-date', nowMs), false);
    assert.equal(isPersistedShareFresh('', nowMs), false);
  });

  it('keeps a slot at the 30 minute boundary and discards after', () => {
    const savedAt = new Date(nowMs - newsShareStaleWindowMs).toISOString();
    assert.equal(isPersistedShareFresh(savedAt, nowMs), true);
    assert.equal(isPersistedShareFresh(savedAt, nowMs + 1), false);
  });
});

describe('share fingerprints', () => {
  it('normalizes a form URL and a choose candidate set', () => {
    assert.equal(
      shareSlotFingerprint({
        v: 1,
        kind: 'form',
        draft: { url: 'https://WWW.Example.com/story/', title: '', notes: '', origin: 'share' },
      }),
      'https://www.example.com/story',
    );
    assert.equal(
      shareSlotFingerprint({
        v: 1,
        kind: 'choose',
        candidates: ['https://example.com/a/', 'https://example.com/b'],
      }),
      'https://example.com/a\nhttps://example.com/b',
    );
    assert.equal(
      shareSlotFingerprint({
        v: 1,
        kind: 'form',
        draft: { url: '', title: '', notes: '', origin: 'inApp' },
      }),
      null,
    );
  });

  it('matches intake to the last consumed fingerprint', () => {
    const accepted = shareIntakeFingerprint({
      kind: 'accepted',
      url: 'https://example.com/story/',
      leftoverText: '',
    });
    assert.equal(accepted, 'https://example.com/story');
    assert.equal(
      shareIntakeFingerprint({
        kind: 'choose',
        candidates: ['https://example.com/a', 'https://example.com/b'],
      }),
      'https://example.com/a\nhttps://example.com/b',
    );
    assert.equal(
      shareIntakeFingerprint({ kind: 'rejected', reason: 'file', detail: 'no' }),
      null,
    );
  });
});
