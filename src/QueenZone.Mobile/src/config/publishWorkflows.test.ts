import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { describe, it } from 'node:test';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../..');
const play = readFileSync(
  path.join(repoRoot, '.github/workflows/publish-android-google-play.yml'),
  'utf8',
);
const testflight = readFileSync(
  path.join(repoRoot, '.github/workflows/publish-ios-testflight.yml'),
  'utf8',
);

describe('Play publish workflow', () => {
  it('does not inject 0.1.0-internal as versionName', () => {
    assert.doesNotMatch(play, /0\.1\.0-internal/);
    assert.doesNotMatch(play, /android\.injected\.version\.name/);
  });

  it('keeps the monotonic versionCode and passes GITHUB_RUN_NUMBER into prebuild', () => {
    assert.match(play, /android\.injected\.version\.code/);
    const prebuild = play.split('name: Generate Android project')[1] ?? '';
    assert.match(prebuild, /GITHUB_RUN_NUMBER: \$\{\{ github\.run_number \}\}/);
  });
});

describe('TestFlight publish workflow', () => {
  it('asserts CFBundleShortVersionString equals the baked marketing version', () => {
    assert.match(testflight, /CFBundleVersion/);
    assert.match(testflight, /CFBundleShortVersionString/);
    assert.match(testflight, /resolveMarketingVersion/);
    assert.match(testflight, /marketingVersionPrefix/);
  });
});
