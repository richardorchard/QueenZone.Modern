import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  WIDGET_FACE_SLOT_MS,
  WIDGET_QUOTE_LERP_LONG,
  WIDGET_QUOTE_LERP_SHORT,
  WIDGET_QUOTE_MAX_LINES,
  WIDGET_QUOTE_MAX_PT_MEDIUM,
  WIDGET_QUOTE_MAX_PT_SMALL,
  WIDGET_QUOTE_MEDIUM_MIN_WIDTH,
  WIDGET_QUOTE_MIN_SCALE,
  WIDGET_QUOTE_SECONDARY_MAX_LINES,
  WIDGET_QUOTE_SECONDARY_PT_MEDIUM,
  WIDGET_QUOTE_SECONDARY_PT_SMALL,
  nextWidgetFaceSlotMs,
  widgetActiveFace,
  widgetDayPrimary,
  widgetDaySecondary,
  widgetDayText,
  widgetEmptyText,
  widgetEyebrow,
  widgetFamilyFromWidth,
  widgetGraphemeCount,
  widgetHasDay,
  widgetHasQuote,
  widgetPrimaryCeilingPt,
  widgetPrimaryFontSize,
  widgetQuotePrimary,
  widgetQuoteSecondary,
  widgetQuoteText,
  widgetSecondaryPt,
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

  it('splits primary and secondary instead of concatenating in the view', () => {
    assert.equal(widgetQuotePrimary(quote), '“A kind of magic”');
    assert.equal(widgetQuoteSecondary(quote), '— Freddie Mercury');
    assert.equal(widgetDayPrimary(day), 'Queen released The Game.');
    assert.equal(widgetDaySecondary(day), '30 June 1980');
    assert.notEqual(widgetQuotePrimary(quote), widgetQuoteText(quote));
    assert.notEqual(widgetDayPrimary(day), widgetDayText(day));
  });

  it('publishes the shared primary/secondary type-scale band', () => {
    assert.equal(WIDGET_QUOTE_MAX_PT_SMALL, 17);
    assert.equal(WIDGET_QUOTE_MAX_PT_MEDIUM, 22);
    assert.equal(WIDGET_QUOTE_MIN_SCALE, 0.65);
    assert.equal(WIDGET_QUOTE_MAX_LINES, 6);
    assert.equal(WIDGET_QUOTE_SECONDARY_PT_SMALL, 9);
    assert.equal(WIDGET_QUOTE_SECONDARY_PT_MEDIUM, 11);
    assert.equal(WIDGET_QUOTE_SECONDARY_MAX_LINES, 2);
    assert.equal(widgetPrimaryCeilingPt('small'), 17);
    assert.equal(widgetPrimaryCeilingPt('medium'), 22);
    assert.equal(widgetSecondaryPt('small'), 9);
    assert.equal(widgetSecondaryPt('medium'), 11);
    assert.ok(WIDGET_QUOTE_SECONDARY_PT_SMALL < 17 * WIDGET_QUOTE_MIN_SCALE);
    assert.ok(WIDGET_QUOTE_SECONDARY_PT_MEDIUM < 22 * WIDGET_QUOTE_MIN_SCALE);
  });

  it('picks the medium ceiling once the Android span is 4×2-wide', () => {
    assert.equal(widgetFamilyFromWidth(undefined), 'small');
    assert.equal(widgetFamilyFromWidth(WIDGET_QUOTE_MEDIUM_MIN_WIDTH - 1), 'small');
    assert.equal(widgetFamilyFromWidth(WIDGET_QUOTE_MEDIUM_MIN_WIDTH), 'medium');
  });

  it('lerps Android primary size from grapheme count between floor and ceiling', () => {
    const short = 'x'.repeat(WIDGET_QUOTE_LERP_SHORT);
    const long = 'x'.repeat(WIDGET_QUOTE_LERP_LONG);
    const mid = 'x'.repeat((WIDGET_QUOTE_LERP_SHORT + WIDGET_QUOTE_LERP_LONG) / 2);
    assert.equal(widgetPrimaryFontSize(short, 'small'), 17);
    assert.equal(widgetPrimaryFontSize(short, 'medium'), 22);
    assert.equal(widgetPrimaryFontSize(long, 'small'), 17 * 0.65);
    assert.equal(widgetPrimaryFontSize(long, 'medium'), 22 * 0.65);
    assert.ok(widgetPrimaryFontSize(mid, 'small') < 17);
    assert.ok(widgetPrimaryFontSize(mid, 'small') > 17 * 0.65);
    assert.equal(widgetPrimaryFontSize(widgetEmptyText, 'small'), 17);
    assert.ok(widgetGraphemeCount('Get drunk and sing along to Queen.') <= WIDGET_QUOTE_LERP_SHORT);
  });
});
