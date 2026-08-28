import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { AppState, Linking } from 'react-native';
import * as Notifications from 'expo-notifications';
import { getAppConfig } from '../config/appConfig';
import { ApiError, fetchJson } from '../api/client';
import { parseMemberProfile, type MemberProfile } from '../api/me';
import type { AuthTokens } from '../api/auth';
import { clearPushRegistration, refreshPushRegistration, syncPushRegistration } from '../notifications';
import { logoutRemote, refreshAccessToken, revokeRefreshToken, signInWithProvider } from './oauth';
import {
  isSmokeAuthEnabled,
  parseSmokeAuthAccessToken,
  smokeAuthExpiresInSeconds,
  smokeAuthRefreshPlaceholder,
} from './smokeAuth';
import { clearStoredSession, readStoredSession, writeStoredSession } from './tokenStore';

export type Session = {
  isSignedIn: boolean;
  isRestoring: boolean;
  displayName: string | null;
  accessToken: string | null;
  profile: MemberProfile | null;
};

type SessionContextValue = Session & {
  signIn: (provider: string) => Promise<void>;
  signOut: () => Promise<void>;
  refreshProfile: () => Promise<MemberProfile | null>;
  ensureAccessToken: () => Promise<string | null>;
  setAccessToken: (accessToken: string | null) => void;
  applySmokeSession: (accessToken: string) => Promise<boolean>;
};

const SessionContext = createContext<SessionContextValue | undefined>(undefined);

const signedOut: Session = {
  isSignedIn: false,
  isRestoring: false,
  displayName: null,
  accessToken: null,
  profile: null,
};

function sessionFromAccessToken(
  accessToken: string | null | undefined,
  extras: Partial<Pick<Session, 'isRestoring' | 'displayName' | 'profile'>> = {},
): Session {
  const trimmed = accessToken?.trim() ?? '';
  const token = trimmed.length > 0 ? trimmed : null;
  return {
    isSignedIn: token !== null,
    isRestoring: extras.isRestoring ?? false,
    displayName: extras.displayName ?? null,
    accessToken: token,
    profile: extras.profile ?? null,
  };
}

function smokeAuthAllowed(): boolean {
  return isSmokeAuthEnabled({
    dev: typeof __DEV__ !== 'undefined' ? __DEV__ : false,
    appEnv: getAppConfig().appEnv,
  });
}

export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session>({ ...signedOut, isRestoring: true });
  const [refreshToken, setRefreshToken] = useState<string | null>(null);
  const [expiresAt, setExpiresAt] = useState(0);
  const sessionRef = useRef(session);
  const refreshTokenRef = useRef(refreshToken);
  const expiresAtRef = useRef(expiresAt);
  sessionRef.current = session;
  refreshTokenRef.current = refreshToken;
  expiresAtRef.current = expiresAt;

  const applyTokenState = useCallback((tokens: { accessToken: string; refreshToken: string; expiresAt: number }) => {
    refreshTokenRef.current = tokens.refreshToken;
    expiresAtRef.current = tokens.expiresAt;
    setRefreshToken(tokens.refreshToken);
    setExpiresAt(tokens.expiresAt);
    setSession((current) => {
      const next = sessionFromAccessToken(tokens.accessToken, {
        displayName: current.accessToken === tokens.accessToken ? current.displayName : null,
        profile: current.accessToken === tokens.accessToken ? current.profile : null,
      });
      sessionRef.current = next;
      return next;
    });
  }, []);

  const applyProfile = useCallback((accessToken: string, profile: MemberProfile | null) => {
    setSession(() => {
      const next = sessionFromAccessToken(accessToken, {
        displayName: profile?.displayName ?? null,
        profile,
      });
      sessionRef.current = next;
      return next;
    });
  }, []);

  const applyTokens = useCallback(
    async (tokens: AuthTokens): Promise<MemberProfile | null> => {
      const stored = await writeStoredSession(tokens);
      applyTokenState(stored);
      const profile = await loadProfile(tokens.accessToken);
      applyProfile(tokens.accessToken, profile);
      return profile;
    },
    [applyProfile, applyTokenState],
  );

  const clearLocal = useCallback(async () => {
    try {
      await clearStoredSession();
    } catch {
      // In-memory sign-out still has to happen if SecureStore delete fails.
    }
    refreshTokenRef.current = null;
    expiresAtRef.current = 0;
    const next = { ...signedOut, isRestoring: false };
    sessionRef.current = next;
    setRefreshToken(null);
    setExpiresAt(0);
    setSession(next);
  }, []);

  const refreshWithStoredGrant = useCallback(async (): Promise<string | null> => {
    const refresh = refreshTokenRef.current;
    if (!refresh) {
      return sessionRef.current.accessToken;
    }

    let tokens: AuthTokens;
    try {
      tokens = await refreshAccessToken(getAppConfig().apiBaseUrl, refresh);
    } catch {
      await clearLocal();
      return null;
    }

    try {
      await applyTokens(tokens);
    } catch {
      // The refresh grant itself succeeded — the access token is good. A follow-up
      // `/me` hiccup (a transient 401, an outage, ...) shouldn't sign the member out;
      // it just means the profile stays stale until it can be fetched successfully.
    }
    return tokens.accessToken;
  }, [applyTokens, clearLocal]);

  const ensureAccessToken = useCallback(async (): Promise<string | null> => {
    const currentToken = sessionRef.current.accessToken;
    if (currentToken && expiresAtRef.current > Date.now()) {
      return currentToken;
    }

    return refreshWithStoredGrant();
  }, [refreshWithStoredGrant]);

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      const stored = await readStoredSession();
      if (cancelled) {
        return;
      }

      if (!stored) {
        setSession({ ...signedOut, isRestoring: false });
        return;
      }

      try {
        const tokens =
          stored.expiresAt > Date.now()
            ? stored
            : await writeStoredSession(
                await refreshAccessToken(getAppConfig().apiBaseUrl, stored.refreshToken),
              );
        if (cancelled) {
          return;
        }

        applyTokenState(tokens);

        try {
          const profile = await loadProfile(tokens.accessToken);
          if (!cancelled) {
            applyProfile(tokens.accessToken, profile);
          }
        } catch (err) {
          if (cancelled) {
            return;
          }

          const canRetryRefresh =
            err instanceof ApiError && err.status === 401 && stored.expiresAt > Date.now();
          if (canRetryRefresh) {
            const next = await refreshWithStoredGrant();
            if (!next && !cancelled) {
              await clearLocal();
            }
            return;
          }

          if (err instanceof ApiError && err.status === 401) {
            await clearLocal();
          }
        }
      } catch {
        if (!cancelled) {
          await clearLocal();
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [applyProfile, applyTokenState, clearLocal, refreshWithStoredGrant]);

  useEffect(() => {
    const subscription = AppState.addEventListener('change', (state) => {
      if (state === 'active' && refreshTokenRef.current) {
        void ensureAccessToken();
      }
    });
    return () => subscription.remove();
  }, [ensureAccessToken]);

  const applySmokeSession = useCallback(
    async (accessToken: string): Promise<boolean> => {
      if (!smokeAuthAllowed()) {
        return false;
      }

      const token = accessToken.trim();
      if (!token) {
        return false;
      }

      await applyTokens({
        accessToken: token,
        refreshToken: smokeAuthRefreshPlaceholder,
        expiresIn: smokeAuthExpiresInSeconds,
      });
      return true;
    },
    [applyTokens],
  );

  useEffect(() => {
    if (!smokeAuthAllowed()) {
      return;
    }

    const handleUrl = (url: string | null) => {
      if (!url) {
        return;
      }
      const token = parseSmokeAuthAccessToken(url);
      if (token) {
        void applySmokeSession(token);
      }
    };

    const subscription = Linking.addEventListener('url', ({ url }) => handleUrl(url));
    void Linking.getInitialURL().then(handleUrl);
    return () => subscription.remove();
  }, [applySmokeSession]);

  const isSignedIn = session.isSignedIn;
  const accessToken = session.accessToken;

  // #850: request push permission and register the device once signed in
  // (not before) — never on cold start. Best-effort throughout; see
  // notifications/pushRegistration.ts.
  useEffect(() => {
    if (!isSignedIn || !accessToken) {
      return;
    }

    void syncPushRegistration(accessToken);

    const tokenSubscription = Notifications.addPushTokenListener(() => {
      void syncPushRegistration(accessToken);
    });

    const appStateSubscription = AppState.addEventListener('change', (state) => {
      if (state === 'active') {
        void refreshPushRegistration(accessToken);
      }
    });

    return () => {
      tokenSubscription.remove();
      appStateSubscription.remove();
    };
  }, [isSignedIn, accessToken]);

  const refreshProfile = useCallback(async () => {
    const token = await ensureAccessToken();
    if (!token) {
      return null;
    }

    try {
      const profile = await loadProfile(token);
      applyProfile(token, profile);
      return profile;
    } catch (err) {
      if (!(err instanceof ApiError) || err.status !== 401) {
        return sessionRef.current.profile;
      }

      const refresh = refreshTokenRef.current;
      if (!refresh) {
        await clearLocal();
        return null;
      }

      try {
        const tokens = await refreshAccessToken(getAppConfig().apiBaseUrl, refresh);
        return await applyTokens(tokens);
      } catch {
        await clearLocal();
        return null;
      }
    }
  }, [applyProfile, applyTokens, clearLocal, ensureAccessToken]);

  const value = useMemo<SessionContextValue>(
    () => ({
      ...session,
      signIn: async (provider: string) => {
        const tokens = await signInWithProvider(getAppConfig().apiBaseUrl, provider);
        await applyTokens(tokens);
      },
      applySmokeSession,
      signOut: async () => {
        const token = sessionRef.current.accessToken;
        const refresh = refreshTokenRef.current;
        const apiBaseUrl = getAppConfig().apiBaseUrl;
        // Clear the device session first. Remote logout/revoke/push unregister
        // can hang (React Native fetch often ignores AbortSignal) or crash the
        // process; awaiting them first left the member signed in after a kill.
        await clearLocal();
        startRemoteSignOut({ accessToken: token, refreshToken: refresh, apiBaseUrl });
      },
      refreshProfile,
      ensureAccessToken,
      setAccessToken: (accessToken) =>
        setSession((current) =>
          sessionFromAccessToken(accessToken, {
            isRestoring: current.isRestoring,
            displayName: current.displayName,
            profile: current.profile,
          }),
        ),
    }),
    [applySmokeSession, applyTokens, clearLocal, ensureAccessToken, refreshProfile, session],
  );

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}

export function useSession(): SessionContextValue {
  const value = useContext(SessionContext);
  if (!value) {
    throw new Error('useSession must be used inside SessionProvider');
  }

  return value;
}

function startRemoteSignOut(input: {
  accessToken: string | null;
  refreshToken: string | null;
  apiBaseUrl: string;
}): void {
  const tasks: Promise<unknown>[] = [];
  if (input.accessToken) {
    tasks.push(clearPushRegistration(input.accessToken));
    tasks.push(logoutRemote(input.apiBaseUrl, input.accessToken));
  }
  if (input.refreshToken) {
    tasks.push(revokeRefreshToken(input.apiBaseUrl, input.refreshToken));
  }
  if (tasks.length === 0) {
    return;
  }

  void Promise.allSettled(tasks);
}

async function loadProfile(accessToken: string): Promise<MemberProfile | null> {
  try {
    return parseMemberProfile(await fetchJson('/me', { accessToken }));
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      throw err;
    }

    return null;
  }
}
