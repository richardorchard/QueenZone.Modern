/**
 * Align WorkManager artifacts after adding react-native-android-widget.
 *
 * work-runtime 2.8+ vendors the Kotlin helpers that used to live in
 * work-runtime-ktx. A leftover ktx 2.7.1 on the classpath then fails
 * :app:checkDebugDuplicateClasses (OneTimeWorkRequestKt / PeriodicWorkRequestKt).
 */
const { createRunOncePlugin, withProjectBuildGradle } = require('expo/config-plugins');

const TAG = 'queenzone-work-runtime-alignment';
const WORK_RUNTIME = '2.8.1';

const SNIPPET = `
allprojects {
    configurations.configureEach {
        resolutionStrategy {
            force "androidx.work:work-runtime:${WORK_RUNTIME}"
            force "androidx.work:work-runtime-ktx:${WORK_RUNTIME}"
        }
    }
}
`;

function applyWorkRuntimeAlignment(contents) {
  if (contents.includes(TAG)) {
    return contents;
  }

  return `${contents.trimEnd()}\n\n// @generated begin ${TAG} - expo prebuild\n${SNIPPET.trim()}\n// @generated end ${TAG}\n`;
}

function withAndroidWorkRuntimeAlignment(config) {
  return withProjectBuildGradle(config, (mod) => {
    mod.modResults.contents = applyWorkRuntimeAlignment(mod.modResults.contents);
    return mod;
  });
}

const plugin = createRunOncePlugin(
  withAndroidWorkRuntimeAlignment,
  'withAndroidWorkRuntimeAlignment',
  '1.0.0',
);

plugin.applyWorkRuntimeAlignment = applyWorkRuntimeAlignment;
module.exports = plugin;

