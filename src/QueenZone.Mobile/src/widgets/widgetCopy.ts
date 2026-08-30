export type WidgetProps = {
  formattedDate?: string;
  summary?: string;
  quoteText?: string;
  quoteWhoSaid?: string;
  quoteId?: number;
};

export type WidgetFace = 'day' | 'quote';

/**
 * Shared copy helpers for Android and tests. The iOS `'widget'` view must inline
 * the same strings and logic — expo-widgets serializes that function into JSC
 * without this module's bindings. Native SwiftUI duplicates the face-slot math
 * and the primary/secondary type-scale literals against `entry.date`
 * (see `withIosOnThisDayNativeWidget.cjs`).
 */

/** Same 4-hour slot the Android widget period and iOS quote timeline already use. */
export const WIDGET_FACE_SLOT_MS = 4 * 60 * 60 * 1000;

export function widgetHasDay(props: WidgetProps): boolean {
  return Boolean(props.formattedDate && props.summary);
}

export function widgetHasQuote(props: WidgetProps): boolean {
  return Boolean(props.quoteText && props.quoteWhoSaid);
}

export function nextWidgetFaceSlotMs(atMs: number): number {
  return Math.floor(atMs / WIDGET_FACE_SLOT_MS) * WIDGET_FACE_SLOT_MS + WIDGET_FACE_SLOT_MS;
}

export function widgetActiveFace(props: WidgetProps, atMs: number = Date.now()): WidgetFace | null {
  const hasDay = widgetHasDay(props);
  const hasQuote = widgetHasQuote(props);
  if (!hasDay && !hasQuote) {
    return null;
  }
  if (hasDay && hasQuote) {
    return Math.floor(atMs / WIDGET_FACE_SLOT_MS) % 2 === 0 ? 'day' : 'quote';
  }
  return hasDay ? 'day' : 'quote';
}

export function widgetEyebrow(face: WidgetFace): string {
  return face === 'day' ? 'ON THIS DAY' : 'QUEEN QUOTES';
}

export function widgetDayText(props: WidgetProps): string {
  return `${props.formattedDate}: ${props.summary}`;
}

export function widgetQuoteText(props: WidgetProps): string {
  return `“${props.quoteText}” — ${props.quoteWhoSaid}`;
}

export const widgetEmptyText = "Open QueenZone to load today's story.";

/** Primary type ceiling on systemSmall / 2×2. Native SwiftUI duplicates this literal. */
export const WIDGET_QUOTE_MAX_PT_SMALL = 17;

/** Primary type ceiling on systemMedium / 4×2. Native SwiftUI duplicates this literal. */
export const WIDGET_QUOTE_MAX_PT_MEDIUM = 22;

/** Shrink the primary from the ceiling until it fits; never below ceiling × this. */
export const WIDGET_QUOTE_MIN_SCALE = 0.65;

/** Overflow net after scaling (#992). Native SwiftUI duplicates this literal. */
export const WIDGET_QUOTE_MAX_LINES = 6;

/** Attribution / date under the primary. Always below the primary floor. */
export const WIDGET_QUOTE_SECONDARY_PT_SMALL = 9;
export const WIDGET_QUOTE_SECONDARY_PT_MEDIUM = 11;
export const WIDGET_QUOTE_SECONDARY_MAX_LINES = 2;

/** Android lerp: at or below this grapheme count the primary stays at the ceiling. */
export const WIDGET_QUOTE_LERP_SHORT = 40;

/** Android lerp: at or above this grapheme count the primary sits on the floor. */
export const WIDGET_QUOTE_LERP_LONG = 120;

/** Android width (dp) at which the widget uses the medium ceiling (4×2). */
export const WIDGET_QUOTE_MEDIUM_MIN_WIDTH = 250;

export type WidgetFamily = 'small' | 'medium';

export function widgetFamilyFromWidth(width?: number): WidgetFamily {
  return width != null && width >= WIDGET_QUOTE_MEDIUM_MIN_WIDTH ? 'medium' : 'small';
}

export function widgetPrimaryCeilingPt(family: WidgetFamily): number {
  return family === 'medium' ? WIDGET_QUOTE_MAX_PT_MEDIUM : WIDGET_QUOTE_MAX_PT_SMALL;
}

export function widgetSecondaryPt(family: WidgetFamily): number {
  return family === 'medium' ? WIDGET_QUOTE_SECONDARY_PT_MEDIUM : WIDGET_QUOTE_SECONDARY_PT_SMALL;
}

export function widgetGraphemeCount(copy: string): number {
  return [...new Intl.Segmenter('en', { granularity: 'grapheme' }).segment(copy)].length;
}

/**
 * Android TextWidget has auto-size but no 0.65 min scale, so lerp the starting
 * size from copy length. iOS native uses minimumScaleFactor from the ceiling.
 */
export function widgetPrimaryFontSize(copy: string, family: WidgetFamily): number {
  const ceiling = widgetPrimaryCeilingPt(family);
  const floor = ceiling * WIDGET_QUOTE_MIN_SCALE;
  const length = widgetGraphemeCount(copy);
  if (length <= WIDGET_QUOTE_LERP_SHORT) {
    return ceiling;
  }
  if (length >= WIDGET_QUOTE_LERP_LONG) {
    return floor;
  }
  const t = (length - WIDGET_QUOTE_LERP_SHORT) / (WIDGET_QUOTE_LERP_LONG - WIDGET_QUOTE_LERP_SHORT);
  return ceiling + (floor - ceiling) * t;
}

export function widgetQuotePrimary(props: WidgetProps): string {
  return `“${props.quoteText}”`;
}

export function widgetQuoteSecondary(props: WidgetProps): string {
  return `— ${props.quoteWhoSaid}`;
}

export function widgetDayPrimary(props: WidgetProps): string {
  return props.summary ?? '';
}

export function widgetDaySecondary(props: WidgetProps): string {
  return props.formattedDate ?? '';
}
