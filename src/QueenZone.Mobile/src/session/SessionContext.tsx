import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { AppState, Linking } from 'react-native';
import * as Notifications from 'expo-notifications';
import { getAppConfig } from '../config/appConfig';
import { fetchJson } from '../api/client';
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

  const applyTokens = useCallback(async (tokens: AuthTokens): Promise<MemberProfile | null> => {
    const stored = await writeStoredSession(tokens);
    setRefreshToken(stored.refreshToken);
    setExpiresAt(stored.expiresAt);
    setSession(sessionFromAccessToken(tokens.accessToken));
    const profile = await loadProfile(tokens.accessToken);
    setSession(
      sessionFromAccessToken(tokens.accessToken, {
        displayName: profile?.displayName ?? null,
        profile,
      }),
    );
    return profile;
  }, []);

  const clearLocal = useCallback(async () => {
    await clearStoredSession();
    setRefreshToken(null);
    setExpiresAt(0);
    setSession({ ...signedOut, isRestoring: false });
  }, []);

  const ensureAccessToken = useCallback(async (): Promise<string | null> => {
    if (session.accessToken && expiresAt > Date.now()) {
      return session.accessToken;
    }

    if (!refreshToken) {
      return session.accessToken;
    }

    try {
      const tokens = await refreshAccessToken(getAppConfig().apiBaseUrl, refreshToken);
      await applyTokens(tokens);
      return tokens.accessToken;
    } catch {
      await clearLocal();
      return null;
    }
  }, [applyTokens, clearLocal, expiresAt, refreshToken, session.accessToken]);

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
        const profile = await loadProfile(tokens.accessToken);
        if (cancelled) {
          return;
        }

        setRefreshToken(tokens.refreshToken);
        setExpiresAt(tokens.expiresAt);
        setSession(
          sessionFromAccessToken(tokens.accessToken, {
            displayName: profile?.displayName ?? null,
            profile,
          }),
        );
      } catch {
        if (!cancelled) {
          await clearLocal();
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [clearLocal]);

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

  const value = useMemo<SessionContextValue>(
    () => ({
      ...session,
      signIn: async (provider: string) => {
        const tokens = await signInWithProvider(getAppConfig().apiBaseUrl, provider);
        await applyTokens(tokens);
      },
      applySmokeSession,
      signOut: async () => {
        const token = session.accessToken;
        const refresh = refreshToken;
        if (token) {
          await clearPushRegistration(token);
          await logoutRemote(getAppConfig().apiBaseUrl, token);
        }

        if (refresh) {
          await revokeRefreshToken(getAppConfig().apiBaseUrl, refresh);
        }

        await clearLocal();
      },
      refreshProfile: async () => {
        const token = await ensureAccessToken();
        if (!token) {
          return null;
        }

        const profile = await loadProfile(token);
        setSession((current) =>
          sessionFromAccessToken(token, {
            isRestoring: current.isRestoring,
            displayName: profile?.displayName ?? current.displayName,
            profile,
          }),
        );
        return profile;
      },
      setAccessToken: (accessToken) =>
        setSession((current) =>
          sessionFromAccessToken(accessToken, {
            isRestoring: current.isRestoring,
            displayName: current.displayName,
            profile: current.profile,
          }),
        ),
    }),
    [applySmokeSession, applyTokens, clearLocal, ensureAccessToken, refreshToken, session],
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

async function loadProfile(accessToken: string): Promise<MemberProfile | null> {
  try {
    return parseMemberProfile(await fetchJson('/me', { accessToken }));
  } catch {
    return null;
  }
}
