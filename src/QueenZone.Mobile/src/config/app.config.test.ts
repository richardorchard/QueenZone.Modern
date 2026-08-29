import assert from 'node:assert/strict';
import { afterEach, describe, it } from 'node:test';

const originalRunNumber = process.env.GITHUB_RUN_NUMBER;
const originalIosBuild = process.env.IOS_BUILD_NUMBER;

afterEach(() => {
  if (originalRunNumber === undefined) {
    delete process.env.GITHUB_RUN_NUMBER;
  } else {
    process.env.GITHUB_RUN_NUMBER = originalRunNumber;
  }
  if (originalIosBuild === undefined) {
    delete process.env.IOS_BUILD_NUMBER;
  } else {
    process.env.IOS_BUILD_NUMBER = originalIosBuild;
  }
});

describe('app.config marketing version', () => {
  it('keeps the unsigned app.json default when no run number is set', async () => {
    delete process.env.GITHUB_RUN_NUMBER;
    delete process.env.IOS_BUILD_NUMBER;
    const { default: appConfig } = await import('../../app.config.ts');
    const config = appConfig({
      config: {
        name: 'QueenZone',
        slug: 'queenzone-mobile',
        version: '0.1.0',
      },
    });
    assert.equal(config.version, '0.1.0');
  });

  it('bakes {prefix}.{run} when GITHUB_RUN_NUMBER is present', async () => {
    process.env.GITHUB_RUN_NUMBER = '847';
    const { default: appConfig } = await import('../../app.config.ts');
    const config = appConfig({
      config: {
        name: 'QueenZone',
        slug: 'queenzone-mobile',
        version: '0.1.0',
      },
    });
    assert.equal(config.version, '0.1.847');
    assert.equal(config.ios?.buildNumber, '847');
    assert.equal(config.extra?.buildNumber, '847');
  });
});
