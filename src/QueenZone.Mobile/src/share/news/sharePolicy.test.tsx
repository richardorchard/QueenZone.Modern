import {
  isPersistedShareFresh,
  newsShareStaleWindowMs,
  shareIntakeFingerprint,
  shareSlotFingerprint,
} from './sharePolicy';

const nowMs = Date.parse('2026-09-08T12:00:00.000Z');

describe('isPersistedShareFresh', () => {
  it('treats a missing savedAt as stale', () => {
    expect(isPersistedShareFresh(undefined, nowMs)).toBe(false);
  });

  it('treats an invalid savedAt as stale', () => {
    expect(isPersistedShareFresh('not-a-date', nowMs)).toBe(false);
    expect(isPersistedShareFresh('', nowMs)).toBe(false);
  });

  it('keeps a slot at the 30 minute boundary and discards after', () => {
    const savedAt = new Date(nowMs - newsShareStaleWindowMs).toISOString();
    expect(isPersistedShareFresh(savedAt, nowMs)).toBe(true);
    expect(isPersistedShareFresh(savedAt, nowMs + 1)).toBe(false);
  });
});

describe('share fingerprints', () => {
  it('normalizes a form URL and a choose candidate set', () => {
    expect(
      shareSlotFingerprint({
        v: 1,
        kind: 'form',
        draft: { url: 'https://WWW.Example.com/story/', title: '', notes: '', origin: 'share' },
      }),
    ).toBe('https://www.example.com/story');
    expect(
      shareSlotFingerprint({
        v: 1,
        kind: 'choose',
        candidates: ['https://example.com/a/', 'https://example.com/b'],
      }),
    ).toBe('https://example.com/a\nhttps://example.com/b');
    expect(
      shareSlotFingerprint({
        v: 1,
        kind: 'form',
        draft: { url: '', title: '', notes: '', origin: 'inApp' },
      }),
    ).toBeNull();
  });

  it('matches intake to the last consumed fingerprint', () => {
    expect(
      shareIntakeFingerprint({
        kind: 'accepted',
        url: 'https://example.com/story/',
        leftoverText: '',
      }),
    ).toBe('https://example.com/story');
    expect(
      shareIntakeFingerprint({
        kind: 'choose',
        candidates: ['https://example.com/a', 'https://example.com/b'],
      }),
    ).toBe('https://example.com/a\nhttps://example.com/b');
    expect(shareIntakeFingerprint({ kind: 'rejected', reason: 'file', detail: 'no' })).toBeNull();
  });
});
