/**
 * Device-smoke / journeys embed (#1225, #1322).
 *
 * CI installs a Release-embedded binary (assembleRelease / Release-iphonesimulator)
 * so the JS bundle is in the APK/app and Maestro never sees the expo-dev-client
 * launcher. This plugin is belt-and-suspenders: strip expo-dev-client from
 * plugins/autolinking, force Android Debug to bundle too, and set iOS
 * FORCE_BUNDLING. Release embed is the primary fix — Debug + these flags
 * alone still opened the launcher. Local `expo start --dev-client` is
 * unchanged unless QUEENZONE_MOBILE_SMOKE_EMBED=1 is set at prebuild.
 */
const { createRunOncePlugin, withAppBuildGradle, withXcodeProject } = require('expo/config-plugins');

const TAG = 'queenzone-smoke-embed';
const EMBED_FLAG = 'QUEENZONE_MOBILE_SMOKE_EMBED';

const DEV_CLIENT_PACKAGES = [
  'expo-dev-client',
  'expo-dev-launcher',
  'expo-dev-menu',
  'expo-dev-menu-interface',
];

function isSmokeEmbedEnabled(env = process.env) {
  const raw = env[EMBED_FLAG];
  if (raw == null) {
    return false;
  }
  const value = String(raw).trim().toLowerCase();
  return value === '1' || value === 'true' || value === 'yes';
}

function pluginName(plugin) {
  return Array.isArray(plugin) ? plugin[0] : plugin;
}

function filterExpoPluginsForSmokeEmbed(plugins, env = process.env) {
  const list = Array.isArray(plugins) ? plugins : [];
  if (!isSmokeEmbedEnabled(env)) {
    return list;
  }
  return list.filter((plugin) => pluginName(plugin) !== 'expo-dev-client');
}

function smokeEmbedAutolinking() {
  return { exclude: [...DEV_CLIENT_PACKAGES] };
}

function applyAndroidBundleInDebug(contents) {
  if (contents.includes(TAG)) {
    return contents;
  }

  if (!contents.includes('react {')) {
    return `${contents.trimEnd()}\n\n// @generated begin ${TAG} - expo prebuild\nreact {\n    debuggableVariants = []\n}\n// @generated end ${TAG}\n`;
  }

  return contents.replace(
    /react\s*\{/,
    `react {\n    // @generated begin ${TAG} - expo prebuild\n    debuggableVariants = []\n    // @generated end ${TAG}`,
  );
}

function applyAndroidReleaseDebugSigning(contents) {
  const marker = `${TAG}-release-signing`;
  if (contents.includes(marker)) {
    return contents;
  }

  if (/release\s*\{[\s\S]*?signingConfig\s+signingConfigs\.debug/.test(contents)) {
    return contents;
  }

  if (!/release\s*\{/.test(contents)) {
    return contents;
  }

  return contents.replace(
    /release\s*\{/,
    `release {\n        // @generated begin ${marker} - expo prebuild\n        signingConfig signingConfigs.debug\n        // @generated end ${marker}`,
  );
}

function withAndroidSmokeEmbed(config) {
  return withAppBuildGradle(config, (mod) => {
    if (mod.modResults.language !== 'groovy') {
      return mod;
    }
    let next = applyAndroidBundleInDebug(mod.modResults.contents);
    next = applyAndroidReleaseDebugSigning(next);
    mod.modResults.contents = next;
    return mod;
  });
}

function withIosSmokeEmbed(config) {
  return withXcodeProject(config, (mod) => {
    const project = mod.modResults;
    if (typeof project.addBuildProperty === 'function') {
      project.addBuildProperty('FORCE_BUNDLING', '1');
    }
    return mod;
  });
}

function withSmokeEmbeddedBundle(config) {
  if (!isSmokeEmbedEnabled()) {
    return config;
  }
  config = withAndroidSmokeEmbed(config);
  config = withIosSmokeEmbed(config);
  return config;
}

const plugin = createRunOncePlugin(withSmokeEmbeddedBundle, 'withSmokeEmbeddedBundle', '1.0.0');
plugin.isSmokeEmbedEnabled = isSmokeEmbedEnabled;
plugin.filterExpoPluginsForSmokeEmbed = filterExpoPluginsForSmokeEmbed;
plugin.smokeEmbedAutolinking = smokeEmbedAutolinking;
plugin.applyAndroidBundleInDebug = applyAndroidBundleInDebug;
plugin.applyAndroidReleaseDebugSigning = applyAndroidReleaseDebugSigning;
plugin.DEV_CLIENT_PACKAGES = DEV_CLIENT_PACKAGES;
plugin.EMBED_FLAG = EMBED_FLAG;
module.exports = plugin;
