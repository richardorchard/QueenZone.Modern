import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { describe, it } from 'node:test';

const maestroDir = new URL('../../maestro/', import.meta.url);
const scriptsDir = new URL('../../../../scripts/', import.meta.url);
const workflowsDir = new URL('../../../../.github/workflows/', import.meta.url);

function readMaestro(name: string): string {
  return readFileSync(new URL(name, maestroDir), 'utf8');
}

function readRepo(relFromWorkflowsOrScripts: string, base: URL): string {
  return readFileSync(new URL(relFromWorkflowsOrScripts, base), 'utf8');
}

describe('Maestro device flows (#1281)', () => {
  it('does not add journeys to smoke.yaml', () => {
    const smoke = readMaestro('smoke.yaml');
    assert.match(smoke, /flows\/01-launch\.yaml/);
    assert.match(smoke, /flows\/09-authenticated\.yaml/);
    assert.doesNotMatch(smoke, /10-forum-attach|11-news-discussion|12-masthead-unread/);
  });

  it('keeps the #1247 journeys shape and waits for chrome before smoke-auth', () => {
    const attach = readMaestro('flows/10-forum-attach.yaml');
    assert.match(attach, /launchApp:/);
    assert.match(attach, /id: home-screen/);
    assert.match(attach, /runFlow: open-smoke-auth\.yaml/);
    const homeIdx = attach.indexOf('id: home-screen');
    const authIdx = attach.indexOf('runFlow: open-smoke-auth.yaml');
    assert.ok(homeIdx >= 0 && authIdx > homeIdx);
    assert.match(attach, /id: tab-forum/);
  });

  it('dismisses the iOS Open-in-QueenZone confirm after smoke-auth and attach', () => {
    const openAuth = readMaestro('flows/open-smoke-auth.yaml');
    const accept = readMaestro('flows/accept-ios-open-link.yaml');
    assert.match(openAuth, /openLink: \$\{SMOKE_AUTH_URL\}/);
    assert.match(openAuth, /accept-ios-open-link\.yaml/);
    assert.match(accept, /Open in \.\*QueenZone/);
    assert.match(accept, /\^Open\$/);

    assert.match(readMaestro('flows/09-authenticated.yaml'), /open-smoke-auth\.yaml/);
    assert.match(readMaestro('flows/12-masthead-unread.yaml'), /open-smoke-auth\.yaml/);
    assert.match(readMaestro('flows/10-forum-attach.yaml'), /accept-ios-open-link\.yaml/);
  });
});

describe('device-smoke harness (#1281)', () => {
  it('prepends the Maestro install dir before probing PATH', () => {
    const script = readRepo('run-mobile-device-smoke.sh', scriptsDir);
    assert.match(script, /HOME\}\/\.maestro\/bin/);
    assert.match(script, /Maestro failing flow:/);
    assert.match(script, /timeout 30 adb logcat/);
  });

  it('fails leftover Android collect fast enough to upload artifacts', () => {
    const workflow = readRepo('mobile-device-smoke.yml', workflowsDir);
    assert.match(workflow, /Collect leftover Android smoke logs[\s\S]*timeout-minutes: 2/);
    assert.match(workflow, /Collect leftover Android journeys logs[\s\S]*timeout-minutes: 2/);
    assert.match(workflow, /maestro --version/);
  });
});

describe('device-smoke Release embed (#1322)', () => {
  it('builds and installs Release-embedded binaries, never Debug/dev-client', () => {
    const workflow = readRepo('mobile-device-smoke.yml', workflowsDir);
    const script = readRepo('run-mobile-device-smoke.sh', scriptsDir);

    assert.match(workflow, /assembleRelease/);
    assert.match(workflow, /apk\/release\/app-release\.apk/);
    assert.match(workflow, /-configuration Release/);
    assert.match(workflow, /Products\/Release-iphonesimulator/);
    assert.match(workflow, /QUEENZONE_MOBILE_SMOKE_EMBED/);
    assert.match(workflow, /SENTRY_DISABLE_AUTO_UPLOAD/);
    assert.doesNotMatch(workflow, /\.\/gradlew assembleDebug/);
    assert.doesNotMatch(workflow, /apk\/debug\/app-debug\.apk/);
    assert.doesNotMatch(workflow, /-configuration Debug/);
    assert.doesNotMatch(workflow, /Products\/Debug-iphonesimulator/);
    assert.doesNotMatch(workflow, /npx expo start|expo start --|metro start|packager start/i);

    assert.match(script, /assembleRelease/);
    assert.match(script, /android_release_apk/);
    assert.match(script, /-configuration Release/);
    assert.match(script, /Release-iphonesimulator/);
    assert.match(script, /QUEENZONE_MOBILE_SMOKE_EMBED=1/);
    assert.doesNotMatch(script, /\.\/gradlew assembleDebug/);
    assert.doesNotMatch(script, /apk\/debug\/app-debug\.apk/);
    assert.doesNotMatch(script, /-configuration Debug/);
    assert.doesNotMatch(script, /Products\/Debug-iphonesimulator/);
    assert.doesNotMatch(script, /npx expo start|expo start --|metro start|packager start/i);
  });

  it('documents Release-embedded device smoke in the mobile README', () => {
    const readme = readFileSync(new URL('../../README.md', import.meta.url), 'utf8');
    assert.match(readme, /Release-embedded/);
    assert.match(readme, /assembleRelease/);
    assert.match(readme, /never install `app-debug\.apk`/);
  });

  it('keeps home-screen as the launch assertion', () => {
    const launch = readMaestro('flows/01-launch.yaml');
    assert.match(launch, /id: home-screen/);
    assert.match(readMaestro('smoke.yaml'), /flows\/01-launch\.yaml/);
    assert.match(readMaestro('flows/10-forum-attach.yaml'), /id: home-screen/);
    assert.match(readMaestro('journeys.yaml'), /flows\/10-forum-attach\.yaml/);
  });
});
