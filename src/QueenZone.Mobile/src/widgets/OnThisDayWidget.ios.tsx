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
import type { WidgetProps } from './widgetCopy';

/**
 * Props pushed from the app via `syncHomeWidget` (see widgetSync.tsx). Both halves are
 * optional and independent — either can be missing (no event today, no published quotes).
 * Quote reloads every few hours with jitter; on-this-day is only refetched on a new
 * calendar day.
 *
 * The `'widget'` function is serialized and evaluated in the WidgetKit JSC runtime.
 * It must be referentially free: only `@expo/ui` imports, `props`, and literals.
 * Imported helpers / module constants become missing identifiers, and a Release
 * TestFlight build swallows that error as a black EmptyView.
 */
export type OnThisDayWidgetProps = WidgetProps;

function OnThisDayWidgetView(props: OnThisDayWidgetProps) {
  'widget';

  const formattedDate = props == null ? undefined : props.formattedDate;
  const summary = props == null ? undefined : props.summary;
  const quoteText = props == null ? undefined : props.quoteText;
  const quoteWhoSaid = props == null ? undefined : props.quoteWhoSaid;
  const hasDay = Boolean(formattedDate && summary);
  const hasQuote = Boolean(quoteText && quoteWhoSaid);

  return (
    <VStack
      alignment="leading"
      spacing={6}
      modifiers={[
        padding({ all: 14 }),
        containerBackground('#181614', 'widget'),
        widgetURL('queenzone://home'),
      ]}
    >
      <Text modifiers={[foregroundStyle('#B89A4A'), font({ size: 10, weight: 'semibold' })]}>
        {hasDay ? 'ON THIS DAY' : 'QUOTE'}
      </Text>
      {hasDay ? (
        <Text
          modifiers={[
            foregroundStyle('#F2F1ED'),
            font({ size: 13 }),
            lineLimit(3),
            truncationMode('tail'),
          ]}
        >
          {`${formattedDate}: ${summary}`}
        </Text>
      ) : null}
      {hasQuote ? (
        <Text
          modifiers={[
            foregroundStyle('#B8B6B0'),
            font({ size: 12 }),
            lineLimit(3),
            truncationMode('tail'),
          ]}
        >
          {`“${quoteText}” — ${quoteWhoSaid}`}
        </Text>
      ) : null}
      {!hasDay && !hasQuote ? (
        <Text
          modifiers={[
            foregroundStyle('#B8B6B0'),
            font({ size: 12 }),
            lineLimit(3),
            truncationMode('tail'),
          ]}
        >
          Open QueenZone to load today's story.
        </Text>
      ) : null}
    </VStack>
  );
}

/** Registered widget instance — `updateSnapshot`/`updateTimeline` are called from widgetSync.ts. */
export const OnThisDayWidget = createWidget<OnThisDayWidgetProps>('OnThisDayWidget', OnThisDayWidgetView);
