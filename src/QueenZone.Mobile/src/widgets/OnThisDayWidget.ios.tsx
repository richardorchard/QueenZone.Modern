import { Text, VStack } from '@expo/ui/swift-ui';
import {
  containerBackground,
  font,
  foregroundStyle,
  lineLimit,
  padding,
  truncationMode,
  widgetURL,
} from '@expo/ui/swift-ui/modifiers';
import { createWidget } from 'expo-widgets';
import {
  widgetDayText,
  widgetEmptyText,
  widgetEyebrow,
  widgetHasDay,
  widgetHasQuote,
  widgetQuoteText,
  type WidgetProps,
} from './widgetCopy';
import { widgetDeepLinkUrl } from './widgetDeepLink';

/**
 * Props pushed from the app via `syncHomeWidget` (see widgetSync.tsx). Both halves are
 * optional and independent — either can be missing (no event today, no published quotes).
 */
export type OnThisDayWidgetProps = WidgetProps;

const gold = '#B89A4A';
const cream = '#F2F1ED';
const mutedCream = 'rgba(242,241,237,0.72)';
const cardBackground = '#181614';

/** Matches Android `maxLines={3}` so long copy clips on systemSmall/systemMedium. */
const widgetBodyLineLimit = 3;

function bodyTextModifiers(color: string, size: number) {
  return [
    foregroundStyle(color),
    font({ size }),
    lineLimit(widgetBodyLineLimit),
    truncationMode('tail'),
  ];
}

function OnThisDayWidgetView(props: OnThisDayWidgetProps) {
  'widget';

  const hasDay = widgetHasDay(props);
  const hasQuote = widgetHasQuote(props);

  return (
    <VStack
      alignment="leading"
      spacing={6}
      modifiers={[
        padding({ all: 14 }),
        containerBackground(cardBackground, 'widget'),
        widgetURL(widgetDeepLinkUrl),
      ]}
    >
      <Text modifiers={[foregroundStyle(gold), font({ size: 10, weight: 'semibold' })]}>
        {widgetEyebrow(hasDay)}
      </Text>
      {hasDay ? (
        <Text modifiers={bodyTextModifiers(cream, 13)}>{widgetDayText(props)}</Text>
      ) : null}
      {hasQuote ? (
        <Text modifiers={bodyTextModifiers(mutedCream, 12)}>{widgetQuoteText(props)}</Text>
      ) : null}
      {!hasDay && !hasQuote ? (
        <Text modifiers={bodyTextModifiers(mutedCream, 12)}>{widgetEmptyText}</Text>
      ) : null}
    </VStack>
  );
}

/** Registered widget instance — `updateSnapshot`/`updateTimeline` are called from widgetSync.ts. */
export const OnThisDayWidget = createWidget<OnThisDayWidgetProps>('OnThisDayWidget', OnThisDayWidgetView);
