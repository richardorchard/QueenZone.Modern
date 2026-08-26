import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { newsSuggestionsPath, parseNewsSuggestionCreated } from './newsSuggestionResponse.ts';

describe('newsSuggestionsPath', () => {
  it('posts under the versioned member API', () => {
    assert.equal(newsSuggestionsPath, '/member/news-suggestions');
  });
});

describe('parseNewsSuggestionCreated', () => {
  it('reads the 201 contract', () => {
    const created = parseNewsSuggestionCreated({
      id: '11111111-1111-1111-1111-111111111111',
      status: 'Pending',
      url: 'https://www.bbc.co.uk/news/example',
      title: 'Queen announce dates',
      submittedAt: '2026-08-26T10:00:00.000Z',
    });
    assert.equal(created.status, 'Pending');
    assert.equal(created.url, 'https://www.bbc.co.uk/news/example');
    assert.equal(created.title, 'Queen announce dates');
  });

  it('accepts a null title', () => {
    const created = parseNewsSuggestionCreated({
      id: '11111111-1111-1111-1111-111111111111',
      status: 'Pending',
      url: 'https://www.bbc.co.uk/news/example',
      title: null,
      submittedAt: '2026-08-26T10:00:00.000Z',
    });
    assert.equal(created.title, null);
  });

  it('rejects payloads that cannot show confirmation', () => {
    const base = {
      id: '11111111-1111-1111-1111-111111111111',
      status: 'Pending',
      url: 'https://www.bbc.co.uk/news/example',
      title: 'Queen announce dates',
      submittedAt: '2026-08-26T10:00:00.000Z',
    };
    assert.throws(() => parseNewsSuggestionCreated({ title: 'Missing id' }), /id/);
    assert.throws(() => parseNewsSuggestionCreated(null), /empty/);
    assert.throws(() => parseNewsSuggestionCreated({ ...base, status: '  ' }), /status/);
    assert.throws(() => parseNewsSuggestionCreated({ ...base, url: '' }), /url/);
    assert.throws(() => parseNewsSuggestionCreated({ ...base, title: 12 }), /title/);
    assert.throws(() => parseNewsSuggestionCreated({ ...base, submittedAt: '' }), /submitted/);
  });
});
