import assert from 'node:assert/strict';
import { beforeEach, describe, it } from 'node:test';
import {
  consumeInitialWidgetUrl,
  isWidgetDeepLinkUrl,
  openWidgetDestination,
  resetInitialWidgetUrlConsumption,
  widgetDeepLinkUrl,
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
  });

  it('rejects other schemes, hosts, and malformed input', () => {
    assert.equal(isWidgetDeepLinkUrl('https://queenzone.com/home'), false);
    assert.equal(isWidgetDeepLinkUrl('queenzone://forum'), false);
    assert.equal(isWidgetDeepLinkUrl('queenzone://smoke-auth?access_token=x'), false);
    assert.equal(isWidgetDeepLinkUrl('not a url'), false);
  });

  it('navigates through the Tabs root into HomeTab/Home', () => {
    const navigate = (name: string, params: unknown) => {
      assert.equal(name, 'Tabs');
      assert.deepEqual(params, {
        screen: 'HomeTab',
        params: { screen: 'Home', initial: false },
      });
    };
    openWidgetDestination({ navigate: navigate as never });
  });

  it('consumes the launch widget URL once and leaves other schemes alone', () => {
    assert.equal(consumeInitialWidgetUrl('queenzone://smoke-auth?access_token=x'), null);
    assert.equal(consumeInitialWidgetUrl('queenzone://home'), 'queenzone://home');
    assert.equal(consumeInitialWidgetUrl('queenzone://home'), null);
    assert.equal(consumeInitialWidgetUrl('queenzone://timeline'), null);
  });
});
