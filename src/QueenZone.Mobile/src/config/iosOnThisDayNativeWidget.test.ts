import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);
const {
  applyOnThisDayNativeWidget,
  GENERATED_ENTRY,
  NATIVE_ENTRY,
  TAG,
} = require('../../plugins/withIosOnThisDayNativeWidget.cjs') as {
  applyOnThisDayNativeWidget: (contents: string) => string;
  GENERATED_ENTRY: string;
  NATIVE_ENTRY: string;
  TAG: string;
};

const generatedSwift = `import WidgetKit
import SwiftUI
internal import ExpoWidgets

struct OnThisDayWidget: Widget {
  let name: String = "OnThisDayWidget"

  var body: some WidgetConfiguration {
    StaticConfiguration(kind: name, provider: WidgetsTimelineProvider(name: name)) { entry in
      WidgetsEntryView(entry: entry)
    }
    .configurationDisplayName("On This Day")
    .description("A Queen history highlight and a rolling random quote.")
    .supportedFamilies([.systemSmall, .systemMedium])
  }
}
`;

describe('applyOnThisDayNativeWidget', () => {
  it('replaces the JSC entry view with native SwiftUI once', () => {
    const first = applyOnThisDayNativeWidget(generatedSwift);
    assert.equal(first.includes(GENERATED_ENTRY), false);
    assert.equal(first.includes(NATIVE_ENTRY), true);
    assert.match(first, /struct OnThisDayNativeEntryView/);
    assert.match(first, /Open QueenZone to load today's story\./);
    assert.match(first, /queenzone:\/\/home/);
    assert.match(first, /containerBackground/);
    assert.match(first, /foregroundColor/);
    assert.equal(first.includes(TAG), true);

    const second = applyOnThisDayNativeWidget(first);
    assert.equal(second, first);
  });

  it('fails closed when the expo-widgets template no longer uses WidgetsEntryView', () => {
    assert.throws(
      () => applyOnThisDayNativeWidget('struct OnThisDayWidget: Widget {}'),
      /template may have changed/,
    );
  });
});
