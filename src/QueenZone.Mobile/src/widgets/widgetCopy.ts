export type WidgetProps = {
  formattedDate?: string;
  summary?: string;
  quoteText?: string;
  quoteWhoSaid?: string;
};

export type WidgetFace = 'day' | 'quote';

/**
 * Shared copy helpers for Android and tests. The iOS `'widget'` view must inline
 * the same strings and logic — expo-widgets serializes that function into JSC
 * without this module's bindings. Native SwiftUI duplicates the face-slot math
 * against `entry.date` (see `withIosOnThisDayNativeWidget.cjs`).
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
