'use no memo';
import { FlexWidget, TextWidget } from 'react-native-android-widget';
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
 * Props rendered into the widget's bitmap (the library rasterizes this tree, it does not
 * host live RN views). Same shape as OnThisDayWidget.ios.tsx.
 */
export type OnThisDayAndroidWidgetProps = WidgetProps;

const gold = '#B89A4A';
const cream = '#F2F1ED';
const mutedCream = '#B8B6B0';
const cardBackground = '#181614';

export function OnThisDayAndroidWidget(props: OnThisDayAndroidWidgetProps) {
  const hasDay = widgetHasDay(props);
  const hasQuote = widgetHasQuote(props);

  return (
    <FlexWidget
      clickAction="OPEN_URI"
      clickActionData={{ uri: widgetDeepLinkUrl }}
      accessibilityLabel="QueenZone on this day widget"
      style={{
        height: 'match_parent',
        width: 'match_parent',
        flexDirection: 'column',
        justifyContent: 'center',
        padding: 14,
        backgroundColor: cardBackground,
        borderRadius: 16,
      }}
    >
      <TextWidget
        text={widgetEyebrow(hasDay)}
        style={{ fontSize: 10, fontWeight: '600', color: gold, marginBottom: 6 }}
      />
      {hasDay ? (
        <TextWidget
          text={widgetDayText(props)}
          style={{ fontSize: 13, color: cream, marginBottom: 6 }}
          maxLines={3}
        />
      ) : null}
      {hasQuote ? (
        <TextWidget text={widgetQuoteText(props)} style={{ fontSize: 12, color: mutedCream }} maxLines={3} />
      ) : null}
      {!hasDay && !hasQuote ? (
        <TextWidget text={widgetEmptyText} style={{ fontSize: 12, color: mutedCream }} />
      ) : null}
    </FlexWidget>
  );
}
