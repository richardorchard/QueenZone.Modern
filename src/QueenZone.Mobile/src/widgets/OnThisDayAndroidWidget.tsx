'use no memo';
import { FlexWidget, TextWidget } from 'react-native-android-widget';
import { widgetDeepLinkUrl } from './widgetDeepLink';

/**
 * Props rendered into the widget's bitmap (see limitations.md — the library rasterizes this
 * tree, it does not host live RN views). Kept in sync with OnThisDayWidget.ios.tsx's props.
 */
export type OnThisDayAndroidWidgetProps = {
  formattedDate?: string;
  summary?: string;
  quoteText?: string;
  quoteWhoSaid?: string;
};

const gold = '#B89A4A';
const cream = '#F2F1ED';
const mutedCream = '#B8B6B0';
const cardBackground = '#181614';

export function OnThisDayAndroidWidget(props: OnThisDayAndroidWidgetProps) {
  const hasDay = Boolean(props.formattedDate && props.summary);
  const hasQuote = Boolean(props.quoteText && props.quoteWhoSaid);

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
        text={hasDay ? 'ON THIS DAY' : 'QUOTE OF THE DAY'}
        style={{ fontSize: 10, fontWeight: '600', color: gold, marginBottom: 6 }}
      />
      {hasDay ? (
        <TextWidget
          text={`${props.formattedDate}: ${props.summary}`}
          style={{ fontSize: 13, color: cream, marginBottom: 6 }}
          maxLines={3}
        />
      ) : null}
      {hasQuote ? (
        <TextWidget
          text={`“${props.quoteText}” — ${props.quoteWhoSaid}`}
          style={{ fontSize: 12, color: mutedCream }}
          maxLines={3}
        />
      ) : null}
      {!hasDay && !hasQuote ? (
        <TextWidget text="Open QueenZone to load today's story." style={{ fontSize: 12, color: mutedCream }} />
      ) : null}
    </FlexWidget>
  );
}
