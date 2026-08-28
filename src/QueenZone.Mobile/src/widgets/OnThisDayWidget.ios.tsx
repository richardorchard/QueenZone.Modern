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
 * `createWidget` still has to store a layout string so `updateTimeline` can
 * write props into the app group. The home-screen pixels come from native
 * SwiftUI (`plugins/withIosOnThisDayNativeWidget.cjs`) because WidgetKit's
 * gallery snapshot never runs this JS — a missing JSC layout is a black
 * EmptyView in Release/TestFlight.
 */
export type OnThisDayWidgetProps = WidgetProps;

function OnThisDayWidgetView(props: OnThisDayWidgetProps) {
  'widget';

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
        {props.formattedDate && props.summary ? 'ON THIS DAY' : 'QUOTE'}
      </Text>
      {props.formattedDate && props.summary ? (
        <Text
          modifiers={[
            foregroundStyle('#F2F1ED'),
            font({ size: 13 }),
            lineLimit(3),
            truncationMode('tail'),
          ]}
        >
          {`${props.formattedDate}: ${props.summary}`}
        </Text>
      ) : null}
      {props.quoteText && props.quoteWhoSaid ? (
        <Text
          modifiers={[
            foregroundStyle('#B8B6B0'),
            font({ size: 12 }),
            lineLimit(3),
            truncationMode('tail'),
          ]}
        >
          {`“${props.quoteText}” — ${props.quoteWhoSaid}`}
        </Text>
      ) : null}
      {!(props.formattedDate && props.summary) && !(props.quoteText && props.quoteWhoSaid) ? (
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
