/**
 * Replace the expo-widgets JSC entry view with native SwiftUI.
 *
 * WidgetKit's gallery snapshot and a fresh home-screen add run inside the
 * extension process. They never execute the app's JS, so `createWidget` has
 * not stored a layout yet. expo-widgets 57 then renders EmptyView in Release
 * (TestFlight) — a black box. Reading the same app-group timeline props in
 * SwiftUI works before the first app launch (empty-state copy) and after
 * Home / background refresh writes real on-this-day + quote data.
 */
const fs = require('fs');
const path = require('path');
const { createRunOncePlugin, withXcodeProject } = require('expo/config-plugins');

const TAG = 'queenzone-on-this-day-native-widget';
const WIDGET_RELATIVE_PATH = path.join('ExpoWidgetsTarget', 'OnThisDayWidget.swift');
const GENERATED_ENTRY = 'WidgetsEntryView(entry: entry)';
const NATIVE_ENTRY = 'OnThisDayNativeEntryView(entry: entry)';

const NATIVE_VIEW_SOURCE = `
struct OnThisDayNativeEntryView: View {
  var entry: WidgetsTimelineEntry

  private var formattedDate: String { stringProp("formattedDate") }
  private var summary: String { stringProp("summary") }
  private var quoteText: String { stringProp("quoteText") }
  private var quoteWhoSaid: String { stringProp("quoteWhoSaid") }
  private var hasDay: Bool { !formattedDate.isEmpty && !summary.isEmpty }
  private var hasQuote: Bool { !quoteText.isEmpty && !quoteWhoSaid.isEmpty }

  var body: some View {
    let card = VStack(alignment: .leading, spacing: 6) {
      Text(hasDay ? "ON THIS DAY" : "QUOTE")
        .font(.system(size: 10, weight: .semibold))
        .foregroundColor(Color(red: 184 / 255, green: 154 / 255, blue: 74 / 255))
      if hasDay {
        Text("\\(formattedDate): \\(summary)")
          .font(.system(size: 13))
          .foregroundColor(Color(red: 242 / 255, green: 241 / 255, blue: 237 / 255))
          .lineLimit(3)
      }
      if hasQuote {
        Text("“\\(quoteText)” — \\(quoteWhoSaid)")
          .font(.system(size: 12))
          .foregroundColor(Color(red: 184 / 255, green: 182 / 255, blue: 176 / 255))
          .lineLimit(3)
      }
      if !hasDay && !hasQuote {
        Text("Open QueenZone to load today's story.")
          .font(.system(size: 12))
          .foregroundColor(Color(red: 184 / 255, green: 182 / 255, blue: 176 / 255))
          .lineLimit(3)
      }
    }
    .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .leading)
    .padding(14)
    .widgetURL(URL(string: "queenzone://home"))

    if #available(iOS 17.0, *) {
      card.containerBackground(
        Color(red: 24 / 255, green: 22 / 255, blue: 20 / 255),
        for: .widget
      )
    } else {
      card.background(Color(red: 24 / 255, green: 22 / 255, blue: 20 / 255))
    }
  }

  private func stringProp(_ key: String) -> String {
    (entry.props?[key] as? String) ?? ""
  }
}
`.trim();

function applyOnThisDayNativeWidget(contents) {
  if (contents.includes(TAG)) {
    return contents;
  }
  if (!contents.includes(GENERATED_ENTRY)) {
    throw new Error(
      `OnThisDayWidget.swift does not contain ${GENERATED_ENTRY}. expo-widgets template may have changed.`,
    );
  }

  const replaced = contents.replace(GENERATED_ENTRY, NATIVE_ENTRY);
  return `${replaced.trimEnd()}\n\n// @generated begin ${TAG} - expo prebuild\n${NATIVE_VIEW_SOURCE}\n// @generated end ${TAG}\n`;
}

function withIosOnThisDayNativeWidget(config) {
  return withXcodeProject(config, (mod) => {
    const widgetPath = path.join(mod.modRequest.platformProjectRoot, WIDGET_RELATIVE_PATH);
    if (!fs.existsSync(widgetPath)) {
      throw new Error(
        `Missing ${WIDGET_RELATIVE_PATH} — expo-widgets must generate ExpoWidgetsTarget before this plugin.`,
      );
    }
    fs.writeFileSync(widgetPath, applyOnThisDayNativeWidget(fs.readFileSync(widgetPath, 'utf8')));
    return mod;
  });
}

const plugin = createRunOncePlugin(
  withIosOnThisDayNativeWidget,
  'withIosOnThisDayNativeWidget',
  '1.0.0',
);

plugin.applyOnThisDayNativeWidget = applyOnThisDayNativeWidget;
plugin.GENERATED_ENTRY = GENERATED_ENTRY;
plugin.NATIVE_ENTRY = NATIVE_ENTRY;
plugin.TAG = TAG;
module.exports = plugin;
