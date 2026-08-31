import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  type BootSplashAction,
  type BootSplashState,
  SPLASH_FADE_MS,
  SPLASH_MAX_WAIT_MS,
  SPLASH_MIN_VISIBLE_MS,
  bootSplashReducer,
  initialBootSplashState,
} from './bootSplashMachine.ts';

function apply(actions: BootSplashAction[], start: BootSplashState = initialBootSplashState) {
  return actions.reduce(bootSplashReducer, start);
}

describe('boot splash timings', () => {
  it('keeps the design-handoff floor, ceiling, and fade durations', () => {
    assert.equal(SPLASH_MIN_VISIBLE_MS, 600);
    assert.equal(SPLASH_MAX_WAIT_MS, 2500);
    assert.equal(SPLASH_FADE_MS, 320);
  });
});

describe('bootSplashReducer', () => {
  it('starts in booting with the floor not elapsed', () => {
    assert.deepEqual(initialBootSplashState, { phase: 'booting', floorElapsed: false });
  });

  it('fast boot: ASSETS_READY then FLOOR_ELAPSED fades, then FADE_COMPLETE is done', () => {
    const holding = apply([{ type: 'ASSETS_READY' }]);
    assert.deepEqual(holding, { phase: 'holding', floorElapsed: false });

    const fading = apply([{ type: 'FLOOR_ELAPSED' }], holding);
    assert.deepEqual(fading, { phase: 'fading', floorElapsed: true });

    assert.deepEqual(apply([{ type: 'FADE_COMPLETE' }], fading), {
      phase: 'done',
      floorElapsed: true,
    });
  });

  it('floor-before-ready: FLOOR_ELAPSED stays booting, then ASSETS_READY fades immediately', () => {
    const afterFloor = apply([{ type: 'FLOOR_ELAPSED' }]);
    assert.deepEqual(afterFloor, { phase: 'booting', floorElapsed: true });

    assert.deepEqual(apply([{ type: 'ASSETS_READY' }], afterFloor), {
      phase: 'fading',
      floorElapsed: true,
    });
  });

  it('CEILING_REACHED from booting starts the fade', () => {
    assert.deepEqual(apply([{ type: 'CEILING_REACHED' }]), {
      phase: 'fading',
      floorElapsed: false,
    });
  });

  it('CEILING_REACHED from holding starts the fade', () => {
    const holding = apply([{ type: 'ASSETS_READY' }]);
    assert.deepEqual(apply([{ type: 'CEILING_REACHED' }], holding), {
      phase: 'fading',
      floorElapsed: false,
    });
  });

  it('CEILING_REACHED during fading or done is a no-op', () => {
    const fading = apply([{ type: 'CEILING_REACHED' }]);
    assert.equal(bootSplashReducer(fading, { type: 'CEILING_REACHED' }), fading);

    const done = apply([{ type: 'FADE_COMPLETE' }], fading);
    assert.equal(bootSplashReducer(done, { type: 'CEILING_REACHED' }), done);
  });

  it('treats a font error as ASSETS_READY with the same transitions', () => {
    // fontsLoaded || fontError both dispatch ASSETS_READY; the reducer has no FONT_ERROR.
    const holding = bootSplashReducer(initialBootSplashState, { type: 'ASSETS_READY' });
    assert.deepEqual(holding, { phase: 'holding', floorElapsed: false });
    assert.deepEqual(bootSplashReducer(holding, { type: 'FLOOR_ELAPSED' }), {
      phase: 'fading',
      floorElapsed: true,
    });
  });

  it('double ASSETS_READY is a no-op once holding, fading, or done', () => {
    const holding = apply([{ type: 'ASSETS_READY' }]);
    assert.equal(bootSplashReducer(holding, { type: 'ASSETS_READY' }), holding);

    const fading = apply([{ type: 'FLOOR_ELAPSED' }], holding);
    assert.equal(bootSplashReducer(fading, { type: 'ASSETS_READY' }), fading);

    const done = apply([{ type: 'FADE_COMPLETE' }], fading);
    assert.equal(bootSplashReducer(done, { type: 'ASSETS_READY' }), done);
  });

  it('ASSETS_READY and FLOOR_ELAPSED are no-ops once fading or done', () => {
    const fading = apply([{ type: 'CEILING_REACHED' }]);
    assert.equal(bootSplashReducer(fading, { type: 'ASSETS_READY' }), fading);
    assert.equal(bootSplashReducer(fading, { type: 'FLOOR_ELAPSED' }), fading);

    const done = apply([{ type: 'FADE_COMPLETE' }], fading);
    assert.equal(bootSplashReducer(done, { type: 'ASSETS_READY' }), done);
    assert.equal(bootSplashReducer(done, { type: 'FLOOR_ELAPSED' }), done);
  });
});
