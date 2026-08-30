import assert from 'node:assert/strict';
import { beforeEach, describe, it } from 'node:test';
import {
  consumeInitialWidgetUrl,
  isWidgetDeepLinkUrl,
  openWidgetDestination,
  parseWidgetQuoteId,
  resetInitialWidgetUrlConsumption,
  widgetDeepLinkUrl,
  widgetFaceDeepLinkUrl,
  widgetQuoteDeepLinkUrl,
} from './widgetDeepLink.ts';

describe('widgetDeepLink', () => {
  beforeEach(() => {
    resetInitialWidgetUrlConsumption();
  });

  it('recognizes the widget URL by scheme and host', () => {
    assert.equal(isWidgetDeepLinkUrl(widgetDeepLinkUrl), true);
    assert.equal(isWidgetDeepLinkUrl('queenzone://home'), true);
    assert.equal(isWidgetDeepLinkUrl('queenzone://home/'), true);
    assert.equal(isWidgetDeepLinkUrl('queenzone://timeline'), true);
    assert.equal(isWidgetDeepLinkUrl('queenzone://timeline/'), true);
    assert.equal(isWidgetDeepLinkUrl('queenzone://quotes/9'), true);
    assert.equal(isWidgetDeepLinkUrl('queenzone://quotes/9/'), true);
  });

  it('rejects other schemes, hosts, and malformed input', () => {
    assert.equal(isWidgetDeepLinkUrl('https://queenzone.com/home'), false);
    assert.equal(isWidgetDeepLinkUrl('queenzone://forum'), false);
    assert.equal(isWidgetDeepLinkUrl('queenzone://smoke-auth?access_token=x'), false);
    assert.equal(isWidgetDeepLinkUrl('not a url'), false);
  });

  it('builds quote-face vs day-face URLs from the shown slot', () => {
    assert.equal(widgetFaceDeepLinkUrl('day', 9), 'queenzone://home');
    assert.equal(widgetFaceDeepLinkUrl('quote', 9), 'queenzone://quotes/9');
    assert.equal(widgetQuoteDeepLinkUrl(9), 'queenzone://quotes/9');
    assert.equal(widgetFaceDeepLinkUrl('quote', 0), 'queenzone://home');
    assert.equal(widgetFaceDeepLinkUrl('quote'), 'queenzone://home');
    assert.equal(widgetFaceDeepLinkUrl(null, 9), 'queenzone://home');
  });

  it('parses a positive integer quote id and rejects missing or invalid ids', () => {
    assert.equal(parseWidgetQuoteId('queenzone://quotes/9'), 9);
    assert.equal(parseWidgetQuoteId('queenzone://quotes/9/'), 9);
    assert.equal(parseWidgetQuoteId('queenzone://quotes/0'), null);
    assert.equal(parseWidgetQuoteId('queenzone://quotes/abc'), null);
    assert.equal(parseWidgetQuoteId('queenzone://quotes/'), null);
    assert.equal(parseWidgetQuoteId('queenzone://quotes'), null);
    assert.equal(parseWidgetQuoteId('queenzone://home'), null);
  });

  it('navigates through the Tabs root into HomeTab/Home', () => {
    const navigate = (name: string, params: unknown) => {
      assert.equal(name, 'Tabs');
      assert.deepEqual(params, {
        screen: 'HomeTab',
        params: { screen: 'Home', initial: false },
      });
    };
    openWidgetDestination({ navigate: navigate as never }, 'queenzone://home');
  });

  it('navigates a quote URL onto the Home stack Quote screen', () => {
    const navigate = (name: string, params: unknown) => {
      assert.equal(name, 'Tabs');
      assert.deepEqual(params, {
        screen: 'HomeTab',
        params: { screen: 'Quote', params: { id: 9 }, initial: false },
      });
    };
    openWidgetDestination({ navigate: navigate as never }, 'queenzone://quotes/9');
  });

  it('falls back to Home when the quote id is missing or not an integer', () => {
    const destinations: unknown[] = [];
    const navigate = (_name: string, params: unknown) => {
      destinations.push(params);
    };
    openWidgetDestination({ navigate: navigate as never }, 'queenzone://quotes/abc');
    openWidgetDestination({ navigate: navigate as never }, 'queenzone://quotes/0');
    assert.deepEqual(destinations, [
      { screen: 'HomeTab', params: { screen: 'Home', initial: false } },
      { screen: 'HomeTab', params: { screen: 'Home', initial: false } },
    ]);
  });

  it('consumes the launch widget URL once and leaves other schemes alone', () => {
    assert.equal(consumeInitialWidgetUrl('queenzone://smoke-auth?access_token=x'), null);
    assert.equal(consumeInitialWidgetUrl('queenzone://quotes/9'), 'queenzone://quotes/9');
    assert.equal(consumeInitialWidgetUrl('queenzone://quotes/9'), null);
    assert.equal(consumeInitialWidgetUrl('queenzone://home'), null);
  });
});
