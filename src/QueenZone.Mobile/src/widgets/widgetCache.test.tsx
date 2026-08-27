import AsyncStorage from '@react-native-async-storage/async-storage';
import { readCachedWidgetProps, writeCachedWidgetProps } from './widgetCache';

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
});
