import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { describe, it } from 'node:test';

const source = readFileSync(new URL('./OnThisDayWidget.ios.tsx', import.meta.url), 'utf8');

const viewStart = source.indexOf('function OnThisDayWidgetView');
const viewEnd = source.indexOf('export const OnThisDayWidget');
const viewBody = source.slice(viewStart, viewEnd);

describe('OnThisDayWidget.ios referential freedom', () => {
  it('keeps the widget function free of imported helpers and module constants', () => {
    assert.notEqual(viewStart, -1);
    assert.notEqual(viewEnd, -1);

    for (const ident of [
      'widgetHasDay',
      'widgetHasQuote',
      'widgetEyebrow',
      'widgetDayText',
      'widgetQuoteText',
      'widgetEmptyText',
      'widgetDeepLinkUrl',
      'gold',
      'cream',
      'mutedCream',
      'cardBackground',
    ]) {
      assert.equal(viewBody.includes(ident), false, `widget function must not reference ${ident}`);
    }
  });

  it('inlines the home deep link, card colors, and empty-state copy', () => {
    assert.match(viewBody, /queenzone:\/\/home/);
    assert.match(viewBody, /#181614/);
    assert.match(viewBody, /#B89A4A/);
    assert.match(viewBody, /#F2F1ED/);
    assert.match(viewBody, /#B8B6B0/);
    assert.match(viewBody, /Open QueenZone to load today's story\./);
    assert.match(viewBody, /ON THIS DAY/);
    assert.match(viewBody, /QUEEN QUOTES/);
    assert.match(viewBody, /4 \* 60 \* 60 \* 1000/);
  });
});
