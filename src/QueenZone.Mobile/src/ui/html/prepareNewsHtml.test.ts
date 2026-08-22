import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { prepareNewsHtml } from './prepareNewsHtml.ts';

describe('prepareNewsHtml', () => {
  it('returns empty for nullish input', () => {
    assert.equal(prepareNewsHtml(null), '');
    assert.equal(prepareNewsHtml(undefined), '');
    assert.equal(prepareNewsHtml(''), '');
  });

  it('keeps allowed news markup', () => {
    const html =
      '<p>Hello <strong>Queen</strong> <a href="https://example.com">link</a></p>' +
      '<p><img src="/ugc/news/x.jpg" alt="crest"></p>';
    assert.equal(prepareNewsHtml(html), html);
  });

  it('strips unsupported embeds so they degrade without markup leftovers', () => {
    const result = prepareNewsHtml(
      '<p>Before</p><iframe src="https://evil.example"></iframe><p>After</p><script>alert(1)</script>',
    );
    assert.doesNotMatch(result, /iframe/i);
    assert.doesNotMatch(result, /script/i);
    assert.match(result, /Before/);
    assert.match(result, /After/);
  });
});
