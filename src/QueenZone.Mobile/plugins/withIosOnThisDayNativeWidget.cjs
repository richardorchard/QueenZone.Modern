/**
 * Replace the expo-widgets JSC entry view with native SwiftUI.
 *
 * WidgetKit's gallery snapshot and a fresh home-screen add run inside the
 * extension process. They never execute the app's JS, so `createWidget` has
 * not stored a layout yet. expo-widgets 57 then renders EmptyView in Release
 * (TestFlight) — a black box. Reading the same app-group timeline props in
 * SwiftUI works before the first app launch (empty-state copy) and after
 * Home / background refresh writes real on-this-day + quote data.
 *
 * When both halves are present the native view shows one face at a time on a
 * 4-hour UTC slot (`entry.date`), matching `widgetActiveFace` in widgetCopy.ts.
 * Primary/secondary type-scale literals match widgetCopy.ts (17/22, 0.65, 6, 9/11, 2).
 */
const fs = require('fs');
const path = require('path');
const { createRunOncePlugin, withXcodeProject } = require('expo/config-plugins');

const TAG = 'queenzone-on-this-day-native-widget';
const WIDGET_RELATIVE_PATH = path.join('ExpoWidgetsTarget', 'OnThisDayWidget.swift');
const GENERATED_ENTRY = 'WidgetsEntryView(entry: entry)';
const NATIVE_ENTRY = 'OnThisDayNativeEntryView(entry: entry)';
const CREST_ASSET_NAME = 'crest-widget-watermark.png';
const CREST_SOURCE_RELATIVE = path.join('assets', 'archive', CREST_ASSET_NAME);

const NATIVE_VIEW_SOURCE = `
struct OnThisDayNativeEntryView: View {
  var entry: WidgetsTimelineEntry
  @Environment(\\.widgetFamily) private var family

  private var formattedDate: String { stringProp("formattedDate") }
  private var summary: String { stringProp("summary") }
  private var quoteText: String { stringProp("quoteText") }
  private var quoteWhoSaid: String { stringProp("quoteWhoSaid") }
  private var quoteId: Int { intProp("quoteId") }
  private var eventId: Int { intProp("eventId") }
  private var hasDay: Bool { !formattedDate.isEmpty && !summary.isEmpty }
  private var hasQuote: Bool { !quoteText.isEmpty && !quoteWhoSaid.isEmpty }
  private var showDay: Bool {
    if hasDay && hasQuote {
      let slot = Int(floor(entry.date.timeIntervalSince1970 / (4 * 3600)))
      return slot % 2 == 0
    }
    return hasDay
  }
  private var showQuote: Bool {
    if hasDay && hasQuote {
      return !showDay
    }
    return hasQuote
  }
  private var primaryCeiling: CGFloat { family == .systemMedium ? 22 : 17 }
  private var secondaryPt: CGFloat { family == .systemMedium ? 11 : 9 }

  var body: some View {
    let card = ZStack(alignment: .bottomTrailing) {
      if let crest = UIImage(named: "crest-widget-watermark") {
        Image(uiImage: crest)
          .resizable()
          .scaledToFit()
          .frame(width: 120, height: 120)
          .padding(8)
          .accessibilityHidden(true)
      }
      VStack(alignment: .leading, spacing: 6) {
        Text(showDay || (!hasDay && !hasQuote) ? "ON THIS DAY" : "QUEEN QUOTES")
          .font(.system(size: 10, weight: .semibold))
          .foregroundColor(Color(red: 184 / 255, green: 154 / 255, blue: 74 / 255))
        if showDay {
          Text(summary)
            .font(.system(size: primaryCeiling))
            .foregroundColor(Color(red: 242 / 255, green: 241 / 255, blue: 237 / 255))
            .minimumScaleFactor(0.65)
            .lineLimit(6)
            .truncationMode(.tail)
            .frame(maxHeight: .infinity, alignment: .topLeading)
          Text(formattedDate)
            .font(.system(size: secondaryPt))
            .foregroundColor(Color(red: 184 / 255, green: 182 / 255, blue: 176 / 255))
            .lineLimit(2)
        }
        if showQuote {
          Text("“\\(quoteText)”")
            .font(.system(size: primaryCeiling))
            .foregroundColor(Color(red: 184 / 255, green: 182 / 255, blue: 176 / 255))
            .minimumScaleFactor(0.65)
            .lineLimit(6)
            .truncationMode(.tail)
            .frame(maxHeight: .infinity, alignment: .topLeading)
          Text("— \\(quoteWhoSaid)")
            .font(.system(size: secondaryPt))
            .foregroundColor(Color(red: 184 / 255, green: 182 / 255, blue: 176 / 255))
            .lineLimit(2)
        }
        if !hasDay && !hasQuote {
          Text("Open QueenZone to load today's story.")
            .font(.system(size: primaryCeiling))
            .foregroundColor(Color(red: 184 / 255, green: 182 / 255, blue: 176 / 255))
            .minimumScaleFactor(0.65)
            .lineLimit(6)
            .truncationMode(.tail)
            .frame(maxHeight: .infinity, alignment: .topLeading)
        }
      }
      .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .leading)
      .padding(14)
    }
    .widgetURL(tapURL)

    if #available(iOS 17.0, *) {
      card.containerBackground(
        Color(red: 24 / 255, green: 22 / 255, blue: 20 / 255),
        for: .widget
      )
    } else {
      card.background(Color(red: 24 / 255, green: 22 / 255, blue: 20 / 255))
    }
  }

  private var tapURL: URL? {
    if showQuote && quoteId > 0 {
      return URL(string: "queenzone://quotes/\\(quoteId)")
    }
    if showDay && eventId > 0 {
      return URL(string: "queenzone://timeline/\\(eventId)")
    }
    if showQuote {
      return URL(string: "queenzone://home")
    }
    return URL(string: "queenzone://timeline")
  }

  private func stringProp(_ key: String) -> String {
    (entry.props?[key] as? String) ?? ""
  }

  private func intProp(_ key: String) -> Int {
    if let number = entry.props?[key] as? Int {
      return number
    }
    if let number = entry.props?[key] as? Double {
      return Int(number)
    }
    if let text = entry.props?[key] as? String, let number = Int(text) {
      return number
    }
    return 0
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

  let replaced = contents.replace(GENERATED_ENTRY, NATIVE_ENTRY);
  if (!replaced.includes('import UIKit')) {
    replaced = replaced.replace(/^import SwiftUI$/m, 'import SwiftUI\nimport UIKit');
  }
  return `${replaced.trimEnd()}\n\n// @generated begin ${TAG} - expo prebuild\n${NATIVE_VIEW_SOURCE}\n// @generated end ${TAG}\n`;
}

function copyWidgetCrestAsset(projectRoot, platformProjectRoot) {
  const src = path.join(projectRoot, CREST_SOURCE_RELATIVE);
  if (!fs.existsSync(src)) {
    throw new Error(`Missing widget crest asset ${src}`);
  }
  const destDir = path.join(platformProjectRoot, 'ExpoWidgetsTarget');
  fs.mkdirSync(destDir, { recursive: true });
  const dest = path.join(destDir, CREST_ASSET_NAME);
  fs.copyFileSync(src, dest);
  return dest;
}

function widgetTargetUuid(project) {
  const nativeTargets = project.pbxNativeTargetSection();
  for (const [key, target] of Object.entries(nativeTargets)) {
    if (typeof target === 'object' && String(target.name ?? '').includes('ExpoWidgetsTarget')) {
      return key;
    }
  }
  return null;
}

function addCrestResourceToXcodeProject(project) {
  const targetUuid = widgetTargetUuid(project);
  if (!targetUuid) {
    return;
  }
  const files = project.pbxFileReferenceSection();
  const already = Object.values(files).some(
    (file) => typeof file === 'object' && String(file.path ?? '').includes(CREST_ASSET_NAME),
  );
  if (already) {
    return;
  }
  project.addResourceFile(`ExpoWidgetsTarget/${CREST_ASSET_NAME}`, { target: targetUuid });
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
    copyWidgetCrestAsset(mod.modRequest.projectRoot, mod.modRequest.platformProjectRoot);
    addCrestResourceToXcodeProject(mod.modResults);
    return mod;
  });
}

const plugin = createRunOncePlugin(
  withIosOnThisDayNativeWidget,
  'withIosOnThisDayNativeWidget',
  '1.4.0',
);

plugin.applyOnThisDayNativeWidget = applyOnThisDayNativeWidget;
plugin.copyWidgetCrestAsset = copyWidgetCrestAsset;
plugin.addCrestResourceToXcodeProject = addCrestResourceToXcodeProject;
plugin.CREST_ASSET_NAME = CREST_ASSET_NAME;
plugin.GENERATED_ENTRY = GENERATED_ENTRY;
plugin.NATIVE_ENTRY = NATIVE_ENTRY;
plugin.TAG = TAG;
module.exports = plugin;
