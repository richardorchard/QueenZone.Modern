import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';

export type Session = {
  isSignedIn: boolean;
  displayName: string | null;
  /** Member access token from `/api/v1/auth/token`. Null for the development sign-in toggle. */
  accessToken: string | null;
};

type SessionContextValue = Session & {
  signIn: () => void;
  signOut: () => void;
};

const SessionContext = createContext<SessionContextValue | undefined>(undefined);

const signedOut: Session = { isSignedIn: false, displayName: null, accessToken: null };

export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session>(signedOut);

  const value = useMemo<SessionContextValue>(
    () => ({
      ...session,
      signIn: () => setSession({ isSignedIn: true, displayName: 'Dev member', accessToken: null }),
      signOut: () => setSession(signedOut),
    }),
    [session],
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
