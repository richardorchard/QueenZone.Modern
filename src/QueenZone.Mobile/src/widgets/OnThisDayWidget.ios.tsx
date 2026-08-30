import { Text, VStack } from '@expo/ui/swift-ui';
import {
  containerBackground,
  font,
  foregroundStyle,
  lineLimit,
  minimumScaleFactor,
  padding,
  truncationMode,
  widgetURL,
} from '@expo/ui/swift-ui/modifiers';
import { createWidget } from 'expo-widgets';
import type { WidgetProps } from './widgetCopy';

/**
 * Props pushed from the app via `syncHomeWidget` (see widgetSync.tsx). Both halves are
 * optional and independent — either can be missing (no event today, no published quotes).
 * When both exist the widget shows one face at a time on a 4-hour UTC slot; quote
 * reloads every few hours with jitter; on-this-day is only refetched on a new
 * calendar day.
 *
 * `createWidget` still has to store a layout string so `updateTimeline` can
 * write props into the app group. The home-screen pixels come from native
 * SwiftUI (`plugins/withIosOnThisDayNativeWidget.cjs`) because WidgetKit's
 * gallery snapshot never runs this JS — a missing JSC layout is a black
 * EmptyView in Release/TestFlight. The crest watermark lives in that native
 * view; Expo UI Image is SF Symbols only.
 *
 * JSC cannot import widgetCopy or read widgetFamily, so the small-ceiling
 * numbers (17 / 9 / 0.65 / 6 / 2) are inlined here. Native picks 17 vs 22.
 */
export type OnThisDayWidgetProps = WidgetProps;

function OnThisDayWidgetView(props: OnThisDayWidgetProps) {
  'widget';

  const hasDay = Boolean(props.formattedDate && props.summary);
  const hasQuote = Boolean(props.quoteText && props.quoteWhoSaid);
  const slot = Math.floor(Date.now() / (4 * 60 * 60 * 1000));
  const showDay = hasDay && hasQuote ? slot % 2 === 0 : hasDay;
  const showQuote = hasDay && hasQuote ? slot % 2 !== 0 : hasQuote;
  const quoteId = Number(props.quoteId);
  const tapUrl =
    showQuote && quoteId > 0 ? `queenzone://quotes/${quoteId}` : 'queenzone://home';

  return (
    <VStack
      alignment="leading"
      spacing={6}
      modifiers={[
        padding({ all: 14 }),
        containerBackground('#181614', 'widget'),
        widgetURL(tapUrl),
      ]}
    >
      <Text modifiers={[foregroundStyle('#B89A4A'), font({ size: 10, weight: 'semibold' })]}>
        {showDay || (!hasDay && !hasQuote) ? 'ON THIS DAY' : 'QUEEN QUOTES'}
      </Text>
      {showDay ? (
        <Text
          modifiers={[
            foregroundStyle('#F2F1ED'),
            font({ size: 17 }),
            minimumScaleFactor(0.65),
            lineLimit(6),
            truncationMode('tail'),
          ]}
        >
          {props.summary}
        </Text>
      ) : null}
      {showDay ? (
        <Text modifiers={[foregroundStyle('#B8B6B0'), font({ size: 9 }), lineLimit(2), truncationMode('tail')]}>
          {props.formattedDate}
        </Text>
      ) : null}
      {showQuote ? (
        <Text
          modifiers={[
            foregroundStyle('#B8B6B0'),
            font({ size: 17 }),
            minimumScaleFactor(0.65),
            lineLimit(6),
            truncationMode('tail'),
          ]}
        >
          {`“${props.quoteText}”`}
        </Text>
      ) : null}
      {showQuote ? (
        <Text modifiers={[foregroundStyle('#B8B6B0'), font({ size: 9 }), lineLimit(2), truncationMode('tail')]}>
          {`— ${props.quoteWhoSaid}`}
        </Text>
      ) : null}
      {!hasDay && !hasQuote ? (
        <Text
          modifiers={[
            foregroundStyle('#B8B6B0'),
            font({ size: 17 }),
            minimumScaleFactor(0.65),
            lineLimit(6),
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
