import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { isHttpUrl, resolveContentUrl } from './resolveContentUrl.ts';

describe('resolveContentUrl', () => {
  const origin = 'https://www.queenzone.org';

  it('returns absolute http(s) URLs unchanged', () => {
    assert.equal(
      resolveContentUrl('https://cdn.example/img.jpg', origin),
      'https://cdn.example/img.jpg',
    );
  });

  it('resolves root-relative UGC paths against the API origin', () => {
    assert.equal(
      resolveContentUrl('/ugc/news/sample-crest.jpg', origin),
      'https://www.queenzone.org/ugc/news/sample-crest.jpg',
    );
  });

  it('resolves protocol-relative URLs', () => {
    assert.equal(
      resolveContentUrl('//cdn.queenzone.org/photo.jpg', origin),
      'https://cdn.queenzone.org/photo.jpg',
    );
  });

  it('returns null for empty or invalid base', () => {
    assert.equal(resolveContentUrl('', origin), null);
    assert.equal(resolveContentUrl('/ugc/x.jpg', 'not-a-url'), null);
  });
});

describe('isHttpUrl', () => {
  it('accepts only http and https', () => {
    assert.equal(isHttpUrl('https://example.com'), true);
    assert.equal(isHttpUrl('http://example.com'), true);
    assert.equal(isHttpUrl('javascript:alert(1)'), false);
    assert.equal(isHttpUrl('/local'), false);
    assert.equal(isHttpUrl(null), false);
  });
});
