import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { describe, it } from 'node:test';
import { createRequire } from 'node:module';

type EnvBag = Record<string, string | undefined>;

const require = createRequire(import.meta.url);
const smokeEmbed = require('../../plugins/smokeEmbed.cjs') as {
  isSmokeEmbedEnabled: (env?: EnvBag) => boolean;
  filterExpoPluginsForSmokeEmbed: (plugins: unknown[], env?: EnvBag) => unknown[];
  smokeEmbedAutolinking: () => { exclude: string[] };
  applyAndroidBundleInDebug: (contents: string) => string;
  applyAndroidReleaseDebugSigning: (contents: string) => string;
  applyAndroidManifestCleartextTraffic: (manifest: unknown) => unknown;
  applyAndroidSmokeGradleProperties: (properties: unknown[]) => unknown[];
  ANDROID_GRADLE_JVM_ARGS: string;
  DEV_CLIENT_PACKAGES: string[];
  EMBED_FLAG: string;
};

const appConfigSource = readFileSync(new URL('../../app.config.ts', import.meta.url), 'utf8');

describe('smoke embed flag', () => {
  it('is off unless QUEENZONE_MOBILE_SMOKE_EMBED is a truthy token', () => {
    assert.equal(smokeEmbed.isSmokeEmbedEnabled({}), false);
    assert.equal(smokeEmbed.isSmokeEmbedEnabled({ QUEENZONE_MOBILE_SMOKE_EMBED: '' }), false);
    assert.equal(smokeEmbed.isSmokeEmbedEnabled({ QUEENZONE_MOBILE_SMOKE_EMBED: '0' }), false);
    assert.equal(smokeEmbed.isSmokeEmbedEnabled({ QUEENZONE_MOBILE_SMOKE_EMBED: 'false' }), false);
    assert.equal(smokeEmbed.isSmokeEmbedEnabled({ QUEENZONE_MOBILE_SMOKE_EMBED: '1' }), true);
    assert.equal(smokeEmbed.isSmokeEmbedEnabled({ QUEENZONE_MOBILE_SMOKE_EMBED: 'true' }), true);
    assert.equal(smokeEmbed.isSmokeEmbedEnabled({ QUEENZONE_MOBILE_SMOKE_EMBED: 'YES' }), true);
  });

  it('drops expo-dev-client only when smoke embed is on', () => {
    const plugins = ['expo-font', 'expo-dev-client', ['expo-splash-screen', {}]];
    assert.deepEqual(smokeEmbed.filterExpoPluginsForSmokeEmbed(plugins, {}), plugins);
    assert.deepEqual(smokeEmbed.filterExpoPluginsForSmokeEmbed(plugins, { QUEENZONE_MOBILE_SMOKE_EMBED: '1' }), [
      'expo-font',
      ['expo-splash-screen', {}],
    ]);
  });

  it('excludes the expo-dev-client autolink set so Debug launch is the app', () => {
    for (const name of ['expo-dev-client', 'expo-dev-launcher', 'expo-dev-menu']) {
      assert.ok(smokeEmbed.DEV_CLIENT_PACKAGES.includes(name));
    }
    assert.deepEqual(smokeEmbed.smokeEmbedAutolinking().exclude, smokeEmbed.DEV_CLIENT_PACKAGES);
  });
});

describe('applyAndroidBundleInDebug', () => {
  it('sets debuggableVariants so Debug Gradle embeds the JS bundle', () => {
    const first = smokeEmbed.applyAndroidBundleInDebug('apply plugin: "com.facebook.react"\n\nreact {\n    autolinkLibrariesWithApp()\n}\n');
    assert.match(first, /debuggableVariants = \[\]/);
    assert.match(first, /queenzone-smoke-embed/);
    assert.equal(smokeEmbed.applyAndroidBundleInDebug(first), first);
  });

  it('appends a react block when prebuild did not emit one', () => {
    const patched = smokeEmbed.applyAndroidBundleInDebug('// app gradle\n');
    assert.match(patched, /react \{\s*debuggableVariants = \[\]\s*\}/);
  });
});

describe('applyAndroidReleaseDebugSigning', () => {
  it('signs Release with the debug keystore when prebuild left it unsigned', () => {
    const patched = smokeEmbed.applyAndroidReleaseDebugSigning(
      'buildTypes {\n    release {\n        minifyEnabled false\n    }\n}\n',
    );
    assert.match(patched, /signingConfig signingConfigs\.debug/);
    assert.match(patched, /queenzone-smoke-embed-release-signing/);
    assert.equal(smokeEmbed.applyAndroidReleaseDebugSigning(patched), patched);
  });

  it('leaves an existing debug signingConfig alone', () => {
    const source = 'release {\n        signingConfig signingConfigs.debug\n        minifyEnabled false\n}\n';
    assert.equal(smokeEmbed.applyAndroidReleaseDebugSigning(source), source);
  });
});

describe('applyAndroidManifestCleartextTraffic', () => {
  it('allows cleartext HTTP so smoke/journeys can reach the Testing host over 10.0.2.2 (#1372)', () => {
    const manifest = { manifest: { application: [{ $: { 'android:label': '@string/app_name' } }] } };
    const patched = smokeEmbed.applyAndroidManifestCleartextTraffic(manifest) as typeof manifest;
    assert.equal(
      (patched.manifest.application[0].$ as Record<string, string>)['android:usesCleartextTraffic'],
      'true',
    );
  });

  it('is a no-op when the manifest has no application element', () => {
    const manifest = { manifest: {} };
    assert.deepEqual(smokeEmbed.applyAndroidManifestCleartextTraffic(manifest), manifest);
  });
});

describe('applyAndroidSmokeGradleProperties', () => {
  it('replaces Expo\'s default Gradle heap for Release smoke packaging', () => {
    const properties = [
      { type: 'comment', value: 'Project-wide Gradle settings.' },
      { type: 'property', key: 'org.gradle.jvmargs', value: '-Xmx2048m -XX:MaxMetaspaceSize=512m' },
      { type: 'property', key: 'android.useAndroidX', value: 'true' },
    ];

    const patched = smokeEmbed.applyAndroidSmokeGradleProperties(properties) as {
      type: string;
      key?: string;
      value: string;
    }[];
    const jvmArgs = patched.filter((item) => item.key === 'org.gradle.jvmargs');

    assert.equal(jvmArgs.length, 1);
    assert.equal(jvmArgs[0]?.value, smokeEmbed.ANDROID_GRADLE_JVM_ARGS);
    assert.match(jvmArgs[0]?.value ?? '', /-Xmx6g/);
    assert.ok(patched.some((item) => item.key === 'android.useAndroidX'));
  });
});

describe('app.config smoke embed wiring', () => {
  it('filters expo-dev-client and registers the embed plugin when the flag is on', () => {
    assert.match(appConfigSource, /filterExpoPluginsForSmokeEmbed/);
    assert.match(appConfigSource, /isSmokeEmbedEnabled/);
    assert.match(appConfigSource, /smokeEmbedAutolinking/);
    assert.match(appConfigSource, /QUEENZONE_MOBILE_SMOKE_EMBED|smokeEmbed/);
    assert.match(appConfigSource, /'\.\/plugins\/smokeEmbed\.cjs'/);
    assert.match(appConfigSource, /smokeEmbed: smokeEmbed \|\| undefined/);
  });
});
