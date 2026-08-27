import AsyncStorage from '@react-native-async-storage/async-storage';
import {
  readCachedWidgetProps,
  readLastWidgetRefreshAt,
  writeCachedWidgetProps,
  writeLastWidgetRefreshAt,
} from './widgetCache';

describe('widgetCache', () => {
  afterEach(async () => {
    await AsyncStorage.clear();
  });

  it('round-trips widget props', async () => {
    await writeCachedWidgetProps({
      formattedDate: '30 June 1980',
      summary: 'Queen released The Game.',
      quoteText: 'A kind of magic',
      quoteWhoSaid: 'Freddie Mercury',
    });

    await expect(readCachedWidgetProps()).resolves.toEqual({
      formattedDate: '30 June 1980',
      summary: 'Queen released The Game.',
      quoteText: 'A kind of magic',
      quoteWhoSaid: 'Freddie Mercury',
    });
  });

  it('returns empty props when nothing is cached', async () => {
    await expect(readCachedWidgetProps()).resolves.toEqual({});
  });

  it('returns empty props when the cache is not JSON', async () => {
    await AsyncStorage.setItem('widget:onThisDay:v1', '{not-json');
    await expect(readCachedWidgetProps()).resolves.toEqual({});
  });

  it('round-trips the last refresh timestamp', async () => {
    await writeLastWidgetRefreshAt(1_700_000_000_000);
    await expect(readLastWidgetRefreshAt()).resolves.toBe(1_700_000_000_000);
  });

  it('returns null when no refresh has been recorded', async () => {
    await expect(readLastWidgetRefreshAt()).resolves.toBeNull();
  });

  it('returns null when the refresh timestamp is not a number', async () => {
    await AsyncStorage.setItem('widget:onThisDay:refreshAt:v1', 'not-a-number');
    await expect(readLastWidgetRefreshAt()).resolves.toBeNull();
  });
});
