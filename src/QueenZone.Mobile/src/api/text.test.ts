import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { formatPublishedDate, toPlainText } from './text.ts';

describe('toPlainText', () => {
  it('returns empty string for nullish input', () => {
    assert.equal(toPlainText(null), '');
    assert.equal(toPlainText(undefined), '');
    assert.equal(toPlainText(''), '');
  });

  it('strips tags and decodes common entities', () => {
    assert.equal(
      toPlainText('<p>Hello&nbsp;<strong>Queen</strong> &amp; Co.</p>'),
      'Hello Queen & Co.',
    );
  });

  it('turns breaks into newlines', () => {
    assert.equal(toPlainText('Line one<br/>Line two'), 'Line one\nLine two');
  });
});

describe('formatPublishedDate', () => {
  it('formats a valid ISO date', () => {
    const formatted = formatPublishedDate('2020-06-15T12:00:00Z');
    assert.match(formatted, /2020/);
    assert.match(formatted, /15|Jun/i);
  });

  it('returns empty string for invalid dates', () => {
    assert.equal(formatPublishedDate('not-a-date'), '');
  });
});
