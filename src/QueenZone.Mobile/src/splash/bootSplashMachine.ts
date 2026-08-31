/** In-app splash floor/ceiling (design handoff): never flash, never block. */
export const SPLASH_MIN_VISIBLE_MS = 600;
export const SPLASH_MAX_WAIT_MS = 2500;
export const SPLASH_FADE_MS = 320;

export type BootSplashPhase = 'booting' | 'holding' | 'fading' | 'done';

export type BootSplashState = {
  phase: BootSplashPhase;
  floorElapsed: boolean;
};

export const initialBootSplashState: BootSplashState = {
  phase: 'booting',
  floorElapsed: false,
};

export type BootSplashAction =
  | { type: 'ASSETS_READY' }
  | { type: 'FLOOR_ELAPSED' }
  | { type: 'CEILING_REACHED' }
  | { type: 'FADE_COMPLETE' };

export function bootSplashReducer(
  state: BootSplashState,
  action: BootSplashAction,
): BootSplashState {
  switch (action.type) {
    case 'ASSETS_READY':
      if (state.phase !== 'booting') {
        return state;
      }
      return state.floorElapsed
        ? { ...state, phase: 'fading' }
        : { ...state, phase: 'holding' };

    case 'FLOOR_ELAPSED':
      if (state.phase === 'holding') {
        return { ...state, floorElapsed: true, phase: 'fading' };
      }
      if (state.phase === 'booting') {
        return { ...state, floorElapsed: true };
      }
      return state;

    case 'CEILING_REACHED':
      if (state.phase === 'booting' || state.phase === 'holding') {
        return { ...state, phase: 'fading' };
      }
      return state;

    case 'FADE_COMPLETE':
      if (state.phase === 'fading') {
        return { ...state, phase: 'done' };
      }
      return state;
  }
}
