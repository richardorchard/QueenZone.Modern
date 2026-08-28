'use no memo';
import { FlexWidget, ImageWidget, OverlapWidget, TextWidget } from 'react-native-android-widget';
import {
  widgetActiveFace,
  widgetDayText,
  widgetEmptyText,
  widgetEyebrow,
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
  const face = widgetActiveFace(props);

  return (
    <OverlapWidget
      clickAction="OPEN_URI"
      clickActionData={{ uri: widgetDeepLinkUrl }}
      accessibilityLabel="QueenZone on this day widget"
      style={{
        height: 'match_parent',
        width: 'match_parent',
        backgroundColor: cardBackground,
        borderRadius: 16,
        overflow: 'hidden',
      }}
    >
      <FlexWidget
        style={{
          height: 'match_parent',
          width: 'match_parent',
          justifyContent: 'flex-end',
          alignItems: 'flex-end',
          padding: 4,
        }}
      >
        <ImageWidget
          image={require('../../assets/archive/crest-widget-watermark.png')}
          imageWidth={120}
          imageHeight={120}
          resizeMode="contain"
        />
      </FlexWidget>
      <FlexWidget
        style={{
          height: 'match_parent',
          width: 'match_parent',
          flexDirection: 'column',
          justifyContent: 'center',
          padding: 14,
        }}
      >
        {face ? (
          <TextWidget
            text={widgetEyebrow(face)}
            style={{ fontSize: 10, fontWeight: '600', color: gold, marginBottom: 6 }}
          />
        ) : (
          <TextWidget
            text="ON THIS DAY"
            style={{ fontSize: 10, fontWeight: '600', color: gold, marginBottom: 6 }}
          />
        )}
        {face === 'day' ? (
          <TextWidget text={widgetDayText(props)} style={{ fontSize: 13, color: cream }} maxLines={3} />
        ) : null}
        {face === 'quote' ? (
          <TextWidget text={widgetQuoteText(props)} style={{ fontSize: 12, color: mutedCream }} maxLines={3} />
        ) : null}
        {face == null ? <TextWidget text={widgetEmptyText} style={{ fontSize: 12, color: mutedCream }} /> : null}
      </FlexWidget>
    </OverlapWidget>
  );
}
