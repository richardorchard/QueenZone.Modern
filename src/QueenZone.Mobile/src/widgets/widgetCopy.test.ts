import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  WIDGET_FACE_SLOT_MS,
  nextWidgetFaceSlotMs,
  widgetActiveFace,
  widgetDayText,
  widgetEmptyText,
  widgetEyebrow,
  widgetHasDay,
  widgetHasQuote,
  widgetQuoteText,
} from './widgetCopy.ts';

const day = { formattedDate: '30 June 1980', summary: 'Queen released The Game.' };
const quote = { quoteText: 'A kind of magic', quoteWhoSaid: 'Freddie Mercury' };
const both = { ...day, ...quote };

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

  it('uses a gold Queen Quotes eyebrow on the quote face', () => {
    assert.equal(widgetEyebrow('day'), 'ON THIS DAY');
    assert.equal(widgetEyebrow('quote'), 'QUEEN QUOTES');
  });

  it('rotates between faces on a 4-hour UTC slot when both halves exist', () => {
    assert.equal(WIDGET_FACE_SLOT_MS, 4 * 60 * 60 * 1000);
    assert.equal(widgetActiveFace(both, 0), 'day');
    assert.equal(widgetActiveFace(both, WIDGET_FACE_SLOT_MS), 'quote');
    assert.equal(widgetActiveFace(both, WIDGET_FACE_SLOT_MS * 2), 'day');
    assert.equal(nextWidgetFaceSlotMs(0), WIDGET_FACE_SLOT_MS);
    assert.equal(nextWidgetFaceSlotMs(WIDGET_FACE_SLOT_MS - 1), WIDGET_FACE_SLOT_MS);
  });

  it('keeps a single face when only one half is available', () => {
    assert.equal(widgetActiveFace(day, WIDGET_FACE_SLOT_MS), 'day');
    assert.equal(widgetActiveFace(quote, 0), 'quote');
    assert.equal(widgetActiveFace({}, 0), null);
  });

  it('formats the day and quote lines the widgets render', () => {
    assert.equal(widgetDayText(day), '30 June 1980: Queen released The Game.');
    assert.equal(widgetQuoteText(quote), '“A kind of magic” — Freddie Mercury');
    assert.match(widgetEmptyText, /Open QueenZone/);
  });
});
