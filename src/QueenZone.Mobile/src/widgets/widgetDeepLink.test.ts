import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { isWidgetTimelineUrl, openWidgetTimeline, widgetDeepLinkUrl } from './widgetDeepLink.ts';

describe('widgetDeepLink', () => {
  it('recognizes the widget URL by scheme and host', () => {
    assert.equal(isWidgetTimelineUrl(widgetDeepLinkUrl), true);
    assert.equal(isWidgetTimelineUrl('queenzone://timeline'), true);
    assert.equal(isWidgetTimelineUrl('queenzone://timeline/'), true);
  });

  it('rejects other schemes, hosts, and malformed input', () => {
    assert.equal(isWidgetTimelineUrl('https://queenzone.com/timeline'), false);
    assert.equal(isWidgetTimelineUrl('queenzone://forum'), false);
    assert.equal(isWidgetTimelineUrl('not a url'), false);
  });

  it('navigates through the Tabs root into ArchiveTab/Timeline', () => {
    const navigate = (name: string, params: unknown) => {
      assert.equal(name, 'Tabs');
      assert.deepEqual(params, {
        screen: 'ArchiveTab',
        params: { screen: 'Timeline', params: {}, initial: false },
      });
    };
    openWidgetTimeline({ navigate: navigate as never });
  });
});
