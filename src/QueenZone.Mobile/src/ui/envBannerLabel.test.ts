import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { resolveEnvBannerLabel } from './envBannerLabel.ts';

describe('resolveEnvBannerLabel', () => {
  it('hides on production builds', () => {
    assert.equal(resolveEnvBannerLabel('production'), null);
  });

  it('uses LOCAL for development, including smoke embeds', () => {
    assert.equal(resolveEnvBannerLabel('development'), 'LOCAL');
  });

  it('uses DEV for staging', () => {
    assert.equal(resolveEnvBannerLabel('staging'), 'DEV');
  });
});
