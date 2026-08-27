import { Platform } from 'react-native';
import { syncHomeWidget } from './widgetSync';
import { writeCachedWidgetProps } from './widgetCache';

const mockUpdateSnapshot = jest.fn();
const mockRequestWidgetUpdate = jest.fn().mockResolvedValue(undefined);

jest.mock('./widgetCache', () => ({
  writeCachedWidgetProps: jest.fn().mockResolvedValue(undefined),
}));

jest.mock('./OnThisDayWidget.ios', () => ({
  OnThisDayWidget: { updateSnapshot: (...args: unknown[]) => mockUpdateSnapshot(...args) },
}));

jest.mock('react-native-android-widget', () => ({
  requestWidgetUpdate: (...args: unknown[]) => mockRequestWidgetUpdate(...args),
}));

jest.mock('./OnThisDayAndroidWidget', () => ({
  OnThisDayAndroidWidget: () => null,
}));

const writeCached = writeCachedWidgetProps as jest.MockedFunction<typeof writeCachedWidgetProps>;

const content = {
  onThisDay: {
    id: 1,
    title: 'The Game',
    summary: 'Queen released The Game.',
    eventDate: '1980-06-30',
    formattedDate: '30 June 1980',
    category: 'music',
    categoryLabel: 'Release',
    sourceUrl: null,
  },
  quote: { id: 9, text: 'A kind of magic', whoSaid: 'Freddie Mercury' },
};

describe('syncHomeWidget', () => {
  const originalOs = Platform.OS;

  afterEach(() => {
    Object.defineProperty(Platform, 'OS', { value: originalOs });
    mockUpdateSnapshot.mockClear();
    mockRequestWidgetUpdate.mockClear();
    writeCached.mockClear();
  });

  it('pushes a snapshot on iOS', async () => {
    Object.defineProperty(Platform, 'OS', { value: 'ios' });
    await syncHomeWidget(content);
    expect(mockUpdateSnapshot).toHaveBeenCalledWith({
      formattedDate: '30 June 1980',
      summary: 'Queen released The Game.',
      quoteText: 'A kind of magic',
      quoteWhoSaid: 'Freddie Mercury',
    });
    expect(mockRequestWidgetUpdate).not.toHaveBeenCalled();
  });

  it('caches props and requests an Android update', async () => {
    Object.defineProperty(Platform, 'OS', { value: 'android' });
    await syncHomeWidget(content);
    expect(writeCached).toHaveBeenCalledWith({
      formattedDate: '30 June 1980',
      summary: 'Queen released The Game.',
      quoteText: 'A kind of magic',
      quoteWhoSaid: 'Freddie Mercury',
    });
    expect(mockRequestWidgetUpdate).toHaveBeenCalledWith(
      expect.objectContaining({ widgetName: 'OnThisDayWidget' }),
    );
    expect(mockUpdateSnapshot).not.toHaveBeenCalled();
  });
});
