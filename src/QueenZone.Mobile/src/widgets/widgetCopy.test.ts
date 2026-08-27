import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  widgetDayText,
  widgetEmptyText,
  widgetEyebrow,
  widgetHasDay,
  widgetHasQuote,
  widgetQuoteText,
} from './widgetCopy.ts';

const day = { formattedDate: '30 June 1980', summary: 'Queen released The Game.' };
const quote = { quoteText: 'A kind of magic', quoteWhoSaid: 'Freddie Mercury' };

describe('widgetCopy', () => {
  it('requires both date and summary for the on-this-day half', () => {
    assert.equal(widgetHasDay(day), true);
    assert.equal(widgetHasDay({ formattedDate: '30 June 1980' }), false);
    assert.equal(widgetHasDay({ summary: 'Queen released The Game.' }), false);
  });

  it('requires both quote text and speaker', () => {
    assert.equal(widgetHasQuote(quote), true);
    assert.equal(widgetHasQuote({ quoteText: 'A kind of magic' }), false);
    assert.equal(widgetHasQuote({ quoteWhoSaid: 'Freddie Mercury' }), false);
  });

  it('uses the quote eyebrow when there is no on-this-day event', () => {
    assert.equal(widgetEyebrow(true), 'ON THIS DAY');
    assert.equal(widgetEyebrow(false), 'QUOTE');
  });

  it('formats the day and quote lines the widgets render', () => {
    assert.equal(widgetDayText(day), '30 June 1980: Queen released The Game.');
    assert.equal(widgetQuoteText(quote), '“A kind of magic” — Freddie Mercury');
    assert.match(widgetEmptyText, /Open QueenZone/);
  });
});
