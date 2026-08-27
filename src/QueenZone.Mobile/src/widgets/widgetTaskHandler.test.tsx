import type { WidgetTaskHandlerProps } from 'react-native-android-widget';
import { readCachedWidgetProps } from './widgetCache';
import { widgetTaskHandler } from './widgetTaskHandler';

jest.mock('./widgetCache', () => ({
  readCachedWidgetProps: jest.fn(),
}));

jest.mock('./OnThisDayAndroidWidget', () => ({
  OnThisDayAndroidWidget: (props: { quoteText?: string }) => props.quoteText ?? 'empty',
}));

const readCached = readCachedWidgetProps as jest.MockedFunction<typeof readCachedWidgetProps>;

function handlerProps(action: WidgetTaskHandlerProps['widgetAction']): WidgetTaskHandlerProps {
  return {
    widgetInfo: { widgetName: 'OnThisDayWidget', widgetId: 1, height: 110, width: 180 },
    widgetAction: action,
    renderWidget: jest.fn(),
  } as unknown as WidgetTaskHandlerProps;
}

describe('widgetTaskHandler', () => {
  beforeEach(() => {
    readCached.mockReset();
    readCached.mockResolvedValue({ quoteText: 'A kind of magic', quoteWhoSaid: 'Freddie Mercury' });
  });

  it.each(['WIDGET_ADDED', 'WIDGET_UPDATE', 'WIDGET_RESIZED'] as const)(
    'renders cached props for %s',
    async (action) => {
      const props = handlerProps(action);
      await widgetTaskHandler(props);
      expect(readCached).toHaveBeenCalled();
      expect(props.renderWidget).toHaveBeenCalled();
    },
  );

  it('does not render on delete', async () => {
    const props = handlerProps('WIDGET_DELETED');
    await widgetTaskHandler(props);
    expect(readCached).not.toHaveBeenCalled();
    expect(props.renderWidget).not.toHaveBeenCalled();
  });
});
