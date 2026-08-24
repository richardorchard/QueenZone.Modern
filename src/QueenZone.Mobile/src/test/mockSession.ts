/** Mutable session stub for Jest. Variable must stay `mock*` so factories can close over it. */

export function createMockSession() {
  return {
    isSignedIn: false,
    isRestoring: false,
    displayName: null as string | null,
    accessToken: null as string | null,
    profile: null as unknown,
    signIn: jest.fn(),
    signOut: jest.fn(),
    refreshProfile: jest.fn(),
    setAccessToken: jest.fn(),
    applySmokeSession: jest.fn(),
  };
}

export type MockSession = ReturnType<typeof createMockSession>;
