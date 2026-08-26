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
    assert.throws(() => parseNewsSuggestionCreated({ title: 'Missing id' }), /id/);
    assert.throws(() => parseNewsSuggestionCreated(null), /empty/);
  });
});
