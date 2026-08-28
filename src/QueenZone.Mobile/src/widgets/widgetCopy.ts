export type WidgetProps = {
  formattedDate?: string;
  summary?: string;
  quoteText?: string;
  quoteWhoSaid?: string;
};

/**
 * Shared copy helpers for Android and tests. The iOS `'widget'` view must inline
 * the same strings and logic — expo-widgets serializes that function into JSC
 * without this module's bindings.
 */

export function widgetHasDay(props: WidgetProps): boolean {
  return Boolean(props.formattedDate && props.summary);
}

export function widgetHasQuote(props: WidgetProps): boolean {
  return Boolean(props.quoteText && props.quoteWhoSaid);
}

export function widgetEyebrow(hasDay: boolean): string {
  return hasDay ? 'ON THIS DAY' : 'QUOTE';
}

export function widgetDayText(props: WidgetProps): string {
  return `${props.formattedDate}: ${props.summary}`;
}

export function widgetQuoteText(props: WidgetProps): string {
  return `“${props.quoteText}” — ${props.quoteWhoSaid}`;
}

export const widgetEmptyText = "Open QueenZone to load today's story.";
