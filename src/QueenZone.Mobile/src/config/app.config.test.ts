import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { describe, it } from 'node:test';

const source = readFileSync(new URL('../../app.config.ts', import.meta.url), 'utf8');

describe('app.config marketing version', () => {
  it('bakes resolveMarketingVersion into Expo version from GITHUB_RUN_NUMBER', () => {
    assert.match(source, /marketingVersionPrefix/);
    assert.match(source, /resolveMarketingVersion/);
    assert.match(
      source,
      /const version = resolveMarketingVersion\(\{\s*prefix: marketingVersionPrefix,\s*runNumber: process\.env\.GITHUB_RUN_NUMBER,\s*\}\)/,
    );
    assert.match(source, /slug: config\.slug \?\? 'queenzone-mobile',\s*version,/);
  });
});

describe('app.config android versionCode', () => {
  it('bakes android.versionCode from GITHUB_RUN_NUMBER at prebuild', () => {
    assert.match(
      source,
      /const androidVersionCode = Number\(\s*resolveIosBuildNumber\(\{/,
    );
    assert.match(source, /override: process\.env\.ANDROID_VERSION_CODE/);
    assert.match(source, /githubRunNumber: process\.env\.GITHUB_RUN_NUMBER/);
    assert.match(source, /versionCode: androidVersionCode/);
  });
});
