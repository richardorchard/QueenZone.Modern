import { Platform } from 'react-native';
import type { WidgetTaskHandlerProps } from 'react-native-android-widget';
import { fetchOnThisDay, fetchRandomQuote } from '../api/content';
import {
  readCachedWidgetProps,
  readLastWidgetRefreshAt,
  writeCachedWidgetProps,
  writeLastWidgetRefreshAt,
} from './widgetCache';
import { widgetTaskHandler } from './widgetTaskHandler';

jest.mock('./widgetCache', () => ({
  readCachedWidgetProps: jest.fn(),
  writeCachedWidgetProps: jest.fn().mockResolvedValue(undefined),
  writeLastWidgetRefreshAt: jest.fn().mockResolvedValue(undefined),
  readLastWidgetRefreshAt: jest.fn().mockResolvedValue(null),
}));

jest.mock('../api/content', () => ({
  fetchOnThisDay: jest.fn(),
  fetchRandomQuote: jest.fn(),
}));

jest.mock('./OnThisDayAndroidWidget', () => ({
  OnThisDayAndroidWidget: (props: { quoteText?: string }) => props.quoteText ?? 'empty',
}));

jest.mock('react-native-android-widget', () => ({
  requestWidgetUpdate: jest.fn().mockResolvedValue(undefined),
}));

const readCached = readCachedWidgetProps as jest.MockedFunction<typeof readCachedWidgetProps>;
const writeCached = writeCachedWidgetProps as jest.MockedFunction<typeof writeCachedWidgetProps>;
const writeRefreshAt = writeLastWidgetRefreshAt as jest.MockedFunction<typeof writeLastWidgetRefreshAt>;
const readRefreshAt = readLastWidgetRefreshAt as jest.MockedFunction<typeof readLastWidgetRefreshAt>;
const fetchDay = fetchOnThisDay as jest.MockedFunction<typeof fetchOnThisDay>;
const fetchQuote = fetchRandomQuote as jest.MockedFunction<typeof fetchRandomQuote>;

const cachedProps = { quoteText: 'A kind of magic', quoteWhoSaid: 'Freddie Mercury' };

function handlerProps(action: WidgetTaskHandlerProps['widgetAction']): WidgetTaskHandlerProps {
  return {
    widgetInfo: { widgetName: 'OnThisDayWidget', widgetId: 1, height: 110, width: 180 },
    widgetAction: action,
    renderWidget: jest.fn(),
  } as unknown as WidgetTaskHandlerProps;
}

describe('widgetTaskHandler', () => {
  const originalOs = Platform.OS;

  beforeEach(() => {
    Object.defineProperty(Platform, 'OS', { value: 'android' });
    readCached.mockReset();
    readCached.mockResolvedValue(cachedProps);
    writeCached.mockClear();
    writeRefreshAt.mockClear();
    readRefreshAt.mockReset();
    readRefreshAt.mockResolvedValue(null);
    fetchDay.mockReset();
    fetchQuote.mockReset();
  });

  afterEach(() => {
    Object.defineProperty(Platform, 'OS', { value: originalOs });
  });

  it.each(['WIDGET_ADDED', 'WIDGET_RESIZED'] as const)(
    'renders cached props for %s without fetching',
    async (action) => {
      const props = handlerProps(action);
      await widgetTaskHandler(props);
      expect(fetchDay).not.toHaveBeenCalled();
      expect(fetchQuote).not.toHaveBeenCalled();
      expect(readCached).toHaveBeenCalled();
      expect(props.renderWidget).toHaveBeenCalled();
    },
  );

  it('fetches on WIDGET_UPDATE and renders the new snapshot', async () => {
    fetchDay.mockResolvedValue({
      id: 1,
      title: 'The Game',
      summary: 'Queen released The Game.',
      eventDate: '1980-06-30',
      formattedDate: '30 June 1980',
      category: 'music',
      categoryLabel: 'Release',
      sourceUrl: null,
    });
    fetchQuote.mockResolvedValue({ id: 2, text: 'New quote', whoSaid: 'Brian May' });
    readCached.mockResolvedValueOnce(cachedProps).mockResolvedValue({
      formattedDate: '30 June 1980',
      summary: 'Queen released The Game.',
      quoteText: 'New quote',
      quoteWhoSaid: 'Brian May',
    });

    const props = handlerProps('WIDGET_UPDATE');
    await widgetTaskHandler(props);

    expect(fetchDay).toHaveBeenCalledTimes(1);
    expect(fetchQuote).toHaveBeenCalledTimes(1);
    expect(writeCached).toHaveBeenCalledWith({
      formattedDate: '30 June 1980',
      summary: 'Queen released The Game.',
      quoteText: 'New quote',
      quoteWhoSaid: 'Brian May',
      quoteId: 2,
    });
    expect(props.renderWidget).toHaveBeenCalled();
  });

  it('keeps the last cached snapshot when WIDGET_UPDATE fetch fails', async () => {
    fetchDay.mockRejectedValue(new Error('offline'));
    fetchQuote.mockRejectedValue(new Error('offline'));

    const props = handlerProps('WIDGET_UPDATE');
    await widgetTaskHandler(props);

    expect(writeCached).not.toHaveBeenCalled();
    expect(readCached).toHaveBeenCalled();
    expect(props.renderWidget).toHaveBeenCalled();
  });

  it('keeps the last good quote when the quote fetch fails', async () => {
    fetchDay.mockResolvedValue({
      id: 1,
      title: 'The Game',
      summary: 'Queen released The Game.',
      eventDate: '1980-06-30',
      formattedDate: '30 June 1980',
      category: 'music',
      categoryLabel: 'Release',
      sourceUrl: null,
    });
    fetchQuote.mockRejectedValue(new Error('offline'));

    const props = handlerProps('WIDGET_UPDATE');
    await widgetTaskHandler(props);

    expect(writeCached).toHaveBeenCalledWith({
      formattedDate: '30 June 1980',
      summary: 'Queen released The Game.',
      quoteText: 'A kind of magic',
      quoteWhoSaid: 'Freddie Mercury',
    });
    expect(props.renderWidget).toHaveBeenCalled();
  });

  it('still fetches a quote when on-this-day is skipped for the same day', async () => {
    const now = 1_700_000_000_000;
    jest.spyOn(Date, 'now').mockReturnValue(now);
    readRefreshAt.mockResolvedValue(now - 4 * 60 * 60 * 1000);
    fetchQuote.mockResolvedValue({ id: 3, text: 'Rolled quote', whoSaid: 'Roger Taylor' });

    const props = handlerProps('WIDGET_UPDATE');
    await widgetTaskHandler(props);

    expect(fetchDay).not.toHaveBeenCalled();
    expect(fetchQuote).toHaveBeenCalledTimes(1);
    expect(writeCached).toHaveBeenCalledWith({
      formattedDate: undefined,
      summary: undefined,
      quoteText: 'Rolled quote',
      quoteWhoSaid: 'Roger Taylor',
      quoteId: 3,
    });
    expect(props.renderWidget).toHaveBeenCalled();
    jest.spyOn(Date, 'now').mockRestore();
  });

  it('does not render on delete', async () => {
    const props = handlerProps('WIDGET_DELETED');
    await widgetTaskHandler(props);
    expect(readCached).not.toHaveBeenCalled();
    expect(fetchDay).not.toHaveBeenCalled();
    expect(props.renderWidget).not.toHaveBeenCalled();
  });
});
